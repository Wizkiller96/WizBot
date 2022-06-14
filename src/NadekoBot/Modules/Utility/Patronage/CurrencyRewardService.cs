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
        if (oldPatron.Amount != newPatron.Amount)
        {
            var conf = _config.Data;

            var newAmount = (long)(Math.Max(newPatron.Amount, oldPatron.Amount) * conf.PatreonCurrencyPerCent);
            UpdateOutput<RewardedUser>[] output;
            await using (var ctx = _db.GetDbContext())
            {
                output = await ctx.GetTable<RewardedUser>()
                                  .Where(x => x.PlatformUserId == newPatron.UnqiuePlatformUserId)
                                  .UpdateWithOutputAsync(old => new()
                                  {
                                      PlatformUserId = newPatron.UnqiuePlatformUserId,
                                      UserId = newPatron.UserId,
                                      // amount before bonuses
                                      AmountRewardedThisMonth = newAmount,
                                      LastReward = newPatron.PaidAt
                                  });
            }

            // if the user wasn't previously in the db for some reason,
            // we will treat him as a new patron
            if (output.Length == 0)
            {
                await OnNewPayment(newPatron);
                return;
            }

            var oldAmount = output[0].Deleted.AmountRewardedThisMonth;

            var diff = newAmount - oldAmount;
            if (diff <= 0)
                return; // no action if new is lower

            // if the user pledges 5$ or more, they will get X % more flowers where X is amount in dollars,
            // up to 100%

            var realAmount = GetRealCurrencyReward(newPatron.Amount, diff, out var percentBonus);
            await _cs.AddAsync(newPatron.UserId, realAmount, new TxData("patron","update"));
            
            _ = SendMessageToUser(newPatron.UserId,
                $"You've received an additional **{realAmount}**{_config.Data.Currency.Sign} as a currency reward (+{percentBonus}%)!");
        }
    }

    private long GetRealCurrencyReward(int fullPledge, long currentAmount, out int percentBonus)
    {
        // needs at least 5$ to be eligible for a bonus
        if (fullPledge < 500)
        {
            percentBonus = 0;
            return currentAmount;
        }

        var dollarValue = fullPledge / 100;
        percentBonus = dollarValue switch
        {
            > 100 => 100,
            _ => dollarValue
        };
        return (long)(currentAmount * (1 + (percentBonus / 100.0f)));
    }

    // on a new payment, always give the full amount.
    private async Task OnNewPayment(Patron patron)
    {
        var amount = (long)(patron.Amount * _config.Data.PatreonCurrencyPerCent);
        await using var ctx = _db.GetDbContext();
        await ctx.GetTable<RewardedUser>()
                 .InsertOrUpdateAsync(() => new()
                     {
                         PlatformUserId = patron.UnqiuePlatformUserId,
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
                         PlatformUserId = patron.UnqiuePlatformUserId
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
                     .UpdateWithOutputAsync(old => new()
                     {
                         AmountRewardedThisMonth = old.AmountRewardedThisMonth * 2
                     });

        // var toTake = old.Length == 0
        //     ? patron.Amount
        //     : old[0].Inserted.AmountRewardedThisMonth;

        // if (toTake > 0)
        // {
        //     Log.Warning("Wiping the wallet and bank of the user {UserId} due to a refund/fraud...",
        //         patron.UserId);
        //     await _cs.RemoveAsync(patron.UserId, patron.Amount, new("patreon", "refund"));
        //     await _bs.BurnAllAsync(patron.UserId);
        //     Log.Warning("Burned {Amount} currency from the bank of the user {UserId} due to a refund/fraud.",
        //         patron.Amount,
        //         patron.UserId);
        // }
    }
}