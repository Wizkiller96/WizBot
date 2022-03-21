#nullable disable
using LinqToDB;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Common.ModuleBehaviors;
using NadekoBot.Db;
using NadekoBot.Modules.Gambling.Common;
using NadekoBot.Modules.Gambling.Common.Connect4;
using NadekoBot.Modules.Gambling.Common.Slot;
using NadekoBot.Modules.Gambling.Common.WheelOfFortune;
using Newtonsoft.Json;

namespace NadekoBot.Modules.Gambling.Services;

public class GamblingService : INService, IReadyExecutor
{
    public ConcurrentDictionary<(ulong, ulong), RollDuelGame> Duels { get; } = new();
    public ConcurrentDictionary<ulong, Connect4Game> Connect4Games { get; } = new();
    private readonly DbService _db;
    private readonly ICurrencyService _cs;
    private readonly Bot _bot;
    private readonly DiscordSocketClient _client;
    private readonly IDataCache _cache;
    private readonly GamblingConfigService _gss;

    public GamblingService(
        DbService db,
        Bot bot,
        ICurrencyService cs,
        DiscordSocketClient client,
        IDataCache cache,
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

                await using var uow = _db.GetDbContext();
                var lastCurrencyDecay = _cache.GetLastCurrencyDecay();

                if (DateTime.UtcNow - lastCurrencyDecay < TimeSpan.FromHours(config.Decay.HourInterval))
                    continue;

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

                _cache.SetLastCurrencyDecay();
                await uow.SaveChangesAsync();
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

    public EconomyResult GetEconomy()
    {
        if (_cache.TryGetEconomy(out var data))
        {
            try
            {
                return JsonConvert.DeserializeObject<EconomyResult>(data);
            }
            catch { }
        }

        decimal cash;
        decimal onePercent;
        decimal planted;
        decimal waifus;
        long bot;

        using (var uow = _db.GetDbContext())
        {
            cash = uow.DiscordUser.GetTotalCurrency();
            onePercent = uow.DiscordUser.GetTopOnePercentCurrency(_client.CurrentUser.Id);
            planted = uow.PlantedCurrency.AsQueryable().Sum(x => x.Amount);
            waifus = uow.WaifuInfo.GetTotalValue();
            bot = uow.DiscordUser.GetUserCurrency(_client.CurrentUser.Id);
        }

        var result = new EconomyResult
        {
            Cash = cash,
            Planted = planted,
            Bot = bot,
            Waifus = waifus,
            OnePercent = onePercent
        };

        _cache.SetEconomy(JsonConvert.SerializeObject(result));
        return result;
    }

    public Task<WheelOfFortuneGame.Result> WheelOfFortuneSpinAsync(ulong userId, long bet)
        => new WheelOfFortuneGame(userId, bet, _gss.Data, _cs).SpinAsync();


    public struct EconomyResult
    {
        public decimal Cash { get; set; }
        public decimal Planted { get; set; }
        public decimal Waifus { get; set; }
        public decimal OnePercent { get; set; }
        public long Bot { get; set; }
    }
}