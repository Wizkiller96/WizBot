#nullable disable
using LinqToDB;
using WizBot.Services.Currency;

namespace WizBot.Services;

public class CurrencyService : ICurrencyService, INService
{
    private readonly DbService _db;

    public CurrencyService(DbService db)
        => _db = db;

    public Task<IWallet> GetWalletAsync(ulong userId, CurrencyType type = CurrencyType.Default)
    {
        if (type == CurrencyType.Default)
            return Task.FromResult<IWallet>(new DefaultWallet(userId, _db.GetDbContext()));

        throw new ArgumentOutOfRangeException(nameof(type));
    }

    public async Task AddBulkAsync(
        IReadOnlyCollection<ulong> userIds,
        long amount,
        TxData txData,
        CurrencyType type = CurrencyType.Default)
    {
        if (type == CurrencyType.Default)
        {
            await using var ctx = _db.GetDbContext();
            foreach (var userId in userIds)
            {
                var wallet = new DefaultWallet(userId, ctx);
                await wallet.Add(amount, txData);
            }

            await ctx.SaveChangesAsync();
            return;
        }

        throw new ArgumentOutOfRangeException(nameof(type));
    }

    public async Task RemoveBulkAsync(
        IReadOnlyCollection<ulong> userIds,
        long amount,
        TxData txData,
        CurrencyType type = CurrencyType.Default)
    {
        if (type == CurrencyType.Default)
        {
            await using var ctx = _db.GetDbContext();
            await ctx.DiscordUser
                     .Where(x => userIds.Contains(x.UserId))
                     .UpdateAsync(du => new()
                     {
                         CurrencyAmount = du.CurrencyAmount >= amount
                             ? du.CurrencyAmount - amount
                             : 0
                     });
            return;
        }

        throw new ArgumentOutOfRangeException(nameof(type));
    }

    public async Task AddAsync(
        ulong userId,
        long amount,
        TxData txData)
    {
        await using var wallet = await GetWalletAsync(userId);
        await wallet.Add(amount, txData);
    }

    public async Task AddAsync(
        IUser user,
        long amount,
        TxData txData)
    {
        await using var wallet = await GetWalletAsync(user.Id);
        await wallet.Add(amount, txData);
    }

    public async Task<bool> RemoveAsync(
        ulong userId,
        long amount,
        TxData txData)
    {
        await using var wallet = await GetWalletAsync(userId);
        return await wallet.Take(amount, txData);
    }

    public async Task<bool> RemoveAsync(
        IUser user,
        long amount,
        TxData txData)
    {
        await using var wallet = await GetWalletAsync(user.Id);
        return await wallet.Take(amount, txData);
    }
}