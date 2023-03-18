#nullable disable
using NadekoBot.Modules.Gambling.Common;

namespace NadekoBot.Modules.Gambling.Services;

public class CurrencyRaffleService : INService
{
    public enum JoinErrorType
    {
        NotEnoughCurrency,
        AlreadyJoinedOrInvalidAmount
    }

    public Dictionary<ulong, CurrencyRaffleGame> Games { get; } = new();
    private readonly SemaphoreSlim _locker = new(1, 1);
    private readonly ICurrencyService _cs;

    public CurrencyRaffleService(ICurrencyService cs)
        => _cs = cs;

    public async Task<(CurrencyRaffleGame, JoinErrorType?)> JoinOrCreateGame(
        ulong channelId,
        IUser user,
        long amount,
        bool mixed,
        Func<IUser, long, Task> onEnded)
    {
        await _locker.WaitAsync();
        try
        {
            var newGame = false;
            if (!Games.TryGetValue(channelId, out var crg))
            {
                newGame = true;
                crg = new(mixed ? CurrencyRaffleGame.Type.Mixed : CurrencyRaffleGame.Type.Normal);
                Games.Add(channelId, crg);
            }

            //remove money, and stop the game if this 
            // user created it and doesn't have the money
            if (!await _cs.RemoveAsync(user.Id, amount, new("raffle", "join")))
            {
                if (newGame)
                    Games.Remove(channelId);
                return (null, JoinErrorType.NotEnoughCurrency);
            }

            if (!crg.AddUser(user, amount))
            {
                await _cs.AddAsync(user.Id, amount, new("raffle", "refund"));
                return (null, JoinErrorType.AlreadyJoinedOrInvalidAmount);
            }

            if (newGame)
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(60000);
                    await _locker.WaitAsync();
                    try
                    {
                        var winner = crg.GetWinner();
                        var won = crg.Users.Sum(x => x.Amount);

                        await _cs.AddAsync(winner.DiscordUser.Id, won, new("raffle", "win"));
                        Games.Remove(channelId, out _);
                        _ = onEnded(winner.DiscordUser, won);
                    }
                    catch { }
                    finally { _locker.Release(); }
                });
            }

            return (crg, null);
        }
        finally
        {
            _locker.Release();
        }
    }
}