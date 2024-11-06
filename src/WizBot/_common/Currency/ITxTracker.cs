using WizBot.Services.Currency;

namespace WizBot.Services;

public interface ITxTracker
{
    Task TrackAdd(ulong userId, long amount, TxData? txData);
    Task TrackRemove(ulong userId, long amount, TxData? txData);
}