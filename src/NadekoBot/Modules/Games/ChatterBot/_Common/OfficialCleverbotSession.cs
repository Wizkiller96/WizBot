#nullable disable
using Newtonsoft.Json;

namespace NadekoBot.Modules.Games.Common.ChatterBot;

public class OfficialCleverbotSession : IChatterBotSession
{
    private string QueryString
        => $"https://www.cleverbot.com/getreply?key={_apiKey}" + "&wrapper=nadekobot" + "&input={0}" + "&cs={1}";

    private readonly string _apiKey;
    private readonly IHttpClientFactory _httpFactory;
    private string cs;

    public OfficialCleverbotSession(string apiKey, IHttpClientFactory factory)
    {
        _apiKey = apiKey;
        _httpFactory = factory;
    }

    public async Task<string> Think(string input)
    {
        using var http = _httpFactory.CreateClient();
        var dataString = await http.GetStringAsync(string.Format(QueryString, input, cs ?? ""));
        try
        {
            var data = JsonConvert.DeserializeObject<CleverbotResponse>(dataString);

            cs = data?.Cs;
            return data?.Output;
        }
        catch
        {
            Log.Warning("Unexpected cleverbot response received: {ResponseString}", dataString);
            return null;
        }
    }
}