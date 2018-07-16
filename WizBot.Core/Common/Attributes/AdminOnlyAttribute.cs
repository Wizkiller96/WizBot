using System;
using System.Threading.Tasks;
using Discord.Commands;
using WizBot.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace WizBot.Common.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class AdminOnlyAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo executingCommand, IServiceProvider services)
        {
            var creds = services.GetService<IBotCredentials>();

            return Task.FromResult((creds.IsOwner(context.User) || creds.IsAdmin(context.User) || context.Client.CurrentUser.Id == context.User.Id ? PreconditionResult.FromSuccess() : PreconditionResult.FromError("Not bot owner or bot admin")));
        }
    }
}