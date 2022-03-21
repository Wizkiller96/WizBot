using NadekoBot.Services.Currency;

namespace NadekoBot.Services;

public static class CurrencyServiceExtensions
{
    public static async Task<long> GetBalanceAsync(this ICurrencyService cs, ulong userId)
    {
        var wallet = await cs.GetWalletAsync(userId);
        return await wallet.GetBalance();
    }
    
    // todo transfer should be a transaction
    public static async Task<bool> TransferAsync(
        this ICurrencyService cs,
        ulong fromId,
        ulong toId,
        long amount,
        string fromName,
        string note)
    {
        var fromWallet = await cs.GetWalletAsync(fromId);
        var toWallet = await cs.GetWalletAsync(toId);

        var extra = new TxData("gift", fromName, note, fromId);

        return await fromWallet.Transfer(amount, toWallet, extra);
    }
}