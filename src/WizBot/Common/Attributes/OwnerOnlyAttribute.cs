using System;
using System.Threading.Tasks;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using WizBot.Extensions;
using WizBot.Services;

namespace WizBot.Common.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class OwnerOnlyAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo executingCommand, IServiceProvider services)
        {
            var creds = services.GetRequiredService<BotCredsProvider>().GetCreds();

            return Task.FromResult((creds.IsOwner(context.User) || context.Client.CurrentUser.Id == context.User.Id ? PreconditionResult.FromSuccess() : PreconditionResult.FromError("Not owner")));
        }
    }
}