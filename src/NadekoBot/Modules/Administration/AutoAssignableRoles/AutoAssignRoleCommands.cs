#nullable disable
using NadekoBot.Modules.Administration.Services;

namespace NadekoBot.Modules.Administration;

public partial class Administration
{
    [Group]
    public partial class AutoAssignRoleCommands : NadekoModule<AutoAssignRoleService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageRoles)]
        [BotPerm(GuildPerm.ManageRoles)]
        public async partial Task AutoAssignRole([Leftover] IRole role)
        {
            var guser = (IGuildUser)ctx.User;
            if (role.Id == ctx.Guild.EveryoneRole.Id)
                return;

            // the user can't aar the role which is higher or equal to his highest role
            if (ctx.User.Id != guser.Guild.OwnerId && guser.GetRoles().Max(x => x.Position) <= role.Position)
            {
                await ReplyErrorLocalizedAsync(strs.hierarchy);
                return;
            }

            var roles = await _service.ToggleAarAsync(ctx.Guild.Id, role.Id);
            if (roles.Count == 0)
                await ReplyConfirmLocalizedAsync(strs.aar_disabled);
            else if (roles.Contains(role.Id))
                await AutoAssignRole();
            else
                await ReplyConfirmLocalizedAsync(strs.aar_role_removed(Format.Bold(role.ToString())));
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageRoles)]
        [BotPerm(GuildPerm.ManageRoles)]
        public async partial Task AutoAssignRole()
        {
            if (!_service.TryGetRoles(ctx.Guild.Id, out var roles))
            {
                await ReplyConfirmLocalizedAsync(strs.aar_none);
                return;
            }

            var existing = roles.Select(rid => ctx.Guild.GetRole(rid)).Where(r => r is not null).ToList();

            if (existing.Count != roles.Count)
                await _service.SetAarRolesAsync(ctx.Guild.Id, existing.Select(x => x.Id));

            await ReplyConfirmLocalizedAsync(strs.aar_roles(
                '\n' + existing.Select(x => Format.Bold(x.ToString())).Join(",\n")));
        }
    }
}