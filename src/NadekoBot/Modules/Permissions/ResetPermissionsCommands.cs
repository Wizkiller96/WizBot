#nullable disable
using NadekoBot.Modules.Permissions.Services;

namespace NadekoBot.Modules.Permissions;

public partial class Permissions
{
    [Group]
    public partial class ResetPermissionsCommands : NadekoModule
    {
        private readonly GlobalPermissionService _gps;
        private readonly PermissionService _perms;

        public ResetPermissionsCommands(GlobalPermissionService gps, PermissionService perms)
        {
            _gps = gps;
            _perms = perms;
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async partial Task ResetPerms()
        {
            await _perms.Reset(ctx.Guild.Id);
            await ReplyConfirmLocalizedAsync(strs.perms_reset);
        }

        [Cmd]
        [OwnerOnly]
        public async partial Task ResetGlobalPerms()
        {
            await _gps.Reset();
            await ReplyConfirmLocalizedAsync(strs.global_perms_reset);
        }
    }
}