#nullable disable
using LinqToDB;
using NadekoBot.Services.Currency;

namespace NadekoBot.Services;

public sealed class CurrencyService : ICurrencyService, INService
{
    private readonly DbService _db;
    private readonly ITxTracker _txTracker;

    public CurrencyService(DbService db, ITxTracker txTracker)
    {
        _db = db;
        _txTracker = txTracker;
    }

    public Task<IWallet> GetWalletAsync(ulong userId, CurrencyType type = CurrencyType.Default)
    {
        if (type == CurrencyType.Default)
            return Task.FromResult<IWallet>(new DefaultWallet(userId, _db));

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
            foreach (var userId in userIds)
            {
                var wallet = await GetWalletAsync(userId);
                await wallet.Add(amount, txData);
            }

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
            await ctx.SaveChangesAsync();
            return;
        }

        throw new ArgumentOutOfRangeException(nameof(type));
    }

    public async Task AddAsync(
        ulong userId,
        long amount,
        TxData txData)
    {
        var wallet = await GetWalletAsync(userId);
        await wallet.Add(amount, txData);
        await _txTracker.TrackAdd(amount, txData);
    }

    public async Task AddAsync(
        IUser user,
        long amount,
        TxData txData)
        => await AddAsync(user.Id, amount, txData);

    public async Task<bool> RemoveAsync(
        ulong userId,
        long amount,
        TxData txData)
    {
        var wallet = await GetWalletAsync(userId);
        var result = await wallet.Take(amount, txData);
        if(result) 
            await _txTracker.TrackRemove(amount, txData);
        return result;
    }

    public async Task<bool> RemoveAsync(
        IUser user,
        long amount,
        TxData txData)
        => await RemoveAsync(user.Id, amount, txData);
}