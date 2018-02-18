using System;
using System.Threading.Tasks;

namespace WizBot.Modules.Gambling.Common
{
    public interface ICurrencyEvent
    {
        event Func<ulong, Task> OnEnded;
        Task Stop();
        Task Start();
    }
}
