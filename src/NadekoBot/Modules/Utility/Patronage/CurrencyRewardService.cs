#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using NadekoBot.Modules.Utility.Patronage;
using NadekoBot.Modules.Gambling.Bank;
using NadekoBot.Modules.Gambling.Services;
using NadekoBot.Services.Currency;
using NadekoBot.Services.Database.Models;

namespace NadekoBot.Modules.Utility;

public class CurrencyRewardService : INService, IDisposable
{
    private readonly ICurrencyService _cs;
    private readonly IPatronageService _ps;
    private readonly DbService _db;
    private readonly IBankService _bs;
    private readonly IEmbedBuilderService _eb;
    private readonly GamblingConfigService _config;
    private readonly DiscordSocketClient _client;

    public CurrencyRewardService(
        ICurrencyService cs,
        IPatronageService ps,
        DbService db,
        IBankService bs,
        IEmbedBuilderService eb,
        GamblingConfigService config,
        DiscordSocketClient client)
    {
        _cs = cs;
        _ps = ps;
        _db = db;
        _bs = bs;
        _eb = eb;
        _config = config;
        _client = client;

        _ps.OnNewPatronPayment += OnNewPayment;
        _ps.OnPatronRefunded += OnPatronRefund;
        _ps.OnPatronUpdated += OnPatronUpdate;
    }

    public void Dispose()
    {
        _ps.OnNewPatronPayment -= OnNewPayment;
        _ps.OnPatronRefunded -= OnPatronRefund;
        _ps.OnPatronUpdated -= OnPatronUpdate;
    }

    private async Task OnPatronUpdate(Patron oldPatron, Patron newPatron)
    {
        // if pledge was increased
        if (oldPatron.Amount < newPatron.Amount)
        {
            var conf = _config.Data;
            var newAmount = (long)(newPatron.Amount * conf.PatreonCurrencyPerCent);

            RewardedUser old;
            await using (var ctx = _db.GetDbContext())
            {
                old = await ctx.GetTable<RewardedUser>()
                         .Where(x => x.PlatformUserId == newPatron.UniquePlatformUserId)
                         .FirstOrDefaultAsync();
                
                if (old is null)
                {
                    await OnNewPayment(newPatron);
                    return;
                }
                
                // no action as the amount is the same or lower
                if (old.AmountRewardedThisMonth >= newAmount)
                    return;
                
                var count = await ctx.GetTable<RewardedUser>()
                                  .Where(x => x.PlatformUserId == newPatron.UniquePlatformUserId)
                                  .UpdateAsync(_ => new()
                                  {
                                      PlatformUserId = newPatron.UniquePlatformUserId,
                                      UserId = newPatron.UserId,
                                      // amount before bonuses
                                      AmountRewardedThisMonth = newAmount,
                                      LastReward = newPatron.PaidAt
                                  });

                // shouldn't ever happen
                if (count == 0)
                    return;
            }

            var oldAmount = old.AmountRewardedThisMonth;

            var realNewAmount = GetRealCurrencyReward(
                (int)(newAmount / conf.PatreonCurrencyPerCent),
                newAmount,
                out var percentBonus);
            
            var realOldAmount = GetRealCurrencyReward(
                (int)(oldAmount / conf.PatreonCurrencyPerCent),
                oldAmount,
                out _);
            
            var diff = realNewAmount - realOldAmount;
            if (diff <= 0)
                return; // no action if new is lower

            // if the user pledges 5$ or more, they will get X % more flowers where X is amount in dollars,
            // up to 100%
            
            await _cs.AddAsync(newPatron.UserId, diff, new TxData("patron","update"));
            
            _ = SendMessageToUser(newPatron.UserId,
                $"You've received an additional **{diff}**{_config.Data.Currency.Sign} as a currency reward (+{percentBonus}%)!");
        }
    }

    private long GetRealCurrencyReward(int pledgeCents, long modifiedAmount, out int percentBonus)
    {
        // needs at least 5$ to be eligible for a bonus
        if (pledgeCents < 500)
        {
            percentBonus = 0;
            return modifiedAmount;
        }

        var dollarValue = pledgeCents / 100;
        percentBonus = dollarValue switch
        {
            >= 100 => 100,
            >= 50 => 50,
            >= 20 => 20,
            >= 10 => 10,
            >= 5 => 5,
            _ => 0
        };
        return (long)(modifiedAmount * (1 + (percentBonus / 100.0f)));
    }

    // on a new payment, always give the full amount.
    private async Task OnNewPayment(Patron patron)
    {
        var amount = (long)(patron.Amount * _config.Data.PatreonCurrencyPerCent);
        await using var ctx = _db.GetDbContext();
        await ctx.GetTable<RewardedUser>()
                 .InsertOrUpdateAsync(() => new()
                     {
                         PlatformUserId = patron.UniquePlatformUserId,
                         UserId = patron.UserId,
                         AmountRewardedThisMonth = amount,
                         LastReward = patron.PaidAt,
                     },
                     old => new()
                     {
                         AmountRewardedThisMonth = amount,
                         UserId = patron.UserId,
                         LastReward = patron.PaidAt
                     },
                     () => new()
                     {
                         PlatformUserId = patron.UniquePlatformUserId
                     });
        
        var realAmount = GetRealCurrencyReward(patron.Amount, amount, out var percentBonus);
        await _cs.AddAsync(patron.UserId, realAmount, new("patron", "new"));
        _ = SendMessageToUser(patron.UserId,
            $"You've received **{realAmount}**{_config.Data.Currency.Sign} as a currency reward (**+{percentBonus}%**)!");
    }

    private async Task SendMessageToUser(ulong userId, string message)
    {
        try
        {
            var user = (IUser)_client.GetUser(userId) ?? await _client.Rest.GetUserAsync(userId);
            if (user is null)
                return;

            var eb = _eb.Create()
                        .WithOkColor()
                        .WithDescription(message);
            
            await user.EmbedAsync(eb);
        }
        catch
        {
            Log.Warning("Unable to send a \"Currency Reward\" message to the patron {UserId}", userId);
        }
    }

    private async Task OnPatronRefund(Patron patron)
    {
        await using var ctx = _db.GetDbContext();
        _ = await ctx.GetTable<RewardedUser>()
                     .UpdateAsync(old => new()
                     {
                         AmountRewardedThisMonth = old.AmountRewardedThisMonth * 2
                     });
    }
}