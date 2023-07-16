#nullable disable
using Newtonsoft.Json;
using System.Net.Http.Json;

namespace NadekoBot.Modules.Games.Common.ChatterBot;

public class OfficialGpt3Session : IChatterBotSession
{
    private string Uri
        => $"https://api.openai.com/v1/completions";

    private readonly string _apiKey;
    private readonly string _model;
    private readonly int _maxTokens;
    private readonly IHttpClientFactory _httpFactory;

    public OfficialGpt3Session(
        string apiKey,
        Gpt3Model model,
        int maxTokens,
        IHttpClientFactory factory)
    {
        _apiKey = apiKey;
        _httpFactory = factory;
        switch (model)
        {
            case Gpt3Model.Ada001:
                _model = "text-ada-001";
                break;
            case Gpt3Model.Babbage001:
                _model = "text-babbage-001";
                break;
            case Gpt3Model.Curie001:
                _model = "text-curie-001";
                break;
            case Gpt3Model.Davinci003:
                _model = "text-davinci-003";
                break;
        }

        _maxTokens = maxTokens;
    }

    public async Task<string> Think(string input)
    {
        using var http = _httpFactory.CreateClient();
        http.DefaultRequestHeaders.Authorization = new("Bearer", _apiKey);
        var data = await http.PostAsJsonAsync(Uri, new Gpt3ApiRequest()
        {
            Model = _model,
            Prompt = input,
            MaxTokens = _maxTokens,
            Temperature = 1,
        });
        var dataString = await data.Content.ReadAsStringAsync();
        try
        {
            var response = JsonConvert.DeserializeObject<Gpt3Response>(dataString);

            return response?.Choices[0]?.Text;
        }
        catch
        {
            Log.Warning("Unexpected GPT-3 response received: {ResponseString}", dataString);
            return null;
        }
    }
}

