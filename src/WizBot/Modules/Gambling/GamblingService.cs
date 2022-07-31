#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using WizBot.Common.ModuleBehaviors;
using WizBot.Db;
using WizBot.Db.Models;
using WizBot.Modules.Gambling.Common;
using WizBot.Modules.Gambling.Common.Connect4;

namespace WizBot.Modules.Gambling.Services;

public class GamblingService : INService, IReadyExecutor
{
    public ConcurrentDictionary<(ulong, ulong), RollDuelGame> Duels { get; } = new();
    public ConcurrentDictionary<ulong, Connect4Game> Connect4Games { get; } = new();
    private readonly DbService _db;
    private readonly ICurrencyService _cs;
    private readonly Bot _bot;
    private readonly DiscordSocketClient _client;
    private readonly IBotCache _cache;
    private readonly GamblingConfigService _gss;

    private static readonly TypedKey<long> _curDecayKey = new("currency:last_decay");

    public GamblingService(
        DbService db,
        Bot bot,
        ICurrencyService cs,
        DiscordSocketClient client,
        IBotCache cache,
        GamblingConfigService gss)
    {
        _db = db;
        _cs = cs;
        _bot = bot;
        _client = client;
        _cache = cache;
        _gss = gss;
    }

    public Task OnReadyAsync()
        => Task.WhenAll(CurrencyDecayLoopAsync(), TransactionClearLoopAsync());

    private async Task TransactionClearLoopAsync()
    {
        if (_bot.Client.ShardId != 0)
            return;

        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        while (await timer.WaitForNextTickAsync())
        {
            try
            {
                var lifetime = _gss.Data.Currency.TransactionsLifetime;
                if (lifetime <= 0)
                    continue;

                var now = DateTime.UtcNow;
                var days = TimeSpan.FromDays(lifetime);
                await using var uow = _db.GetDbContext();
                await uow.CurrencyTransactions
                         .DeleteAsync(ct => ct.DateAdded == null || now - ct.DateAdded < days);
            }
            catch (Exception ex)
            {
                Log.Warning(ex,
                    "An unexpected error occurred in transactions cleanup loop: {ErrorMessage}",
                    ex.Message);
            }
        }
    }
    
    private async Task CurrencyDecayLoopAsync()
    {
        if (_bot.Client.ShardId != 0)
            return;

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        while (await timer.WaitForNextTickAsync())
        {
            try
            {
                var config = _gss.Data;
                var maxDecay = config.Decay.MaxDecay;
                if (config.Decay.Percent is <= 0 or > 1 || maxDecay < 0)
                    continue;

                var now = DateTime.UtcNow;
                
                await using var uow = _db.GetDbContext();
                var result = await _cache.GetAsync(_curDecayKey);

                if (result.TryPickT0(out var bin, out _)
                    && (now - DateTime.FromBinary(bin) < TimeSpan.FromHours(config.Decay.HourInterval)))
                {
                    continue;
                }

                Log.Information(@"Decaying users' currency - decay: {ConfigDecayPercent}% 
                                    | max: {MaxDecay} 
                                    | threshold: {DecayMinTreshold}",
                    config.Decay.Percent * 100,
                    maxDecay,
                    config.Decay.MinThreshold);

                if (maxDecay == 0)
                    maxDecay = int.MaxValue;

                var decay = (double)config.Decay.Percent;
                await uow.DiscordUser
                         .Where(x => x.CurrencyAmount > config.Decay.MinThreshold && x.UserId != _client.CurrentUser.Id)
                         .UpdateAsync(old => new()
                         {
                             CurrencyAmount =
                                 maxDecay > Sql.Round((old.CurrencyAmount * decay) - 0.5)
                                     ? (long)(old.CurrencyAmount - Sql.Round((old.CurrencyAmount * decay) - 0.5))
                                     : old.CurrencyAmount - maxDecay 
                         });

                await uow.SaveChangesAsync();

                await _cache.AddAsync(_curDecayKey, now.ToBinary());
            }
            catch (Exception ex)
            {
                Log.Warning(ex,
                    "An unexpected error occurred in currency decay loop: {ErrorMessage}",
                    ex.Message);
            }
        }
    }

    private static readonly TypedKey<EconomyResult> _ecoKey = new("wizbot:economy");

    public async Task<EconomyResult> GetEconomyAsync()
    {
        var data = await _cache.GetOrAddAsync(_ecoKey,
            async () =>
            {
                await using var uow = _db.GetDbContext();
                var cash = uow.DiscordUser.GetTotalCurrency();
                var onePercent = uow.DiscordUser.GetTopOnePercentCurrency(_client.CurrentUser.Id);
                decimal planted = uow.PlantedCurrency.AsQueryable().Sum(x => x.Amount);
                var waifus = uow.WaifuInfo.GetTotalValue();
                var bot = await uow.DiscordUser.GetUserCurrencyAsync(_client.CurrentUser.Id);
                decimal bank = await uow.GetTable<BankUser>()
                                        .SumAsyncLinqToDB(x => x.Balance);

                var result = new EconomyResult
                {
                    Cash = cash,
                    Planted = planted,
                    Bot = bot,
                    Waifus = waifus,
                    OnePercent = onePercent,
                    Bank = bank
                };

                return result;
            },
            TimeSpan.FromMinutes(3));

        return data;
    }


    private static readonly SemaphoreSlim _timelyLock = new (1, 1);

    private static TypedKey<Dictionary<ulong, long>> _timelyKey
        = new("timely:claims");
    public async Task<TimeSpan?> ClaimTimelyAsync(ulong userId, int period)
    {
        if (period == 0)
            return null;

        await _timelyLock.WaitAsync();
        try
        {
            // get the dictionary from the cache or get a new one
            var dict = (await _cache.GetOrAddAsync(_timelyKey,
                () => Task.FromResult(new Dictionary<ulong, long>())))!;

            var now = DateTime.UtcNow;
            var nowB = now.ToBinary();

            // try to get users last claim
            if (!dict.TryGetValue(userId, out var lastB))
                lastB = dict[userId] = now.ToBinary();

            var diff = now - DateTime.FromBinary(lastB);

            // if its now, or too long ago => success
            if (lastB == nowB || diff > period.Hours())
            {
                // update the cache
                dict[userId] = nowB;
                await _cache.AddAsync(_timelyKey, dict);
                
                return null;
            }
            else
            {
                // otherwise return the remaining time
                return period.Hours() - diff;
            }
        }
        finally
        {
            _timelyLock.Release();
        }
    }

    public async Task RemoveAllTimelyClaimsAsync()
        => await _cache.RemoveAsync(_timelyKey);
}