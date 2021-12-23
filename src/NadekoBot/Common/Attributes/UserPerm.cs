using Microsoft.Extensions.DependencyInjection;
using NadekoBot.Modules.Administration.Services;

namespace Discord;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class UserPermAttribute : RequireUserPermissionAttribute
{
    public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
    {
        var permService = services.GetRequiredService<DiscordPermOverrideService>();
        if (permService.TryGetOverrides(context.Guild?.Id ?? 0, command.Name.ToUpperInvariant(), out var _))
            return Task.FromResult(PreconditionResult.FromSuccess());
            
        return base.CheckPermissionsAsync(context, command, services);
    }

    public UserPermAttribute(GuildPerm permission) : base(permission)
    {
    }

    public UserPermAttribute(ChannelPerm permission) : base(permission)
    {
    }
}