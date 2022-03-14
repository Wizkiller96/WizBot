using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using WizBot.Services.Database;
using WizBot.Services.Database.Models;

namespace WizBot.Services.Currency;

public class DefaultWallet : IWallet
{
    public ulong UserId { get; }

    private readonly WizBotContext _ctx;

    public DefaultWallet(ulong userId, WizBotContext ctx)
    {
        UserId = userId;
        _ctx = ctx;
    }

    public Task<long> GetBalance()
        => _ctx.DiscordUser
               .ToLinqToDBTable()
               .Where(x => x.UserId == UserId)
               .Select(x => x.CurrencyAmount)
               .FirstOrDefaultAsync();

    public async Task<bool> Take(long amount, TxData txData)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount to take must be non negative.");

        var changed = await _ctx.DiscordUser
                                .Where(x => x.UserId == UserId && x.CurrencyAmount >= amount)
                                .UpdateAsync(x => new()
                                {
                                    CurrencyAmount = x.CurrencyAmount - amount
                                });

        if (changed == 0)
            return false;

        await _ctx.CreateLinqToDbContext()
                  .InsertAsync(new CurrencyTransaction()
                  {
                      Amount = -amount,
                      Note = txData.Note,
                      UserId = UserId,
                      Type = txData.Type,
                      Extra = txData.Extra,
                      OtherId = txData.OtherId
                  });

        return true;
    }

    public async Task Add(long amount, TxData txData)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be greater than 0.");

        await using (var tran = await _ctx.Database.BeginTransactionAsync())
        {
            var changed = await _ctx.DiscordUser
                                    .Where(x => x.UserId == UserId)
                                    .UpdateAsync(x => new()
                                    {
                                        CurrencyAmount = x.CurrencyAmount + amount
                                    });

            if (changed == 0)
            {
                await _ctx.DiscordUser
                          .ToLinqToDBTable()
                          .Value(x => x.UserId, UserId)
                          .Value(x => x.Username, "Unknown")
                          .Value(x => x.Discriminator, "????")
                          .Value(x => x.CurrencyAmount, amount)
                          .InsertAsync();
            }

            await tran.CommitAsync();
        }

        var ct = new CurrencyTransaction()
        {
            Amount = amount,
            UserId = UserId,
            Note = txData.Note,
            Type = txData.Type,
            Extra = txData.Extra,
            OtherId = txData.OtherId
        };

        await _ctx.CreateLinqToDbContext()
                  .InsertAsync(ct);
    }

    public void Dispose()
    {
        _ctx.SaveChanges();
        _ctx.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await _ctx.SaveChangesAsync();
        await _ctx.DisposeAsync();
    }
}