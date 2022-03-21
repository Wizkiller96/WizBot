using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using NadekoBot.Services.Database;
using NadekoBot.Services.Database.Models;

namespace NadekoBot.Services.Currency;

public class DefaultWallet : IWallet
{
    private readonly DbService _db;
    public ulong UserId { get; }

    public DefaultWallet(ulong userId, DbService db)
    {
        UserId = userId;
        _db = db;
    }

    public async Task<long> GetBalance()
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.DiscordUser
                        .ToLinqToDBTable()
                        .Where(x => x.UserId == UserId)
                        .Select(x => x.CurrencyAmount)
                        .FirstOrDefaultAsync();
    }

    public async Task<bool> Take(long amount, TxData txData)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount to take must be non negative.");

        await using var ctx = _db.GetDbContext();

        var changed = await ctx.DiscordUser
                               .Where(x => x.UserId == UserId && x.CurrencyAmount >= amount)
                               .UpdateAsync(x => new()
                               {
                                   CurrencyAmount = x.CurrencyAmount - amount
                               });

        if (changed == 0)
            return false;

        await ctx
              .GetTable<CurrencyTransaction>()
              .InsertAsync(() => new()
              {
                  Amount = -amount,
                  Note = txData.Note,
                  UserId = UserId,
                  Type = txData.Type,
                  Extra = txData.Extra,
                  OtherId = txData.OtherId,
                  DateAdded = DateTime.UtcNow
              });

        return true;
    }

    public async Task Add(long amount, TxData txData)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be greater than 0.");

        await using var ctx = _db.GetDbContext();
        
        await using (var tran = await ctx.Database.BeginTransactionAsync())
        {
            var changed = await ctx.DiscordUser
                                    .Where(x => x.UserId == UserId)
                                    .UpdateAsync(x => new()
                                    {
                                        CurrencyAmount = x.CurrencyAmount + amount
                                    });

            if (changed == 0)
            {
                await ctx.DiscordUser
                          .ToLinqToDBTable()
                          .Value(x => x.UserId, UserId)
                          .Value(x => x.Username, "Unknown")
                          .Value(x => x.Discriminator, "????")
                          .Value(x => x.CurrencyAmount, amount)
                          .InsertAsync();
            }

            await tran.CommitAsync();
        }

        await ctx.GetTable<CurrencyTransaction>()
                 .InsertAsync(() => new()
                 {
                     Amount = amount,
                     UserId = UserId,
                     Note = txData.Note,
                     Type = txData.Type,
                     Extra = txData.Extra,
                     OtherId = txData.OtherId,
                     DateAdded = DateTime.UtcNow
                 });
    }
}