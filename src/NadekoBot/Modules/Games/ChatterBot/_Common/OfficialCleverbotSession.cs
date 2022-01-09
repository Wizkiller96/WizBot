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

public class CleverbotIoSession : IChatterBotSession
{
    private readonly string _key;
    private readonly string _user;
    private readonly IHttpClientFactory _httpFactory;
    private readonly AsyncLazy<string> _nick;

    private readonly string _createEndpoint = "https://cleverbot.io/1.0/create";
    private readonly string _askEndpoint = "https://cleverbot.io/1.0/ask";

    public CleverbotIoSession(string user, string key, IHttpClientFactory factory)
    {
        _key = key;
        _user = user;
        _httpFactory = factory;

        _nick = new(GetNick);
    }

    private async Task<string> GetNick()
    {
        using var http = _httpFactory.CreateClient();
        using var msg = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("user", _user), new KeyValuePair<string, string>("key", _key)
        });
        using var data = await http.PostAsync(_createEndpoint, msg);
        var str = await data.Content.ReadAsStringAsync();
        var obj = JsonConvert.DeserializeObject<CleverbotIoCreateResponse>(str);
        if (obj.Status != "success")
            throw new OperationCanceledException(obj.Status);

        return obj.Nick;
    }

    public async Task<string> Think(string input)
    {
        using var http = _httpFactory.CreateClient();
        using var msg = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("user", _user), new KeyValuePair<string, string>("key", _key),
            new KeyValuePair<string, string>("nick", await _nick), new KeyValuePair<string, string>("text", input)
        });
        using var data = await http.PostAsync(_askEndpoint, msg);
        var str = await data.Content.ReadAsStringAsync();
        var obj = JsonConvert.DeserializeObject<CleverbotIoAskResponse>(str);
        if (obj.Status != "success")
            throw new OperationCanceledException(obj.Status);

        return obj.Response;
    }
}