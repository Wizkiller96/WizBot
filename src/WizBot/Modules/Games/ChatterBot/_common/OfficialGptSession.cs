#nullable disable
using Newtonsoft.Json;
using OneOf.Types;
using System.Net.Http.Json;
using SharpToken;
using System.CodeDom;
using System.Text.RegularExpressions;

namespace WizBot.Modules.Games.Common.ChatterBot;

public partial class OfficialGptSession : IChatterBotSession
{
    private string Uri
        => $"https://api.openai.com/v1/chat/completions";

    private readonly string _apiKey;
    private readonly string _model;
    private readonly int _maxHistory;
    private readonly int _maxTokens;
    private readonly int _minTokens;
    private readonly string _wizbotUsername;
    private readonly GptEncoding _encoding;
    private List<GPTMessage> messages = new();
    private readonly IHttpClientFactory _httpFactory;


    public OfficialGptSession(
        string apiKey,
        ChatGptModel model,
        int chatHistory,
        int maxTokens,
        int minTokens,
        string personality,
        string wizbotUsername,
        IHttpClientFactory factory)
    {
        _apiKey = apiKey;
        _httpFactory = factory;

        _model = model switch
        {
            ChatGptModel.Gpt35Turbo => "gpt-3.5-turbo",
            ChatGptModel.Gpt4o => "gpt-4o",
            _ => throw new ArgumentException("Unknown, unsupported or obsolete model", nameof(model))
        };

        _maxHistory = chatHistory;
        _maxTokens = maxTokens;
        _minTokens = minTokens;
        _wizbotUsername = UsernameCleaner().Replace(wizbotUsername, "");
        _encoding = GptEncoding.GetEncodingForModel(_model);
        messages.Add(new()
        {
            Role = "system",
            Content = personality,
            Name = _wizbotUsername
        });
    }

    [GeneratedRegex("[^a-zA-Z0-9_-]")]
    private static partial Regex UsernameCleaner();

    public async Task<OneOf.OneOf<ThinkResult, Error<string>>> Think(string input, string username)
    {
        username = UsernameCleaner().Replace(username, "");
        
        messages.Add(new()
        {
            Role = "user",
            Content = input,
            Name = username
        });
        
        while (messages.Count > _maxHistory + 2)
        {
            messages.RemoveAt(1);
        }

        var tokensUsed = messages.Sum(message => _encoding.Encode(message.Content).Count);

        tokensUsed *= 2;

        //check if we have the minimum number of tokens available to use. Remove messages until we have enough, otherwise exit out and inform the user why.
        while (_maxTokens - tokensUsed <= _minTokens)
        {
            if (messages.Count > 2)
            {
                var tokens = _encoding.Encode(messages[1].Content).Count * 2;
                tokensUsed -= tokens;
                messages.RemoveAt(1);
            }
            else
            {
                return new Error<string>("Token count exceeded, please increase the number of tokens in the bot config and restart.");
            }
        }

        using var http = _httpFactory.CreateClient();
        http.DefaultRequestHeaders.Authorization = new("Bearer", _apiKey);
        
        var data = await http.PostAsJsonAsync(Uri,
            new Gpt3ApiRequest()
            {
                Model = _model,
                Messages = messages,
                MaxTokens = _maxTokens - tokensUsed,
                Temperature = 1,
            });
        
        var dataString = await data.Content.ReadAsStringAsync();
        try
        {
            var response = JsonConvert.DeserializeObject<OpenAiCompletionResponse>(dataString);
            
            Log.Information("Received response: {response} ", dataString);
            var res = response?.Choices?[0];
            var message = res?.Message?.Content;

            if (message is null)
            {
                return new Error<string>("ChatGpt: Received no response.");
            }
            
            messages.Add(new()
            {
                Role = "assistant",
                Content = message,
                Name = _wizbotUsername
            });
            
            return new ThinkResult()
            {
                Text = message,
                TokensIn = response.Usage.PromptTokens,
                TokensOut = response.Usage.CompletionTokens
            };
        }
        catch
        {
            Log.Warning("Unexpected response received from OpenAI: {ResponseString}", dataString);
            return new Error<string>("Unexpected response received");
        }
    }
}

public sealed class ThinkResult
{
    public string Text { get; set; }
    public int TokensIn { get; set; }
    public int TokensOut { get; set; }
}