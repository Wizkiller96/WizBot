using System;
using System.Threading.Tasks;
using Discord.Commands;
using WizBot.Core.Services;

namespace WizBot.Common.Attributes
{
    public class AdminOnlyAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo executingCommand, IServiceProvider services)
        {
            var creds = (IBotCredentials)services.GetService(typeof(IBotCredentials));

            return Task.FromResult((creds.IsAdmin(context.User) || context.Client.CurrentUser.Id == context.User.Id ? PreconditionResult.FromSuccess() : PreconditionResult.FromError("Not bot admin")));
        }
    }
}