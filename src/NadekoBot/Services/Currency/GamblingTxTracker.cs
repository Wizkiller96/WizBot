using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using NadekoBot.Common.ModuleBehaviors;
using NadekoBot.Services.Currency;
using NadekoBot.Services.Database.Models;

namespace NadekoBot.Services;

public sealed class GamblingTxTracker : ITxTracker, INService, IReadyExecutor
{
    private static readonly IReadOnlySet<string> _gamblingTypes = new HashSet<string>(new[]
    {
        "lula",
        "betroll",
        "betflip",
        "blackjack",
        "betdraw",
        "slot",
    });

    private ConcurrentDictionary<string, (decimal Bet, decimal PaidOut)> _stats = new();

    private readonly DbService _db;

    public GamblingTxTracker(DbService db)
    {
        _db = db;
    }

    public async Task OnReadyAsync()
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        while (await timer.WaitForNextTickAsync())
        {
            await using var ctx = _db.GetDbContext();
            await using var trans = await ctx.Database.BeginTransactionAsync();

            try
            {
                var keys = _stats.Keys;
                foreach (var key in keys)
                {
                    if (_stats.TryRemove(key, out var stat))
                    {
                        await ctx.GetTable<GamblingStats>()
                            .InsertOrUpdateAsync(() => new()
                            {
                                Feature = key,
                                Bet = stat.Bet,
                                PaidOut = stat.PaidOut,
                                DateAdded = DateTime.UtcNow
                            }, old => new()
                            {
                                Bet = old.Bet + stat.Bet,
                                PaidOut = old.PaidOut + stat.PaidOut,
                            }, () => new()
                            {
                                Feature = key
                            });
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred in gambling tx tracker");
            }
            finally
            {
                await trans.CommitAsync();
            }
        }
    }

    public Task TrackAdd(long amount, TxData? txData)
    {
        if (txData is null)
            return Task.CompletedTask;
        
        if (_gamblingTypes.Contains(txData.Type))
        {
            _stats.AddOrUpdate(txData.Type,
                _ => (0, amount),
                (_, old) => (old.Bet, old.PaidOut + amount));
        }

        return Task.CompletedTask;
    }

    public Task TrackRemove(long amount, TxData? txData)
    {
        if (txData is null)
            return Task.CompletedTask;
        
        if (_gamblingTypes.Contains(txData.Type))
        {
            _stats.AddOrUpdate(txData.Type,
                _ => (amount, 0),
                (_, old) => (old.Bet + amount, old.PaidOut));
        }

        return Task.CompletedTask;
    }

    public async Task<IReadOnlyCollection<GamblingStats>> GetAllAsync()
    {
        await using var ctx = _db.GetDbContext();
        return await ctx
            .GetTable<GamblingStats>()
            .ToListAsync();
    }
}