using WizBot.Services.Currency;

namespace WizBot.Services;

public interface ITxTracker
{
    Task TrackAdd(long amount, TxData txData);
    Task TrackRemove(long amount, TxData txData);
}