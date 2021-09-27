﻿using Discord.Commands;
using WizBot.Services;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace WizBot.Common.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class RatelimitAttribute : PreconditionAttribute
    {
        public int Seconds { get; }

        public RatelimitAttribute(int seconds)
        {
            if (seconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(seconds));

            Seconds = seconds;
        }

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (Seconds == 0)
                return Task.FromResult(PreconditionResult.FromSuccess());

            var cache = services.GetRequiredService<IDataCache>();
            var rem = cache.TryAddRatelimit(context.User.Id, command.Name, Seconds);

            if(rem is null)
                return Task.FromResult(PreconditionResult.FromSuccess());

            var msgContent = $"You can use this command again in {rem.Value.TotalSeconds:F1}s.";

            return Task.FromResult(PreconditionResult.FromError(msgContent));
        }
    }
}
