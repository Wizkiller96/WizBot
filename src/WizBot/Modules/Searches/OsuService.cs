#nullable disable
using WizBot.Modules.Searches.Common;
using Newtonsoft.Json;

namespace WizBot.Modules.Searches;

public sealed class OsuService : INService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IBotCredentials _creds;

    public OsuService(IHttpClientFactory httpFactory, IBotCredentials creds)
    {
        _httpFactory = httpFactory;
        _creds = creds;
    }

    public async Task<OsuUserData> GetOsuData(string username, string mode)
    {
        using var http = _httpFactory.CreateClient();

        var modeNumber = string.IsNullOrWhiteSpace(mode) ? 0 : ResolveGameMode(mode);
        var userReq = $"https://osu.ppy.sh/api/get_user?k={_creds.OsuApiKey}&u={username}&m={modeNumber}";
        var userResString = await http.GetStringAsync(userReq);

        if (string.IsNullOrWhiteSpace(userResString))
            return null;
        var objs = JsonConvert.DeserializeObject<List<OsuUserData>>(userResString);

        if (objs.Count == 0)
        {
            return null;
        }

        var obj = objs[0];

        obj.ModeNumber = modeNumber;
        return obj;
    }

    private static int ResolveGameMode(string mode)
    {
        switch (mode.ToUpperInvariant())
        {
            case "STD":
            case "STANDARD":
                return 0;
            case "TAIKO":
                return 1;
            case "CTB":
            case "CATCHTHEBEAT":
                return 2;
            case "MANIA":
            case "OSU!MANIA":
                return 3;
            default:
                return 0;
        }
    }
}