﻿using System.Threading.Tasks;
using Discord.Commands;

namespace WizBot.Attributes
{
    public class OwnerOnlyAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo executingCommand,IDependencyMap depMap) =>
            Task.FromResult((WizBot.Credentials.IsOwner(context.User) || WizBot.Client.CurrentUser.Id == context.User.Id ? PreconditionResult.FromSuccess() : PreconditionResult.FromError("Not owner")));
    }
}