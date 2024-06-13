#nullable disable
using System.Diagnostics.CodeAnalysis;

namespace Wiz.Common;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
[SuppressMessage("Style", "IDE0022:Use expression body for methods")]
public sealed class OnlyPublicBotAttribute : PreconditionAttribute
{
    public override Task<PreconditionResult> CheckPermissionsAsync(
        ICommandContext context,
        CommandInfo command,
        IServiceProvider services)
    {
#if GLOBAL_WIZBOT || DEBUG
        return Task.FromResult(PreconditionResult.FromSuccess());
#else
        return Task.FromResult(PreconditionResult.FromError("Only available on the public bot."));
#endif
    }
}