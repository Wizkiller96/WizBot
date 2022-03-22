using WizBot.Services.Currency;

namespace WizBot.Services;

public static class CurrencyServiceExtensions
{
    public static async Task<long> GetBalanceAsync(this ICurrencyService cs, ulong userId)
    {
        var wallet = await cs.GetWalletAsync(userId);
        return await wallet.GetBalance();
    }
    
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