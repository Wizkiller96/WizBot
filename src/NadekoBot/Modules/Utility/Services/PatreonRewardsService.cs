using Discord.WebSocket;
using NadekoBot.Services;
using NadekoBot.Services.Database.Models;
using NadekoBot.Modules.Utility.Common.Patreon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Discord;
using NadekoBot.Modules.Gambling.Services;
using NadekoBot.Extensions;
using Serilog;
using StackExchange.Redis;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace NadekoBot.Modules.Utility.Services
{
    public class PatreonRewardsService : INService
    {
        private readonly SemaphoreSlim getPledgesLocker = new SemaphoreSlim(1, 1);

        private readonly Timer _updater;
        private readonly SemaphoreSlim claimLockJustInCase = new SemaphoreSlim(1, 1);
        
        public TimeSpan Interval { get; } = TimeSpan.FromMinutes(3);
        private readonly DbService _db;
        private readonly ICurrencyService _currency;
        private readonly GamblingConfigService _gamblingConfigService;
        private readonly ConnectionMultiplexer _redis;
        private readonly IBotCredsProvider _credsProvider;
        private readonly IHttpClientFactory _httpFactory;
        private readonly IEmbedBuilderService _eb;
        private readonly DiscordSocketClient _client;

        public DateTime LastUpdate { get; private set; } = DateTime.UtcNow;

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

            if (client.ShardId == 0)
                _updater = new Timer(async _ => await RefreshPledges(_credsProvider.GetCreds()).ConfigureAwait(false),
                    null, TimeSpan.Zero, Interval);
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


        private sealed class PatreonRefreshData
        {
            [JsonPropertyName("access_token")]
            public string AccessToken { get; set; }
            
            [JsonPropertyName("refresh_token")]
            public string RefreshToken { get; set; }
            
            [JsonPropertyName("expires_in")]
            public long ExpiresIn { get; set; }
            
            [JsonPropertyName("scope")]
            public string Scope { get; set; }
            
            [JsonPropertyName("token_type")]
            public string TokenType { get; set; }
        }
        
        private async Task<bool> UpdateAccessToken(IBotCredentials creds)
        {
            Log.Information("Updating patreon access token...");
            try
            {
                using var http = _httpFactory.CreateClient();
                var res = await http.PostAsync($"https://www.patreon.com/api/oauth2/token" +
                                               $"?grant_type=refresh_token" +
                                               $"&refresh_token={creds.Patreon.RefreshToken}" +
                                               $"&client_id={creds.Patreon.ClientId}" +
                                               $"&client_secret={creds.Patreon.ClientSecret}",
                    new StringContent(string.Empty));

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
            var _1 = creds.Patreon.ClientId;
            var _2 = creds.Patreon.ClientSecret;
            var _4 = creds.Patreon.RefreshToken;
            return !(string.IsNullOrWhiteSpace(_1)
                     || string.IsNullOrWhiteSpace(_2)
                     || string.IsNullOrWhiteSpace(_4));
        }

        public async Task RefreshPledges(IBotCredentials creds)
        {
            if (DateTime.UtcNow.Day < 5)
                return;

            // if the user has the necessary patreon creds
            // and the access token expired or doesn't exist
            // -> update access token
            if (!HasPatreonCreds(creds))
                return;

            if (LastAccessTokenUpdate(creds).Month < DateTime.UtcNow.Month
                || string.IsNullOrWhiteSpace(creds.Patreon.AccessToken))
            {
                var success = await UpdateAccessToken(creds);
                if (!success)
                    return;
            }

            LastUpdate = DateTime.UtcNow;
            await getPledgesLocker.WaitAsync().ConfigureAwait(false);
            try
            {
                
                var members = new List<PatreonMember>();
                var users = new List<PatreonUser>();
                using (var http = _httpFactory.CreateClient())
                {
                    http.DefaultRequestHeaders.Clear();
                    http.DefaultRequestHeaders.TryAddWithoutValidation("Authorization",
                        $"Bearer {creds.Patreon.AccessToken}");

                    var page = $"https://www.patreon.com/api/oauth2/v2/campaigns/{creds.Patreon.CampaignId}/members" +
                               "?fields%5Bmember%5D=full_name,currently_entitled_amount_cents" +
                               "&fields%5Buser%5D=social_connections" +
                               "&include=user";
                    PatreonResponse data = null;
                    do
                    {
                        var res = await http.GetStringAsync(page).ConfigureAwait(false);
                        data = JsonSerializer.Deserialize<PatreonResponse>(res);

                        if (data is null)
                            break;
                        
                        members.AddRange(data.Data);
                        users.AddRange(data.Included);
                    } while (!string.IsNullOrWhiteSpace(page = data?.Links?.Next));
                }

                var userData = members.Join(users,
                    (m) => m.Relationships.User.Data.Id,
                    (u) => u.Id,
                    (m, u) => new
                    {
                        PatreonUserId = m.Relationships.User.Data.Id,
                        UserId = ulong.TryParse(u.Attributes?.SocialConnections?.Discord?.UserId ?? string.Empty,
                            out var userId)
                            ? userId
                            : 0,
                        EntitledTo = m.Attributes.CurrentlyEntitledAmountCents,
                    })
                    .Where(x => x is
                    {
                        UserId: not 0,
                        EntitledTo: > 0
                    })
                    .ToList();

                foreach (var pledge in userData)
                {
                    await ClaimReward(pledge.UserId, pledge.PatreonUserId, pledge.EntitledTo);
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error refreshing patreon pledges");
            }
            finally
            {
                getPledgesLocker.Release();
            }

        }

        public async Task<int> ClaimReward(ulong userId, string patreonUserId, int cents)
        {
            await claimLockJustInCase.WaitAsync().ConfigureAwait(false);
            var settings = _gamblingConfigService.Data;
            var now = DateTime.UtcNow;
            try
            {
                var eligibleFor = (int)(cents * settings.PatreonCurrencyPerCent);

                using (var uow = _db.GetDbContext())
                {
                    var users = uow.Set<RewardedUser>();
                    var usr = await users.FirstOrDefaultAsync(x => x.PatreonUserId == patreonUserId);

                    if (usr is null)
                    {
                        users.Add(new RewardedUser()
                        {
                            PatreonUserId = patreonUserId,
                            LastReward = now,
                            AmountRewardedThisMonth = eligibleFor,
                        });

                        await uow.SaveChangesAsync();

                        await _currency.AddAsync(userId, "Patreon reward - new", eligibleFor, gamble: true);
                        
                        Log.Information($"Sending new currency reward to {userId}");
                        await SendMessageToUser(userId, $"Thank you for your pledge! " +
                                                        $"You've been awarded **{eligibleFor}**{settings.Currency.Sign} !");
                        return eligibleFor;
                    }

                    if (usr.LastReward.Month != now.Month)
                    {
                        usr.LastReward = now;
                        usr.AmountRewardedThisMonth = eligibleFor;

                        await uow.SaveChangesAsync();

                        await _currency.AddAsync(userId, "Patreon reward - recurring", eligibleFor, gamble: true);

                        Log.Information($"Sending recurring currency reward to {userId}");
                        await SendMessageToUser(userId, $"Thank you for your continued support! " +
                                                        $"You've been awarded **{eligibleFor}**{settings.Currency.Sign} for this month's support!");

                        return eligibleFor;
                    }

                    if (usr.AmountRewardedThisMonth < eligibleFor)
                    {
                        var toAward = eligibleFor - usr.AmountRewardedThisMonth;

                        usr.LastReward = now;
                        usr.AmountRewardedThisMonth = toAward;
                        await uow.SaveChangesAsync();

                        await _currency.AddAsync(userId, "Patreon reward - update", toAward, gamble: true);
                        
                        Log.Information($"Sending updated currency reward to {userId}");
                        await SendMessageToUser(userId, $"Thank you for increasing your pledge! " +
                                                        $"You've been awarded an additional **{toAward}**{settings.Currency.Sign} !");
                        return toAward;
                    }

                    return 0;
                }
            }
            finally
            {
                claimLockJustInCase.Release();
            }
        }

        private async Task SendMessageToUser(ulong userId, string message)
        {
            try
            {
                var user = (IUser)_client.GetUser(userId) ?? await _client.Rest.GetUserAsync(userId);
                if (user is null)
                    return;
                
                var channel = await user.GetOrCreateDMChannelAsync();
                await channel.SendConfirmAsync(_eb, message);
            }
            catch
            {
                // ignored
            }
        }
    }
}
