using Microsoft.Extensions.DependencyInjection;

namespace WizBot.Common.Attributes;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class AdminOnlyAttribute : PreconditionAttribute
{
    public override Task<PreconditionResult> CheckPermissionsAsync(
        ICommandContext context,
        CommandInfo command,
        IServiceProvider services)
    {
        var creds = services.GetRequiredService<IBotCredsProvider>().GetCreds();

        return Task.FromResult(creds.IsOwner(context.User) || creds.IsAdmin(context.User) || context.Client.CurrentUser.Id == context.User.Id
            ? PreconditionResult.FromSuccess()
            : PreconditionResult.FromError("Not Bot Staff"));
    }
}