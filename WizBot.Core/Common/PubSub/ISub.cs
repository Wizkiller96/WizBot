using System;
using System.Threading.Tasks;

namespace WizBot.Core.Common
{
    public interface ISub
    {
        public Task Sub<TData>(in TypedKey<TData> key, Func<TData, Task> action);
    }
}