using NadekoBot.Services.Currency;

#nullable disable
namespace NadekoBot.Services;

public interface ICurrencyService
{
    Task<IWallet> GetWalletAsync(ulong userId, CurrencyType type = CurrencyType.Default);

    Task AddBulkAsync(
        IReadOnlyCollection<ulong> userIds,
        long amount,
        Extra extra,
        CurrencyType type = CurrencyType.Default);

    Task RemoveBulkAsync(
        IReadOnlyCollection<ulong> userIds,
        long amount,
        Extra extra,
        CurrencyType type = CurrencyType.Default);

    Task AddAsync(
        ulong userId,
        long amount,
        Extra extra);

    Task AddAsync(
        IUser user,
        long amount,
        Extra extra);

    Task<bool> RemoveAsync(
        ulong userId,
        long amount,
        Extra extra);

    Task<bool> RemoveAsync(
        IUser user,
        long amount,
        Extra extra);

    Task<bool> TransferAsync(
        ulong from,
        ulong to,
        long amount,
        string note);
}