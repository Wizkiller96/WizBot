using Discord.Commands;
using System;
using System.Threading.Tasks;

namespace WizBot.DataStructures
{
    public class NoPublicBot : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
#if GLOBAL_WIZBOT
            return Task.FromResult(PreconditionResult.FromError("Not available on the public bot"));
#else
            return Task.FromResult(PreconditionResult.FromSuccess());
#endif
        }
    }
}