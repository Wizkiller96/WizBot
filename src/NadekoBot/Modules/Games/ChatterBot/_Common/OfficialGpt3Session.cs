#nullable disable
using Newtonsoft.Json;
using System.Net.Http.Json;
using SharpToken;
using Antlr.Runtime;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NadekoBot.Modules.Games.Common.ChatterBot;

public class OfficialGpt3Session : IChatterBotSession
{
    private string Uri
        => $"https://api.openai.com/v1/chat/completions";

    private readonly string _apiKey;
    private readonly string _model;
    private readonly int _maxHistory;
    private readonly int _maxTokens;
    private readonly int _minTokens;
    private readonly string _nadekoUsername;
    private readonly GptEncoding _encoding;
    private List<GPTMessage> messages =  new();
    private readonly IHttpClientFactory _httpFactory;

    

    public OfficialGpt3Session(
        string apiKey,
        ChatGptModel model,
        int chatHistory,
        int maxTokens,
        int minTokens,
        string personality,
        string nadekoUsername,
        IHttpClientFactory factory)
    {
        _apiKey = apiKey;
        _httpFactory = factory;
        switch (model)
        {
            case ChatGptModel.Gpt35Turbo:
                _model = "gpt-3.5-turbo";
                break;
            case ChatGptModel.Gpt4:
                _model = "gpt-4";
                break;
            case ChatGptModel.Gpt432k:
                _model = "gpt-4-32k";
                break;
        }
        _maxHistory = chatHistory;
        _maxTokens = maxTokens;
        _minTokens = minTokens;
        _nadekoUsername = nadekoUsername;
        _encoding = GptEncoding.GetEncodingForModel(_model);
        messages.Add(new GPTMessage(){Role = "user", Content = personality, Name = _nadekoUsername});
    }

    public async Task<string> Think(string input, string username)
    {
        messages.Add(new GPTMessage(){Role = "user", Content = input, Name = username});
        while(messages.Count > _maxHistory + 2){
            messages.RemoveAt(1);
        }
        int tokensUsed = 0;
        foreach(GPTMessage message in messages){
            tokensUsed += _encoding.Encode(message.Content).Count;
        }
        tokensUsed *= 2; //Unsure why this is the case, but the token count chatgpt reports back is double what I calculate.
        //check if we have the minimum number of tokens available to use. Remove messages until we have enough, otherwise exit out and inform the user why.
        while(_maxTokens - tokensUsed <= _minTokens){
            if(messages.Count > 2){
                int tokens = _encoding.Encode(messages[1].Content).Count * 2;
                tokensUsed -= tokens;
                messages.RemoveAt(1);
            }
            else{
                return "Token count exceeded, please increase the number of tokens in the bot config and restart.";
            }
        }
        using var http = _httpFactory.CreateClient();
        http.DefaultRequestHeaders.Authorization = new("Bearer", _apiKey);
        var data = await http.PostAsJsonAsync(Uri, new Gpt3ApiRequest()
        {
            Model = _model,
            Messages = messages,
            MaxTokens = _maxTokens - tokensUsed,
            Temperature = 1,
        });
        var dataString = await data.Content.ReadAsStringAsync();
        try
        {
            var response = JsonConvert.DeserializeObject<Gpt3Response>(dataString);
            string message = response?.Choices[0]?.Message?.Content;
            //Can't rely on the return to except, now that we need to add it to the messages list.
            _ = message ?? throw new ArgumentNullException(nameof(message));
            messages.Add(new GPTMessage(){Role = "assistant", Content = message, Name = _nadekoUsername});
            return message;
        }
        catch
        {
            Log.Warning("Unexpected GPT-3 response received: {ResponseString}", dataString);
            return null;
        }
    }
}

