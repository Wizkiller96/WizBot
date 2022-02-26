#nullable disable
using NadekoBot.Modules.Administration.Services;
using System.Text;

namespace NadekoBot.Modules.Administration;

public partial class Administration
{
    [Group]
    public partial class SelfAssignedRolesCommands : NadekoModule<SelfAssignedRolesService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        [BotPerm(GuildPerm.ManageMessages)]
        public async partial Task AdSarm()
        {
            var newVal = _service.ToggleAdSarm(ctx.Guild.Id);

            if (newVal)
                await ReplyConfirmLocalizedAsync(strs.adsarm_enable(prefix));
            else
                await ReplyConfirmLocalizedAsync(strs.adsarm_disable(prefix));
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageRoles)]
        [BotPerm(GuildPerm.ManageRoles)]
        [Priority(1)]
        public partial Task Asar([Leftover] IRole role)
            => Asar(0, role);

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageRoles)]
        [BotPerm(GuildPerm.ManageRoles)]
        [Priority(0)]
        public async partial Task Asar(int group, [Leftover] IRole role)
        {
            var guser = (IGuildUser)ctx.User;
            if (ctx.User.Id != guser.Guild.OwnerId && guser.GetRoles().Max(x => x.Position) <= role.Position)
                return;

            var succ = _service.AddNew(ctx.Guild.Id, role, group);

            if (succ)
            {
                await ReplyConfirmLocalizedAsync(strs.role_added(Format.Bold(role.Name),
                    Format.Bold(@group.ToString())));
            }
            else
                await ReplyErrorLocalizedAsync(strs.role_in_list(Format.Bold(role.Name)));
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageRoles)]
        [BotPerm(GuildPerm.ManageRoles)]
        [Priority(0)]
        public async partial Task Sargn(int group, [Leftover] string name = null)
        {
            var set = await _service.SetNameAsync(ctx.Guild.Id, group, name);

            if (set)
            {
                await ReplyConfirmLocalizedAsync(
                    strs.group_name_added(Format.Bold(@group.ToString()), Format.Bold(name)));
            }
            else
                await ReplyConfirmLocalizedAsync(strs.group_name_removed(Format.Bold(group.ToString())));
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageRoles)]
        public async partial Task Rsar([Leftover] IRole role)
        {
            var guser = (IGuildUser)ctx.User;
            if (ctx.User.Id != guser.Guild.OwnerId && guser.GetRoles().Max(x => x.Position) <= role.Position)
                return;

            var success = _service.RemoveSar(role.Guild.Id, role.Id);
            if (!success)
                await ReplyErrorLocalizedAsync(strs.self_assign_not);
            else
                await ReplyConfirmLocalizedAsync(strs.self_assign_rem(Format.Bold(role.Name)));
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async partial Task Lsar(int page = 1)
        {
            if (--page < 0)
                return;

            var (exclusive, roles, groups) = _service.GetRoles(ctx.Guild);

            await ctx.SendPaginatedConfirmAsync(page,
                cur =>
                {
                    var rolesStr = new StringBuilder();
                    var roleGroups = roles.OrderBy(x => x.Model.Group)
                                          .Skip(cur * 20)
                                          .Take(20)
                                          .GroupBy(x => x.Model.Group)
                                          .OrderBy(x => x.Key);

                    foreach (var kvp in roleGroups)
                    {
                        string groupNameText;
                        if (!groups.TryGetValue(kvp.Key, out var name))
                            groupNameText = Format.Bold(GetText(strs.self_assign_group(kvp.Key)));
                        else
                            groupNameText = Format.Bold($"{kvp.Key} - {name.TrimTo(25, true)}");

                        rolesStr.AppendLine("\t\t\t\t ⟪" + groupNameText + "⟫");
                        foreach (var (model, role) in kvp.AsEnumerable())
                        {
                            if (role is null)
                            {
                            }
                            else
                            {
                                // first character is invisible space
                                if (model.LevelRequirement == 0)
                                    rolesStr.AppendLine("‌‌   " + role.Name);
                                else
                                    rolesStr.AppendLine("‌‌   " + role.Name + $" (lvl {model.LevelRequirement}+)");
                            }
                        }

                        rolesStr.AppendLine();
                    }

                    return _eb.Create()
                              .WithOkColor()
                              .WithTitle(Format.Bold(GetText(strs.self_assign_list(roles.Count()))))
                              .WithDescription(rolesStr.ToString())
                              .WithFooter(exclusive
                                  ? GetText(strs.self_assign_are_exclusive)
                                  : GetText(strs.self_assign_are_not_exclusive));
                },
                roles.Count(),
                20);
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageRoles)]
        [BotPerm(GuildPerm.ManageRoles)]
        public async partial Task Togglexclsar()
        {
            var areExclusive = _service.ToggleEsar(ctx.Guild.Id);
            if (areExclusive)
                await ReplyConfirmLocalizedAsync(strs.self_assign_excl);
            else
                await ReplyConfirmLocalizedAsync(strs.self_assign_no_excl);
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageRoles)]
        [BotPerm(GuildPerm.ManageRoles)]
        public async partial Task RoleLevelReq(int level, [Leftover] IRole role)
        {
            if (level < 0)
                return;

            var succ = _service.SetLevelReq(ctx.Guild.Id, role, level);

            if (!succ)
            {
                await ReplyErrorLocalizedAsync(strs.self_assign_not);
                return;
            }

            await ReplyConfirmLocalizedAsync(strs.self_assign_level_req(Format.Bold(role.Name),
                Format.Bold(level.ToString())));
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async partial Task Iam([Leftover] IRole role)
        {
            var guildUser = (IGuildUser)ctx.User;

            var (result, autoDelete, extra) = await _service.Assign(guildUser, role);

            IUserMessage msg;
            if (result == SelfAssignedRolesService.AssignResult.ErrNotAssignable)
                msg = await ReplyErrorLocalizedAsync(strs.self_assign_not);
            else if (result == SelfAssignedRolesService.AssignResult.ErrLvlReq)
                msg = await ReplyErrorLocalizedAsync(strs.self_assign_not_level(Format.Bold(extra.ToString())));
            else if (result == SelfAssignedRolesService.AssignResult.ErrAlreadyHave)
                msg = await ReplyErrorLocalizedAsync(strs.self_assign_already(Format.Bold(role.Name)));
            else if (result == SelfAssignedRolesService.AssignResult.ErrNotPerms)
                msg = await ReplyErrorLocalizedAsync(strs.self_assign_perms);
            else
                msg = await ReplyConfirmLocalizedAsync(strs.self_assign_success(Format.Bold(role.Name)));

            if (autoDelete)
            {
                msg.DeleteAfter(3);
                ctx.Message.DeleteAfter(3);
            }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async partial Task Iamnot([Leftover] IRole role)
        {
            var guildUser = (IGuildUser)ctx.User;

            var (result, autoDelete) = await _service.Remove(guildUser, role);

            IUserMessage msg;
            if (result == SelfAssignedRolesService.RemoveResult.ErrNotAssignable)
                msg = await ReplyErrorLocalizedAsync(strs.self_assign_not);
            else if (result == SelfAssignedRolesService.RemoveResult.ErrNotHave)
                msg = await ReplyErrorLocalizedAsync(strs.self_assign_not_have(Format.Bold(role.Name)));
            else if (result == SelfAssignedRolesService.RemoveResult.ErrNotPerms)
                msg = await ReplyErrorLocalizedAsync(strs.self_assign_perms);
            else
                msg = await ReplyConfirmLocalizedAsync(strs.self_assign_remove(Format.Bold(role.Name)));

            if (autoDelete)
            {
                msg.DeleteAfter(3);
                ctx.Message.DeleteAfter(3);
            }
        }
    }
}