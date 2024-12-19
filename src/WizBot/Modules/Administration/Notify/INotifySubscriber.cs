namespace WizBot.Modules.Administration;

public interface INotifySubscriber
{
    Task NotifyAsync<T>(T data, bool isShardLocal = false)
        where T : struct, INotifyModel;
}