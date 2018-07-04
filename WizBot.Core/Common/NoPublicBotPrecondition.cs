using System;
using System.Threading.Tasks;
using Discord.Commands;

namespace WizBot.Common
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class NoPublicBotAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
#if GLOBAL_WIZBOT
            return Task.FromResult(PreconditionResult.FromError("Not available on the public bot"));
#else
            return Task.FromResult(PreconditionResult.FromSuccess());
#endif
        }
    }
}
