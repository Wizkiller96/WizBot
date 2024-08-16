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

    public static int ResolveGameMode(string mode)
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

    public static string ResolveGameMode(int mode)
    {
        switch (mode)
        {
            case 0:
                return "Standard";
            case 1:
                return "Taiko";
            case 2:
                return "Catch";
            case 3:
                return "Mania";
            default:
                return "Standard";
        }
    }

    public async Task<(GatariUserData userData, GatariUserStats userStats)> GetGatariDataAsync(
        string user,
        string mode)
    {
        using var http = _httpFactory.CreateClient();
        var modeNumber = string.IsNullOrWhiteSpace(mode) ? 0 : ResolveGameMode(mode);

        var resString = await http.GetStringAsync($"https://api.gatari.pw/user/stats?u={user}&mode={modeNumber}");

        var statsResponse = JsonConvert.DeserializeObject<GatariUserStatsResponse>(resString);
        if (statsResponse.Code != 200 || statsResponse.Stats.Id == 0)
        {
            return default;
        }

        var usrResString = await http.GetStringAsync($"https://api.gatari.pw/users/get?u={user}");

        var userData = JsonConvert.DeserializeObject<GatariUserResponse>(usrResString).Users[0];
        var userStats = statsResponse.Stats;

        return (userData, userStats);
    }
}