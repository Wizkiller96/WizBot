#nullable disable
using LinqToDB.EntityFrameworkCore;
using NadekoBot.Common.ModuleBehaviors;
using NadekoBot.Modules.Gambling.Services;
using NadekoBot.Modules.Utility.Common.Patreon;
using NadekoBot.Services.Database.Models;
using StackExchange.Redis;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace NadekoBot.Modules.Utility;

public class PatreonRewardsService : INService, IReadyExecutor
{
    public TimeSpan Interval { get; } = TimeSpan.FromMinutes(3);

    public DateTime LastUpdate { get; private set; } = DateTime.UtcNow;

    private readonly SemaphoreSlim _claimLockJustInCase = new(1, 1);
    private readonly DbService _db;
    private readonly ICurrencyService _currency;
    private readonly GamblingConfigService _gamblingConfigService;
    private readonly ConnectionMultiplexer _redis;
    private readonly IBotCredsProvider _credsProvider;
    private readonly IHttpClientFactory _httpFactory;
    private readonly IEmbedBuilderService _eb;
    private readonly DiscordSocketClient _client;

    public PatreonRewardsService(
        DbService db,
        ICurrencyService currency,
        IHttpClientFactory factory,
        IEmbedBuilderService eb,
        DiscordSocketClient client,
        GamblingConfigService gamblingConfigService,
        ConnectionMultiplexer redis,
        IBotCredsProvider credsProvider)
    {
        _db = db;
        _currency = currency;
        _gamblingConfigService = gamblingConfigService;
        _redis = redis;
        _credsProvider = credsProvider;
        _httpFactory = factory;
        _eb = eb;
        _client = client;
    }

    public async Task OnReadyAsync()
    {
        if (_client.ShardId != 0)
            return;

        using var t = new PeriodicTimer(Interval);
        do
        {
            try
            {
                await RefreshPledges(_credsProvider.GetCreds());
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error refreshing patreon pledges: {ErrorMessage}", ex.Message);
            }
        } while (await t.WaitForNextTickAsync());
    }

    private DateTime LastAccessTokenUpdate(IBotCredentials creds)
    {
        var db = _redis.GetDatabase();
        var val = db.StringGet($"{creds.RedisKey()}_patreon_update");

        if (val == default)
            return DateTime.MinValue;

        var lastTime = DateTime.FromBinary((long)val);
        return lastTime;
    }

    private async Task<bool> UpdateAccessToken(IBotCredentials creds)
    {
        Log.Information("Updating patreon access token...");
        try
        {
            using var http = _httpFactory.CreateClient();
            using var content = new StringContent(string.Empty);
            using var res = await http.PostAsync("https://www.patreon.com/api/oauth2/token"
                                           + "?grant_type=refresh_token"
                                           + $"&refresh_token={creds.Patreon.RefreshToken}"
                                           + $"&client_id={creds.Patreon.ClientId}"
                                           + $"&client_secret={creds.Patreon.ClientSecret}",
                content);

            res.EnsureSuccessStatusCode();

            var data = await res.Content.ReadFromJsonAsync<PatreonRefreshData>();

            if (data is null)
                throw new("Invalid patreon response.");

            _credsProvider.ModifyCredsFile(oldData =>
            {
                oldData.Patreon.AccessToken = data.AccessToken;
                oldData.Patreon.RefreshToken = data.RefreshToken;
            });

            var db = _redis.GetDatabase();
            await db.StringSetAsync($"{creds.RedisKey()}_patreon_update", DateTime.UtcNow.ToBinary());
            return true;
        }
        catch (Exception ex)
        {
            Log.Error("Failed updating patreon access token: {ErrorMessage}", ex.ToString());
            return false;
        }
    }

    private bool HasPatreonCreds(IBotCredentials creds)
    {
        var cid = creds.Patreon.ClientId;
        var cs = creds.Patreon.ClientSecret;
        var rt = creds.Patreon.RefreshToken;
        return !(string.IsNullOrWhiteSpace(cid) || string.IsNullOrWhiteSpace(cs) || string.IsNullOrWhiteSpace(rt));
    }

    public async Task RefreshPledges(IBotCredentials creds)
    {
        if (DateTime.UtcNow.Day < 5)
            return;

        if (string.IsNullOrWhiteSpace(creds.Patreon.CampaignId))
            return;

        var lastUpdate = LastAccessTokenUpdate(creds);
        var now = DateTime.UtcNow;

        if (lastUpdate.Year != now.Year
            || lastUpdate.Month != now.Month
            || string.IsNullOrWhiteSpace(creds.Patreon.AccessToken))
        {
            // if the user has the necessary patreon creds
            // and the access token expired or doesn't exist
            // -> update access token
            if (!HasPatreonCreds(creds))
                return;

            var success = await UpdateAccessToken(creds);
            if (!success)
                return;
        }

        LastUpdate = DateTime.UtcNow;
        try
        {
            var members = new List<PatreonMember>();
            var users = new List<PatreonUser>();
            using (var http = _httpFactory.CreateClient())
            {
                http.DefaultRequestHeaders.Clear();
                http.DefaultRequestHeaders.TryAddWithoutValidation("Authorization",
                    $"Bearer {creds.Patreon.AccessToken}");

                var page = $"https://www.patreon.com/api/oauth2/v2/campaigns/{creds.Patreon.CampaignId}/members"
                           + "?fields%5Bmember%5D=full_name,currently_entitled_amount_cents"
                           + "&fields%5Buser%5D=social_connections"
                           + "&include=user";
                PatreonResponse data;
                do
                {
                    var res = await http.GetStringAsync(page);
                    data = JsonSerializer.Deserialize<PatreonResponse>(res);

                    if (data is null)
                        break;

                    members.AddRange(data.Data);
                    users.AddRange(data.Included);
                } while (!string.IsNullOrWhiteSpace(page = data.Links?.Next));
            }

            var userData = members.Join(users,
                                      m => m.Relationships.User.Data.Id,
                                      u => u.Id,
                                      (m, u) => new
                                      {
                                          PatreonUserId = m.Relationships.User.Data.Id,
                                          UserId = ulong.TryParse(
                                              u.Attributes?.SocialConnections?.Discord?.UserId ?? string.Empty,
                                              out var userId)
                                              ? userId
                                              : 0,
                                          EntitledTo = m.Attributes.CurrentlyEntitledAmountCents
                                      })
                                  .Where(x => x is
                                  {
                                      UserId: not 0,
                                      EntitledTo: > 0
                                  })
                                  .ToList();

            foreach (var pledge in userData)
                await ClaimReward(pledge.UserId, pledge.PatreonUserId, pledge.EntitledTo);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            Log.Warning("Patreon credentials invalid or expired. I will try to refresh them during the next run");
            var db = _redis.GetDatabase();
            await db.KeyDeleteAsync($"{creds.RedisKey()}_patreon_update");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error refreshing patreon pledges");
        }
    }

    public async Task<int> ClaimReward(ulong userId, string patreonUserId, int cents)
    {
        await _claimLockJustInCase.WaitAsync();
        var settings = _gamblingConfigService.Data;
        var now = DateTime.UtcNow;
        try
        {
            var eligibleFor = (int)(cents * settings.PatreonCurrencyPerCent);

            await using var uow = _db.GetDbContext();
            var users = uow.Set<RewardedUser>();
            var usr = await users.FirstOrDefaultAsyncEF(x => x.PatreonUserId == patreonUserId);

            if (usr is null)
            {
                users.Add(new()
                {
                    PatreonUserId = patreonUserId,
                    LastReward = now,
                    AmountRewardedThisMonth = eligibleFor
                });

                await uow.SaveChangesAsync();

                await _currency.AddAsync(userId, eligibleFor, new("patreon", "new"));

                Log.Information("Sending new currency reward to {UserId}", userId);
                await SendMessageToUser(userId,
                    "Thank you for your pledge! " + $"You've been awarded **{eligibleFor}**{settings.Currency.Sign} !");
                return eligibleFor;
            }

            if (usr.LastReward.Month != now.Month)
            {
                usr.LastReward = now;
                usr.AmountRewardedThisMonth = eligibleFor;

                await uow.SaveChangesAsync();

                await _currency.AddAsync(userId, eligibleFor, new("patreon", "recurring"));

                Log.Information("Sending recurring currency reward to {UserId}", userId);
                await SendMessageToUser(userId,
                    "Thank you for your continued support! "
                    + $"You've been awarded **{eligibleFor}**{settings.Currency.Sign} for this month's support!");

                return eligibleFor;
            }

            if (usr.AmountRewardedThisMonth < eligibleFor)
            {
                var toAward = eligibleFor - usr.AmountRewardedThisMonth;

                usr.LastReward = now;
                usr.AmountRewardedThisMonth = eligibleFor;
                await uow.SaveChangesAsync();

                await _currency.AddAsync(userId, toAward, new("patreon", "update"));

                Log.Information("Sending updated currency reward to {UserId}", userId);
                await SendMessageToUser(userId,
                    "Thank you for increasing your pledge! "
                    + $"You've been awarded an additional **{toAward}**{settings.Currency.Sign} !");
                return toAward;
            }

            return 0;
        }
        finally
        {
            _claimLockJustInCase.Release();
        }
    }

    private async Task SendMessageToUser(ulong userId, string message)
    {
        try
        {
            var user = (IUser)_client.GetUser(userId) ?? await _client.Rest.GetUserAsync(userId);
            if (user is null)
                return;

            await user.SendConfirmAsync(_eb, message);
        }
        catch
        {
            // ignored
        }
    }
}