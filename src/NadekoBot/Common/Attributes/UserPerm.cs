using Microsoft.Extensions.DependencyInjection;
using NadekoBot.Modules.Administration.Services;

namespace Discord;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class UserPermAttribute : PreconditionAttribute
{
    public RequireUserPermissionAttribute UserPermissionAttribute { get; }

    public UserPermAttribute(GuildPerm permission)
    {
        UserPermissionAttribute = new((GuildPermission)permission);
    }

    public UserPermAttribute(ChannelPerm permission)
    {
        UserPermissionAttribute = new((ChannelPermission)permission);
    }

    public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
    {
        var permService = services.GetRequiredService<DiscordPermOverrideService>();
        if (permService.TryGetOverrides(context.Guild?.Id ?? 0, command.Name.ToUpperInvariant(), out var _))
            return Task.FromResult(PreconditionResult.FromSuccess());
            
        return UserPermissionAttribute.CheckPermissionsAsync(context, command, services);
    }
}