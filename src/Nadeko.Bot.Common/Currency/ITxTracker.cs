using NadekoBot.Services.Currency;

namespace NadekoBot.Services;

public interface ITxTracker
{
    Task TrackAdd(long amount, TxData? txData);
    Task TrackRemove(long amount, TxData? txData);
}