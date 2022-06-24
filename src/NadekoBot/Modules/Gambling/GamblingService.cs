#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using NadekoBot.Common.ModuleBehaviors;
using NadekoBot.Db;
using NadekoBot.Db.Models;
using NadekoBot.Modules.Gambling.Common;
using NadekoBot.Modules.Gambling.Common.Connect4;
using NadekoBot.Modules.Gambling.Common.Slot;
using NadekoBot.Modules.Gambling.Common.WheelOfFortune;

namespace NadekoBot.Modules.Gambling.Services;

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

    private static readonly TypedKey<long> _curDecayKey = new("currency:last_decay");
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

    public async Task<SlotResponse> SlotAsync(ulong userId, long amount)
    {
        var takeRes = await _cs.RemoveAsync(userId, amount, new("slot", "bet"));

        if (!takeRes)
        {
            return new()
            {
                Error = GamblingError.NotEnough
            };
        }

        var game = new SlotGame();
        var result = game.Spin();
        long won = 0;

        if (result.Multiplier > 0)
        {
            won = (long)(result.Multiplier * amount);

            await _cs.AddAsync(userId, won, new("slot", "win", $"Slot Machine x{result.Multiplier}"));
        }

        var toReturn = new SlotResponse
        {
            Multiplier = result.Multiplier,
            Won = won
        };

        toReturn.Rolls.AddRange(result.Rolls);

        return toReturn;
    }

    private static readonly TypedKey<EconomyResult> _ecoKey = new("nadeko:economy");

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
                var bot = uow.DiscordUser.GetUserCurrency(_client.CurrentUser.Id);
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

    public Task<WheelOfFortuneGame.Result> WheelOfFortuneSpinAsync(ulong userId, long bet)
        => new WheelOfFortuneGame(userId, bet, _gss.Data, _cs).SpinAsync();


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


    public readonly struct EconomyResult
    {
        public decimal Cash { get; init; }
        public decimal Planted { get; init; }
        public decimal Waifus { get; init; }
        public decimal OnePercent { get; init; }
        public decimal Bank { get; init; }
        public long Bot { get; init; }
    }
}