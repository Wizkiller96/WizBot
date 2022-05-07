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
        IEmbedBuilderService ebs,
        IUser from,
        IUser to,
        long amount,
        string? note,
        string formattedAmount)
    {
        var fromWallet = await cs.GetWalletAsync(from.Id);
        var toWallet = await cs.GetWalletAsync(to.Id);

        var extra = new TxData("gift", from.ToString()!, note, from.Id);

        if (await fromWallet.Transfer(amount, toWallet, extra))
        {
            await to.SendConfirmAsync(ebs,
                string.IsNullOrWhiteSpace(note)
                    ? $"Received {formattedAmount} from {from} "
                    : $"Received {formattedAmount} from {from}: {note}");
            return true;
        }

        return false;
    }
}