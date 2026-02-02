using Newtonsoft.Json;

namespace WizBot.Modules.Roblox.Services;

public class RobloxService : INService
{
    private readonly HttpClient _http;

    public RobloxService(IHttpClientFactory factory)
    {
        _http = factory.CreateClient();
    }

    public async Task<RobloxUserInfo?> GetUserInfoAsync(string username)
    {
        try
        {
            var json = await _http.GetStringAsync(
                $"https://wizbot.cc/api/v1/roblox/getPlayerInfo/{username}");

            return JsonConvert.DeserializeObject<RobloxUserInfo>(json);
        }
        catch
        {
            return null;
        }
    }
}