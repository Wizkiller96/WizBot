using WizBot.Services.Currency;

#nullable disable
namespace WizBot.Services;

public interface ICurrencyService
{
    Task<IWallet> GetWalletAsync(ulong userId, CurrencyType type = CurrencyType.Default);

    Task AddBulkAsync(
        IReadOnlyCollection<ulong> userIds,
        long amount,
        TxData txData,
        CurrencyType type = CurrencyType.Default);

    Task RemoveBulkAsync(
        IReadOnlyCollection<ulong> userIds,
        long amount,
        TxData txData,
        CurrencyType type = CurrencyType.Default);

    Task AddAsync(
        ulong userId,
        long amount,
        TxData txData);

    Task AddAsync(
        IUser user,
        long amount,
        TxData txData);

    Task<bool> RemoveAsync(
        ulong userId,
        long amount,
        TxData txData);

    Task<bool> RemoveAsync(
        IUser user,
        long amount,
        TxData txData);

    Task<bool> TransferAsync(
        ulong from,
        ulong to,
        long amount,
        string fromName,
        string note);
}