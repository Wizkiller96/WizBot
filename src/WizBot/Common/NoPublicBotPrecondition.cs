﻿#nullable disable
using System.Diagnostics.CodeAnalysis;

namespace WizBot.Common;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
[SuppressMessage("Style", "IDE0022:Use expression body for methods")]
public sealed class NoPublicBotAttribute : PreconditionAttribute
{
    public override Task<PreconditionResult> CheckPermissionsAsync(
        ICommandContext context,
        CommandInfo command,
        IServiceProvider services)
    {
#if GLOBAL_WIZBOT
        return Task.FromResult(PreconditionResult.FromError("Not available on the public bot. To learn how to selfhost a private bot, click [here](https://wizbot.readthedocs.io/en/latest/)."));
#else
        return Task.FromResult(PreconditionResult.FromSuccess());
#endif
    }
}