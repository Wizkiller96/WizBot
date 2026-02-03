using Newtonsoft.Json;
using Microsoft.Extensions.Caching.Memory;

namespace WizBot.Modules.Roblox.Services;

public class RobloxService : INService
{
    private readonly HttpClient _http;
    private readonly string _creds;
    private readonly IMemoryCache _cache;

    public RobloxService(IHttpClientFactory factory, IBotCreds creds, IMemoryCache cache)
    {
        _http = factory.CreateClient();
        _creds = creds.RobloxApiKey;
        _cache = cache;
    }

    // ⭐ Existing WizBot API (kept exactly as before)
    public async Task<RobloxUserInfo?> GetUserInfoAsync(string username)
    {
        var cacheKey = $"wizbot-rbxinfo-{username.ToLower()}";

        // ⭐ Return cached result if available
        if (_cache.TryGetValue(cacheKey, out RobloxUserInfo cached))
            return cached;

        try
        {
            var json = await _http.GetStringAsync(
                $"https://wizbot.cc/api/v1/roblox/getPlayerInfo/{username}");

            var info = JsonConvert.DeserializeObject<RobloxUserInfo>(json);

            if (info != null)
            {
                // ⭐ Cache for 5 minutes
                _cache.Set(cacheKey, info, TimeSpan.FromMinutes(5));
            }

            return info;
        }
        catch
        {
            return null;
        }
    }

    // ⭐ Roblox Cloud API lookup (cached)
    public async Task<RobloxCloudUser?> GetVerificationStatusAsync(long userId)
    {
        var cacheKey = $"rbx-verify-{userId}";

        if (_cache.TryGetValue(cacheKey, out RobloxCloudUser cached))
            return cached;

        try
        {
            var req = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://apis.roblox.com/cloud/v2/users/{userId}"
            );

            req.Headers.Add("x-api-key", _creds);

            var res = await _http.SendAsync(req);

            if (!res.IsSuccessStatusCode)
                return null;

            var json = await res.Content.ReadAsStringAsync();
            var cloud = JsonConvert.DeserializeObject<RobloxCloudUser>(json);

            var result = new RobloxCloudUser
            {
                Premium = cloud.Premium,
                IdVerified = cloud.IdVerified
            };

            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));

            return result;
        }
        catch
        {
            return null;
        }
    }
}