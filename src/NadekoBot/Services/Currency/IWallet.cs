namespace NadekoBot.Services.Currency;

public interface IWallet : IDisposable, IAsyncDisposable
{
    public ulong UserId { get; }
    
    public Task<long> GetBalance();
    public Task<bool> Take(long amount, Extra extra);
    public Task Add(long amount, Extra extra);
    
    public async Task<bool> Transfer(
        long amount,
        IWallet to,
        Extra extra)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be greater than 0.");

        var succ = await Take(amount,
            extra with
            {
                OtherId = to.UserId
            });

        if (!succ)
            return false;

        await to.Add(amount,
            extra with
            {
                OtherId = UserId
            });

        return true;
    }
}