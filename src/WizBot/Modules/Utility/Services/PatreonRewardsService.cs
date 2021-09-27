﻿using Discord.WebSocket;
using WizBot.Services;
using WizBot.Services.Database.Models;
using WizBot.Modules.Utility.Common.Patreon;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using WizBot.Modules.Gambling.Services;
using WizBot.Extensions;
using Serilog;

namespace WizBot.Modules.Utility.Services
{
    public class PatreonRewardsService : INService
    {
        private readonly SemaphoreSlim getPledgesLocker = new SemaphoreSlim(1, 1);

        private PatreonUserAndReward[] _pledges;

        private readonly Timer _updater;
        private readonly SemaphoreSlim claimLockJustInCase = new SemaphoreSlim(1, 1);
        
        public TimeSpan Interval { get; } = TimeSpan.FromMinutes(3);
        private readonly IBotCredentials _creds;
        private readonly DbService _db;
        private readonly ICurrencyService _currency;
        private readonly GamblingConfigService _gamblingConfigService;
        private readonly IHttpClientFactory _httpFactory;
        private readonly IEmbedBuilderService _eb;
        private readonly DiscordSocketClient _client;

        public DateTime LastUpdate { get; private set; } = DateTime.UtcNow;

        public PatreonRewardsService(IBotCredentials creds, DbService db,
            ICurrencyService currency, IHttpClientFactory factory, IEmbedBuilderService eb,
            DiscordSocketClient client, GamblingConfigService gamblingConfigService)
        {
            _creds = creds;
            _db = db;
            _currency = currency;
            _gamblingConfigService = gamblingConfigService;
            _httpFactory = factory;
            _eb = eb;
            _client = client;

            if (client.ShardId == 0)
                _updater = new Timer(async _ => await RefreshPledges().ConfigureAwait(false),
                    null, TimeSpan.Zero, Interval);
        }

        public async Task RefreshPledges()
        {
            if (string.IsNullOrWhiteSpace(_creds.PatreonAccessToken)
                || string.IsNullOrWhiteSpace(_creds.PatreonAccessToken))
                return;

            if (DateTime.UtcNow.Day < 5)
                return;

            LastUpdate = DateTime.UtcNow;
            await getPledgesLocker.WaitAsync().ConfigureAwait(false);
            try
            {
                var rewards = new List<PatreonPledge>();
                var users = new List<PatreonUser>();
                using (var http = _httpFactory.CreateClient())
                {
                    http.DefaultRequestHeaders.Clear();
                    http.DefaultRequestHeaders.Add("Authorization", "Bearer " + _creds.PatreonAccessToken);
                    var data = new PatreonData()
                    {
                        Links = new PatreonDataLinks()
                        {
                            next = $"https://api.patreon.com/oauth2/api/campaigns/{_creds.PatreonCampaignId}/pledges"
                        }
                    };
                    do
                    {
                        var res = await http.GetStringAsync(data.Links.next)
                            .ConfigureAwait(false);
                        data = JsonConvert.DeserializeObject<PatreonData>(res);
                        var pledgers = data.Data.Where(x => x["type"].ToString() == "pledge");
                        rewards.AddRange(pledgers.Select(x => JsonConvert.DeserializeObject<PatreonPledge>(x.ToString()))
                            .Where(x => x.attributes.declined_since is null));
                        if (data.Included != null)
                        {
                            users.AddRange(data.Included
                                .Where(x => x["type"].ToString() == "user")
                                .Select(x => JsonConvert.DeserializeObject<PatreonUser>(x.ToString())));
                        }
                    } while (!string.IsNullOrWhiteSpace(data.Links.next));
                }
                var toSet = rewards.Join(users, (r) => r.relationships?.patron?.data?.id, (u) => u.id, (x, y) => new PatreonUserAndReward()
                {
                    User = y,
                    Reward = x,
                }).ToArray();

                _pledges = toSet;

                foreach (var pledge in _pledges)
                {
                    var userIdStr = pledge.User.attributes?.social_connections?.discord?.user_id;
                    if (userIdStr != null && ulong.TryParse(userIdStr, out var userId))
                    {
                        await ClaimReward(userId);
                    }
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

        public async Task<int> ClaimReward(ulong userId)
        {
            await claimLockJustInCase.WaitAsync().ConfigureAwait(false);
            var settings = _gamblingConfigService.Data;
            var now = DateTime.UtcNow;
            try
            {
                var datas = _pledges?.Where(x => x.User.attributes?.social_connections?.discord?.user_id == userId.ToString())
                    ?? Enumerable.Empty<PatreonUserAndReward>();

                var totalAmount = 0;
                foreach (var data in datas)
                {
                    var amount = (int)(data.Reward.attributes.amount_cents * settings.PatreonCurrencyPerCent);

                    using (var uow = _db.GetDbContext())
                    {
                        var users = uow.Set<RewardedUser>();
                        var usr = users.FirstOrDefault(x => x.PatreonUserId == data.User.id);

                        if (usr is null)
                        {
                            users.Add(new RewardedUser()
                            {
                                PatreonUserId = data.User.id,
                                LastReward = now,
                                AmountRewardedThisMonth = amount,
                            });

                            await uow.SaveChangesAsync();

                            await _currency.AddAsync(userId, "Patreon reward - new", amount, gamble: true);
                            totalAmount += amount;
                            
                            Log.Information($"Sending new currency reward to {userId}");
                            await SendMessageToUser(userId, $"Thank you for your pledge! " +
                                                            $"You've been awarded **{amount}**{settings.Currency.Sign} !");
                            continue;
                        }

                        if (usr.LastReward.Month != now.Month)
                        {
                            usr.LastReward = now;
                            usr.AmountRewardedThisMonth = amount;

                            await uow.SaveChangesAsync();

                            await _currency.AddAsync(userId, "Patreon reward - recurring", amount, gamble: true);
                            totalAmount += amount;
                            Log.Information($"Sending recurring currency reward to {userId}");
                            await SendMessageToUser(userId, $"Thank you for your continued support! " +
                                                            $"You've been awarded **{amount}**{settings.Currency.Sign} for this month's support!");
                            continue;
                        }

                        if (usr.AmountRewardedThisMonth < amount)
                        {
                            var toAward = amount - usr.AmountRewardedThisMonth;

                            usr.LastReward = now;
                            usr.AmountRewardedThisMonth = amount;
                            await uow.SaveChangesAsync();

                            await _currency.AddAsync(userId, "Patreon reward - update", toAward, gamble: true);
                            totalAmount += toAward;
                            Log.Information($"Sending updated currency reward to {userId}");
                            await SendMessageToUser(userId, $"Thank you for increasing your pledge! " +
                                $"You've been awarded an additional **{toAward}**{settings.Currency.Sign} !");
                            continue;
                        }
                    }
                }

                return totalAmount;
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
