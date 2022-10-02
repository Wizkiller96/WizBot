using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using NadekoBot.Common.ModuleBehaviors;
using NadekoBot.Db.Models;
using OneOf;
using OneOf.Types;
using StackExchange.Redis;
using CommandInfo = Discord.Commands.CommandInfo;

namespace NadekoBot.Modules.Utility.Patronage;

/// <inheritdoc cref="IPatronageService"/>
public sealed class PatronageService
    : IPatronageService,
        IReadyExecutor,
        IExecPreCommand,
        INService
{
    public event Func<Patron, Task> OnNewPatronPayment = static delegate { return Task.CompletedTask; };
    public event Func<Patron, Patron, Task> OnPatronUpdated = static delegate { return Task.CompletedTask; };
    public event Func<Patron, Task> OnPatronRefunded = static delegate { return Task.CompletedTask; };

    // this has to run right before the command
    public int Priority
        => int.MinValue;

    private static readonly PatronTier[] _tiers = Enum.GetValues<PatronTier>();

    private readonly PatronageConfig _pConf;
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;
    private readonly ISubscriptionHandler _subsHandler;
    private readonly IEmbedBuilderService _eb;
    private static readonly TypedKey<long> _quotaKey 
        = new($"quota:last_hourly_reset");

    private readonly IBotCache _cache;
    private readonly IBotCredsProvider _creds;

    public PatronageService(
        PatronageConfig pConf,
        DbService db,
        DiscordSocketClient client,
        ISubscriptionHandler subsHandler,
        IEmbedBuilderService eb,
        IBotCache cache, 
        IBotCredsProvider creds)
    {
        _pConf = pConf;
        _db = db;
        _client = client;
        _subsHandler = subsHandler;
        _eb = eb;
        _cache = cache;
        _creds = creds;
    }

    public Task OnReadyAsync()
    {
        if (_client.ShardId != 0)
            return Task.CompletedTask;

        return Task.WhenAll(ResetLoopAsync(), LoadSubscribersLoopAsync());
    }

    private async Task LoadSubscribersLoopAsync()
    {
        var timer = new PeriodicTimer(TimeSpan.FromSeconds(60));
        while (await timer.WaitForNextTickAsync())
        {
            try
            {
                if (!_pConf.Data.IsEnabled)
                    continue;

                await foreach (var batch in _subsHandler.GetPatronsAsync())
                {
                    await ProcesssPatronsAsync(batch);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error processing patrons");
            }
        }
    }

    public async Task ResetLoopAsync()
    {
        await Task.Delay(1.Minutes());
        while (true)
        {
            try
            {
                if (!_pConf.Data.IsEnabled)
                {
                    await Task.Delay(1.Minutes());
                    continue;
                }

                var now = DateTime.UtcNow;
                var lastRun = DateTime.MinValue;

                var result = await _cache.GetAsync(_quotaKey);
                if (result.TryGetValue(out var lastVal) && lastVal != default)
                {
                    lastRun = DateTime.FromBinary(lastVal);
                }

                var nowDate = now.ToDateOnly();
                var lastDate = lastRun.ToDateOnly();

                await using var ctx = _db.GetDbContext();
                await using var tran = await ctx.Database.BeginTransactionAsync();

                if ((lastDate.Day == 1 || (lastDate.Month != nowDate.Month)) && nowDate.Day > 1)
                {
                    // assumes bot won't be offline for a year
                    await ctx.GetTable<PatronQuota>()
                             .TruncateAsync();
                }
                else if (nowDate.DayNumber != lastDate.DayNumber)
                {
                    // day is different, means hour is different.
                    // reset both hourly and daily quota counts.
                    await ctx.GetTable<PatronQuota>()
                             .UpdateAsync((old) => new()
                             {
                                 HourlyCount = 0,
                                 DailyCount = 0,
                             });
                }
                else if (now.Hour != lastRun.Hour) // if it's not, just reset hourly quotas
                {
                    await ctx.GetTable<PatronQuota>()
                             .UpdateAsync((old) => new()
                             {
                                 HourlyCount = 0
                             });
                }

                // assumes that the code above runs in less than an hour
                await _cache.AddAsync(_quotaKey, now.ToBinary());
                await tran.CommitAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in quota reset loop. Message: {ErrorMessage}", ex.Message);
            }

            await Task.Delay(TimeSpan.FromHours(1).Add(TimeSpan.FromMinutes(1)));
        }
    }

    private async Task ProcesssPatronsAsync(IReadOnlyCollection<ISubscriberData> subscribersEnum)
    {
        // process only users who have discord accounts connected
        var subscribers = subscribersEnum.Where(x => x.UserId != 0).ToArray();

        if (subscribers.Length == 0)
            return;

        var todayDate = DateTime.UtcNow.Date;
        await using var ctx = _db.GetDbContext();

        // handle paid users
        foreach (var subscriber in subscribers.Where(x => x.ChargeStatus == SubscriptionChargeStatus.Paid))
        {
            if (subscriber.LastCharge is null)
                continue;
        
            var lastChargeUtc = subscriber.LastCharge.Value.ToUniversalTime();
            var dateInOneMonth = lastChargeUtc.Date.AddMonths(1);
            // await using var tran = await ctx.Database.BeginTransactionAsync();
            try
            {
                var dbPatron = await ctx.GetTable<PatronUser>()
                                        .FirstOrDefaultAsync(x
                                            => x.UniquePlatformUserId == subscriber.UniquePlatformUserId);
        
                if (dbPatron is null)
                {
                    // if the user is not in the database alrady
                    dbPatron = await ctx.GetTable<PatronUser>()
                                        .InsertWithOutputAsync(() => new()
                                        {
                                            UniquePlatformUserId = subscriber.UniquePlatformUserId,
                                            UserId = subscriber.UserId,
                                            AmountCents = subscriber.Cents,
                                            LastCharge = lastChargeUtc,
                                            ValidThru = dateInOneMonth,
                                        });
        
                    // await tran.CommitAsync();
        
                    var newPatron = PatronUserToPatron(dbPatron);
                    _ = SendWelcomeMessage(newPatron);
                    await OnNewPatronPayment(newPatron);
                }
                else
                {
                    if (dbPatron.LastCharge.Month < lastChargeUtc.Month)
                    {
                        // user is charged again for this month
                        // if his sub would end in teh future, extend it by one month.
                        // if it's not, just add 1 month to the last charge date
                        var count = await ctx.GetTable<PatronUser>()
                                             .Where(x => x.UniquePlatformUserId == subscriber.UniquePlatformUserId)
                                             .UpdateAsync(old => new()
                                             {
                                                 UserId = subscriber.UserId,
                                                 AmountCents = subscriber.Cents,
                                                 LastCharge = lastChargeUtc,
                                                 ValidThru = old.ValidThru >= todayDate
                                                     // ? Sql.DateAdd(Sql.DateParts.Month, 1, old.ValidThru).Value
                                                     ? old.ValidThru.AddMonths(1)
                                                     : dateInOneMonth,
                                             });
        
                        // this should never happen
                        if (count == 0)
                        {
                            // await tran.RollbackAsync();
                            continue;
                        }
        
                        // await tran.CommitAsync();
        
                        await OnNewPatronPayment(PatronUserToPatron(dbPatron));
                    }
                    else if (dbPatron.AmountCents != subscriber.Cents // if user changed the amount 
                             || dbPatron.UserId != subscriber.UserId) // if user updated user id)
                    {
                        var cents = subscriber.Cents;
                        // the user updated the pledge or changed the connected discord account
                        await ctx.GetTable<PatronUser>()
                                 .Where(x => x.UniquePlatformUserId == subscriber.UniquePlatformUserId)
                                 .UpdateAsync(old => new()
                                 {
                                     UserId = subscriber.UserId,
                                     AmountCents = cents,
                                     LastCharge = lastChargeUtc,
                                     ValidThru = old.ValidThru,
                                 });

                        var newPatron = dbPatron.Clone();
                        newPatron.AmountCents = cents;
                        newPatron.UserId = subscriber.UserId;
                        
                        // idk what's going on but UpdateWithOutputAsync doesn't work properly here
                        // nor does firstordefault after update. I'm not seeing something obvious
                        await OnPatronUpdated(
                            PatronUserToPatron(dbPatron),
                            PatronUserToPatron(newPatron));
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex,
                    "Unexpected error occured while processing rewards for patron {UserId}",
                    subscriber.UserId);
            }
        }
        
        var expiredDate = DateTime.MinValue;
        foreach (var patron in subscribers.Where(x => x.ChargeStatus == SubscriptionChargeStatus.Refunded))
        {
            // if the subscription is refunded, Disable user's valid thru 
            var changedCount = await ctx.GetTable<PatronUser>()
                                  .Where(x => x.UniquePlatformUserId == patron.UniquePlatformUserId
                                              && x.ValidThru != expiredDate)
                                  .UpdateAsync(old => new()
                                  {
                                      ValidThru = expiredDate
                                  });
        
            if (changedCount == 0)
                continue;

            var updated = await ctx.GetTable<PatronUser>()
                                  .Where(x => x.UniquePlatformUserId == patron.UniquePlatformUserId)
                                  .FirstAsync();
            
            await OnPatronRefunded(PatronUserToPatron(updated));
        }
    }

    public async Task<bool> ExecPreCommandAsync(ICommandContext ctx,
        string moduleName,
        CommandInfo command)
    {
        var ownerId = ctx.Guild?.OwnerId ?? 0;

        var result = await AttemptRunCommand(
            ctx.User.Id,
            ownerId: ownerId,
            command.Aliases.First().ToLowerInvariant(),
            command.Module.Parent == null ? string.Empty : command.Module.GetGroupName().ToLowerInvariant(),
            moduleName.ToLowerInvariant()
        );

        return result.Match(
            _ => false,
            ins =>
            {
                var eb = _eb.Create(ctx)
                            .WithPendingColor()
                            .WithTitle("Insufficient Patron Tier")
                            .AddField("For", $"{ins.FeatureType}: `{ins.Feature}`", true)
                            .AddField("Required Tier",
                                $"[{ins.RequiredTier.ToFullName()}](https://patreon.com/join/nadekobot)",
                                true);

                if (ctx.Guild is null || ctx.Guild?.OwnerId == ctx.User.Id)
                    eb.WithDescription("You don't have the sufficent Patron Tier to run this command.")
                      .WithFooter("You can use '.patron' and '.donate' commands for more info");
                else
                    eb.WithDescription(
                          "Neither you nor the server owner have the sufficent Patron Tier to run this command.")
                      .WithFooter("You can use '.patron' and '.donate' commands for more info");

                _ = ctx.WarningAsync();

                if (ctx.Guild?.OwnerId == ctx.User.Id)
                    _ = ctx.Channel.EmbedAsync(eb);
                else
                    _ = ctx.User.EmbedAsync(eb);

                return true;
            },
            quota =>
            {
                var eb = _eb.Create(ctx)
                            .WithPendingColor()
                            .WithTitle("Quota Limit Reached");

                if (quota.IsOwnQuota || ctx.User.Id == ownerId)
                {
                    eb.WithDescription($"You've reached your quota of `{quota.Quota} {quota.QuotaPeriod.ToFullName()}`")
                      .WithFooter("You may want to check your quota by using the '.patron' command.");
                }
                else
                {
                    eb.WithDescription(
                          $"This server reached the quota of {quota.Quota} `{quota.QuotaPeriod.ToFullName()}`")
                      .WithFooter("You may contact the server owner about this issue.\n"
                                  + "Alternatively, you can become patron yourself by using the '.donate' command.\n"
                                  + "If you're already a patron, it means you've reached your quota.\n"
                                  + "You can use '.patron' command to check your quota status.");
                }

                eb.AddField("For", $"{quota.FeatureType}: `{quota.Feature}`", true)
                  .AddField("Resets At", quota.ResetsAt.ToShortAndRelativeTimestampTag(), true);

                _ = ctx.WarningAsync();

                // send the message in the server in case it's the owner
                if (ctx.Guild?.OwnerId == ctx.User.Id)
                    _ = ctx.Channel.EmbedAsync(eb);
                else
                    _ = ctx.User.EmbedAsync(eb);

                return true;
            });
    }

    private async ValueTask<OneOf<OneOf.Types.Success, InsufficientTier, QuotaLimit>> AttemptRunCommand(
        ulong userId,
        ulong ownerId,
        string commandName,
        string groupName,
        string moduleName)
    {
        // try to run as a user
        var res = await AttemptRunCommand(userId, commandName, groupName, moduleName, true);

        // if it fails, try to run as an owner
        // but only     if the command is ran in a server
        //          and if the owner is not the user 
        if (!res.IsT0 && ownerId != 0 && ownerId != userId)
            res = await AttemptRunCommand(ownerId, commandName, groupName, moduleName, false);

        return res;
    }

    /// <summary>
    /// Returns either the current usage counter if limit wasn't reached, or QuotaLimit if it is.
    /// </summary>
    public async ValueTask<OneOf<(uint Hourly, uint Daily, uint Monthly), QuotaLimit>> TryIncrementQuotaCounterAsync(ulong userId,
        bool isSelf,
        FeatureType featureType,
        string featureName,
        uint? maybeHourly,
        uint? maybeDaily,
        uint? maybeMonthly)
    {
        await using var ctx = _db.GetDbContext();

        var now = DateTime.UtcNow;
        await using var tran = await ctx.Database.BeginTransactionAsync();

        var userQuotaData = await ctx.GetTable<PatronQuota>()
                                     .FirstOrDefaultAsyncLinqToDB(x => x.UserId == userId
                                                                       && x.Feature == featureName)
                            ?? new PatronQuota();

        // if hourly exists, if daily exists, etc...
        if (maybeHourly is uint hourly && userQuotaData.HourlyCount >= hourly)
        {
            return new QuotaLimit()
            {
                QuotaPeriod = QuotaPer.PerHour,
                Quota = hourly,
                // quite a neat trick. https://stackoverflow.com/a/5733560
                ResetsAt = now.Date.AddHours(now.Hour + 1),
                Feature = featureName,
                FeatureType = featureType,
                IsOwnQuota = isSelf
            };
        }

        if (maybeDaily is uint daily
            && userQuotaData.DailyCount >= daily)
        {
            return new QuotaLimit()
            {
                QuotaPeriod = QuotaPer.PerDay,
                Quota = daily,
                ResetsAt = now.Date.AddDays(1),
                Feature = featureName,
                FeatureType = featureType,
                IsOwnQuota = isSelf
            };
        }

        if (maybeMonthly is uint monthly && userQuotaData.MonthlyCount >= monthly)
        {
            return new QuotaLimit()
            {
                QuotaPeriod = QuotaPer.PerMonth,
                Quota = monthly,
                ResetsAt = now.Date.SecondOfNextMonth(),
                Feature = featureName,
                FeatureType = featureType,
                IsOwnQuota = isSelf
            };
        }

        await ctx.GetTable<PatronQuota>()
                 .InsertOrUpdateAsync(() => new()
                     {
                         UserId = userId,
                         FeatureType = featureType,
                         Feature = featureName,
                         DailyCount = 1,
                         MonthlyCount = 1,
                         HourlyCount = 1,
                     },
                     (old) => new()
                     {
                         HourlyCount = old.HourlyCount + 1,
                         DailyCount = old.DailyCount + 1,
                         MonthlyCount = old.MonthlyCount + 1,
                     },
                     () => new()
                     {
                         UserId = userId,
                         FeatureType = featureType,
                         Feature = featureName,
                     });

        await tran.CommitAsync();

        return (userQuotaData.HourlyCount + 1, userQuotaData.DailyCount + 1, userQuotaData.MonthlyCount + 1);
    }

    /// <summary>
    /// Attempts to add 1 to user's quota for the command, group and module.
    /// Input MUST BE lowercase
    /// </summary>
    /// <param name="userId">Id of the user who is attempting to run the command</param>
    /// <param name="commandName">Name of the command the user is trying to run</param>
    /// <param name="groupName">Name of the command's group</param>
    /// <param name="moduleName">Name of the command's top level module</param>
    /// <param name="isSelf">Whether this is check is for the user himself. False if it's someone else's id (owner)</param>
    /// <returns>Either a succcess (user can run the command) or one of the error values.</returns>
    private async ValueTask<OneOf<OneOf.Types.Success, InsufficientTier, QuotaLimit>> AttemptRunCommand(
        ulong userId,
        string commandName,
        string groupName,
        string moduleName,
        bool isSelf)
    {
        var confData = _pConf.Data;

        if (!confData.IsEnabled)
            return default;

        if (_creds.GetCreds().IsOwner(userId))
            return default;

        // get user tier
        var patron = await GetPatronAsync(userId);
        FeatureType quotaForFeatureType;

        if (confData.Quotas.Commands.TryGetValue(commandName, out var quotaData))
        {
            quotaForFeatureType = FeatureType.Command;
        }
        else if (confData.Quotas.Groups.TryGetValue(groupName, out quotaData))
        {
            quotaForFeatureType = FeatureType.Group;
        }
        else if (confData.Quotas.Modules.TryGetValue(moduleName, out quotaData))
        {
            quotaForFeatureType = FeatureType.Module;
        }
        else
        {
            return default;
        }

        var featureName = quotaForFeatureType switch
        {
            FeatureType.Command => commandName,
            FeatureType.Group => groupName,
            FeatureType.Module => moduleName,
            _ => throw new ArgumentOutOfRangeException(nameof(quotaForFeatureType))
        };

        if (!TryGetTierDataOrLower(quotaData, patron.Tier, out var data))
        {
            return new InsufficientTier()
            {
                Feature = featureName,
                FeatureType = quotaForFeatureType,
                RequiredTier = quotaData.Count == 0
                    ? PatronTier.ComingSoon
                    : quotaData.Keys.First(),
                UserTier = patron.Tier,
            };
        }

        // no quota limits for this tier
        if (data is null)
            return default;

        var quotaCheckResult = await TryIncrementQuotaCounterAsync(userId,
            isSelf,
            quotaForFeatureType,
            featureName,
            data.TryGetValue(QuotaPer.PerHour, out var hourly) ? hourly : null,
            data.TryGetValue(QuotaPer.PerDay, out var daily) ? daily : null,
            data.TryGetValue(QuotaPer.PerMonth, out var monthly) ? monthly : null
        );

        return quotaCheckResult.Match<OneOf<Success, InsufficientTier, QuotaLimit>>(
            _ => new Success(),
            x => x);
    }

    private bool TryGetTierDataOrLower<T>(
        IReadOnlyDictionary<PatronTier, T?> data,
        PatronTier tier,
        out T? o)
    {
        // check for quotas on this tier
        if (data.TryGetValue(tier, out o))
            return true;

        // if there are none, get the quota first tier below this one
        // which has quotas specified
        for (var i = _tiers.Length - 1; i >= 0; i--)
        {
            var lowerTier = _tiers[i];
            if (lowerTier < tier && data.TryGetValue(lowerTier, out o))
                return true;
        }

        // if there are none, that means the feature is intended
        // to be patron-only but the quotas haven't been specified yet
        // so it will be marked as "Coming Soon"
        o = default;
        return false;
    }

    public async Task<Patron> GetPatronAsync(ulong userId)
    {
        await using var ctx = _db.GetDbContext();

        // this can potentially return multiple users if the user
        // is subscribed on multiple platforms
        // or if there are multiple users on the same platform who connected the same discord account?!
        var users = await ctx.GetTable<PatronUser>()
                             .Where(x => x.UserId == userId)
                             .ToListAsync();

        // first find all active subscriptions
        // and return the one with the highest amount
        var maxActive = users.Where(x => !x.ValidThru.IsBeforeToday()).MaxBy(x => x.AmountCents);
        if (maxActive is not null)
            return PatronUserToPatron(maxActive);

        // if there are no active subs, return the one with the highest amount

        var max = users.MaxBy(x => x.AmountCents);
        if (max is null)
            return default; // no patron with that name

        return PatronUserToPatron(max);
    }

    public async Task<UserQuotaStats> GetUserQuotaStatistic(ulong userId)
    {
        var pConfData = _pConf.Data;

        if (!pConfData.IsEnabled)
            return new();

        var patron = await GetPatronAsync(userId);

        await using var ctx = _db.GetDbContext();
        var allPatronQuotas = await ctx.GetTable<PatronQuota>()
                                       .Where(x => x.UserId == userId)
                                       .ToListAsync();

        var allQuotasDict = allPatronQuotas
                            .GroupBy(static x => x.FeatureType)
                            .ToDictionary(static x => x.Key, static x => x.ToDictionary(static y => y.Feature));

        allQuotasDict.TryGetValue(FeatureType.Command, out var data);
        var userCommandQuotaStats = GetFeatureQuotaStats(patron.Tier, data, pConfData.Quotas.Commands);

        allQuotasDict.TryGetValue(FeatureType.Group, out data);
        var userGroupQuotaStats = GetFeatureQuotaStats(patron.Tier, data, pConfData.Quotas.Groups);

        allQuotasDict.TryGetValue(FeatureType.Module, out data);
        var userModuleQuotaStats = GetFeatureQuotaStats(patron.Tier, data, pConfData.Quotas.Modules);

        return new UserQuotaStats()
        {
            Tier = patron.Tier,
            Commands = userCommandQuotaStats,
            Groups = userGroupQuotaStats,
            Modules = userModuleQuotaStats,
        };
    }

    private IReadOnlyDictionary<string, FeatureQuotaStats> GetFeatureQuotaStats(
        PatronTier patronTier,
        IReadOnlyDictionary<string, PatronQuota>? allQuotasDict,
        Dictionary<string, Dictionary<PatronTier, Dictionary<QuotaPer, uint>?>> commands)
    {
        var userCommandQuotaStats = new Dictionary<string, FeatureQuotaStats>();
        foreach (var (key, quotaData) in commands)
        {
            if (TryGetTierDataOrLower(quotaData, patronTier, out var data))
            {
                // if data is null that means the quota for the user's tier is unlimited
                // no point in returning it?

                if (data is null)
                    continue;

                var (daily, hourly, monthly) = default((uint, uint, uint));
                // try to get users stats for this feature
                // if it fails just leave them at 0
                if (allQuotasDict?.TryGetValue(key, out var quota) ?? false)
                    (daily, hourly, monthly) = (quota.DailyCount, quota.HourlyCount, quota.MonthlyCount);

                userCommandQuotaStats[key] = new FeatureQuotaStats()
                {
                    Hourly = data.TryGetValue(QuotaPer.PerHour, out var hourD)
                        ? (hourly, hourD)
                        : default,
                    Daily = data.TryGetValue(QuotaPer.PerDay, out var maxD)
                        ? (daily, maxD)
                        : default,
                    Monthly = data.TryGetValue(QuotaPer.PerMonth, out var maxM)
                        ? (monthly, maxM)
                        : default,
                };
            }
        }

        return userCommandQuotaStats;
    }

    public async Task<FeatureLimit> TryGetFeatureLimitAsync(FeatureLimitKey key, ulong userId, int? defaultValue)
    {
        var conf = _pConf.Data;

        // if patron system is disabled, the quota is just default
        if (!conf.IsEnabled)
            return new()
            {
                Name = key.PrettyName,
                Quota = defaultValue,
                IsPatronLimit = false
            };
        
        
        if (!conf.Quotas.Features.TryGetValue(key.Key, out var data))
            return new()
            {
                Name = key.PrettyName,
                Quota = defaultValue,
                IsPatronLimit = false,
            };

        var patron = await GetPatronAsync(userId);
        if (!TryGetTierDataOrLower(data, patron.Tier, out var limit))
            return new()
            {
                Name = key.PrettyName,
                Quota = 0,
                IsPatronLimit = true,
            };

        return new()
        {
            Name = key.PrettyName,
            Quota = limit,
            IsPatronLimit = true
        };
    }

    // public async Task<Patron> GiftPatronAsync(IUser user, int amount)
    // {
    //     if (amount < 1)
    //         throw new ArgumentOutOfRangeException(nameof(amount));
    //     
    //     
    // }

    private Patron PatronUserToPatron(PatronUser user)
        => new Patron()
        {
            UniquePlatformUserId = user.UniquePlatformUserId,
            UserId = user.UserId,
            Amount = user.AmountCents,
            Tier = CalculateTier(user),
            PaidAt = user.LastCharge,
            ValidThru = user.ValidThru,
        };

    private PatronTier CalculateTier(PatronUser user)
    {
        if (user.ValidThru.IsBeforeToday())
            return PatronTier.None;

        return user.AmountCents switch
        {
            >= 10_000 => PatronTier.C,
            >= 5000 => PatronTier.L,
            >= 2000 => PatronTier.XX,
            >= 1000 => PatronTier.X,
            >= 500 => PatronTier.V,
            >= 100 => PatronTier.I,
            _ => PatronTier.None
        };
    }

    private async Task SendWelcomeMessage(Patron patron)
    {
        try
        {
            var user = (IUser)_client.GetUser(patron.UserId) ?? await _client.Rest.GetUserAsync(patron.UserId);
            if (user is null)
                return;

            var eb = _eb.Create()
                        .WithOkColor()
                        .WithTitle("❤️ Thank you for supporting NadekoBot! ❤️")
                        .WithDescription(
                            "Your donation has been processed and you will receive the rewards shortly.\n"
                            + "You can visit <https://www.patreon.com/join/nadekobot> to see rewards for your tier. 🎉")
                        .AddField("Tier", Format.Bold(patron.Tier.ToString()), true)
                        .AddField("Pledge", $"**{patron.Amount / 100.0f:N1}$**", true)
                        .AddField("Expires",
                            patron.ValidThru.AddDays(1).ToShortAndRelativeTimestampTag(),
                            true)
                        .AddField("Instructions",
                            @"*- Within the next **1-2 minutes** you will have all of the benefits of the Tier you've subscribed to.*
*- You can check your benefits on <https://www.patreon.com/join/nadekobot>*
*- You can use the `.patron` command in this chat to check your current quota usage for the Patron-only commands*
*- **ALL** of the servers that you **own** will enjoy your Patron benefits.*
*- You can use any of the commands available in your tier on any server (assuming you have sufficient permissions to run those commands)*
*- Any user in any of your servers can use Patron-only commands, but they will spend **your quota**, which is why it's recommended to use Nadeko's command cooldown system (.h .cmdcd) or permission system to limit the command usage for your server members.*
*- Permission guide can be found here if you're not familiar with it: <https://nadekobot.readthedocs.io/en/latest/permissions-system/>*",
                            isInline: false)
                        .WithFooter($"platform id: {patron.UniquePlatformUserId}");

            await user.EmbedAsync(eb);
        }
        catch
        {
            Log.Warning("Unable to send a \"Welcome\" message to the patron {UserId}", patron.UserId);
        }
    }

    public async Task<(int Success, int Failed)> SendMessageToPatronsAsync(PatronTier tierAndHigher, string message)
    {
        await using var ctx = _db.GetDbContext();

        var patrons = await ctx.GetTable<PatronUser>()
                 .Where(x => x.ValidThru > DateTime.UtcNow)
                 .ToArrayAsync();

        var text = SmartText.CreateFrom(message);
        
        var succ = 0;
        var fail = 0;
        foreach (var patron in patrons)
        {
            try
            {
                var user = await _client.GetUserAsync(patron.UserId);
                await user.SendAsync(text);
                ++succ;
            }
            catch
            {
                ++fail;
            }

            await Task.Delay(1000);
        }
        
        return (succ, fail);
    }

    public PatronConfigData GetConfig()
        => _pConf.Data;
}

public readonly struct FeatureLimitKey
{
    public string PrettyName { get; init; }
    public string Key { get; init; }
}