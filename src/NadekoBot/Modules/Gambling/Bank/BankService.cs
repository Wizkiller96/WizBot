using LinqToDB;
using LinqToDB.EntityFrameworkCore;

namespace NadekoBot.Modules.Gambling.Bank;

public sealed class BankService : IBankService, INService
{
    private readonly ICurrencyService _cur;
    private readonly DbService _db;

    public BankService(ICurrencyService cur, DbService db)
    {
        _cur = cur;
        _db = db;
    }

    public async Task<bool> DepositAsync(ulong userId, long amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount));
        
        if (!await _cur.RemoveAsync(userId, amount, new("bank", "deposit")))
            return false;

        await using var ctx = _db.GetDbContext();
        await ctx.BankUsers
                 .ToLinqToDBTable()
                 .InsertOrUpdateAsync(() => new()
                     {
                         UserId = userId,
                         Balance = amount
                     },
                     (old) => new()
                     {
                         Balance = old.Balance + amount
                     },
                     () => new()
                     {
                         UserId = userId
                     });

        return true;
    }

    public async Task<bool> WithdrawAsync(ulong userId, long amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount));
        
        await using var ctx = _db.GetDbContext();
        var rows = await ctx.BankUsers
                 .ToLinqToDBTable()
                 .Where(x => x.UserId == userId && x.Balance >= amount)
                 .UpdateAsync((old) => new()
                 {
                     Balance = old.Balance - amount
                 });

        if (rows > 0)
        {
            await _cur.AddAsync(userId, amount, new("bank", "withdraw"));
            return true;
        }

        return false;
    }

    public async Task<long> GetBalanceAsync(ulong userId)
    {
        await using var ctx = _db.GetDbContext();
        return (await ctx.BankUsers
                         .ToLinqToDBTable()
                         .FirstOrDefaultAsync(x => x.UserId == userId))
               ?.Balance
               ?? 0;
    }
}