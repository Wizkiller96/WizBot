﻿using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using WizBot.Common.Attributes;
using WizBot.Core.Modules.Administration.Services;
using WizBot.Core.Services;
using WizBot.Extensions;

namespace WizBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        public class SelfAssignedRolesCommands : WizBotSubmodule<SelfAssignedRolesService>
        {
            private readonly DbService _db;

            public SelfAssignedRolesCommands(DbService db)
            {
                _db = db;
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            [RequireBotPermission(GuildPermission.ManageMessages)]
            public async Task AdSarm()
            {
                var newVal = _service.ToggleAdSarm(Context.Guild.Id);

                if (newVal)
                {
                    await ReplyConfirmLocalized("adsarm_enable", Prefix).ConfigureAwait(false);
                }
                else
                {
                    await ReplyConfirmLocalized("adsarm_disable", Prefix).ConfigureAwait(false);
                }
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            [RequireBotPermission(GuildPermission.ManageRoles)]
            [Priority(1)]
            public Task Asar([Remainder] IRole role) =>
                Asar(0, role);

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            [RequireBotPermission(GuildPermission.ManageRoles)]
            [Priority(0)]
            public async Task Asar(int group, [Remainder] IRole role)
            {
                var guser = (IGuildUser)Context.User;
                if (Context.User.Id != guser.Guild.OwnerId && guser.GetRoles().Max(x => x.Position) <= role.Position)
                    return;

                var succ = _service.AddNew(Context.Guild.Id, role, group);

                if (succ)
                {
                    await ReplyConfirmLocalized("role_added", Format.Bold(role.Name), Format.Bold(group.ToString())).ConfigureAwait(false);
                }
                else
                {
                    await ReplyErrorLocalized("role_in_list", Format.Bold(role.Name)).ConfigureAwait(false);
                }
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            public async Task Rsar([Remainder] IRole role)
            {
                var guser = (IGuildUser)Context.User;
                if (Context.User.Id != guser.Guild.OwnerId && guser.GetRoles().Max(x => x.Position) <= role.Position)
                    return;

                bool success = _service.RemoveSar(role.Guild.Id, role.Id);
                if (!success)
                {
                    await ReplyErrorLocalized("self_assign_not").ConfigureAwait(false);
                }
                else
                {
                    await ReplyConfirmLocalized("self_assign_rem", Format.Bold(role.Name)).ConfigureAwait(false);
                }
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Lsar(int page = 1)
            {
                if (--page < 0)
                    return;

                var (exclusive, roles) = _service.GetRoles(Context.Guild, page);

                var rolesStr = new StringBuilder();

                foreach (var kvp in roles.GroupBy(x => x.Model.Group))
                {
                    rolesStr.AppendLine("\t\t\t\t『" + Format.Bold(GetText("self_assign_group", kvp.Key)) + "』");
                    foreach (var (Model, Role) in kvp.AsEnumerable())
                    {
                        if (Role == null)
                        {
                            continue;
                        }
                        else
                        {
                            if (Model.LevelRequirement == 0)
                                rolesStr.AppendLine(Format.Bold(Role.Name));
                            else
                                rolesStr.AppendLine(Format.Bold(Role.Name) + $" (lvl {Model.LevelRequirement}+)");
                        }
                    }
                }

                await Context.Channel.SendConfirmAsync("",
                    Format.Bold(GetText("self_assign_list", roles.Count()))
                    + "\n\n" + rolesStr.ToString(),
                    footer: exclusive
                    ? GetText("self_assign_are_exclusive")
                    : GetText("self_assign_are_not_exclusive")).ConfigureAwait(false);
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            [RequireBotPermission(GuildPermission.ManageRoles)]
            public async Task Tesar()
            {
                bool areExclusive = _service.ToggleEsar(Context.Guild.Id);
                if (areExclusive)
                    await ReplyConfirmLocalized("self_assign_excl").ConfigureAwait(false);
                else
                    await ReplyConfirmLocalized("self_assign_no_excl").ConfigureAwait(false);
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            [RequireBotPermission(GuildPermission.ManageRoles)]
            public async Task RoleLevelReq(int level, [Remainder] IRole role)
            {
                if (level < 0)
                    return;

                bool succ = _service.SetLevelReq(Context.Guild.Id, role, level);

                if (!succ)
                {
                    await ReplyErrorLocalized("self_assign_not").ConfigureAwait(false);
                    return;
                }

                await ReplyConfirmLocalized("self_assign_level_req",
                    Format.Bold(role.Name),
                    Format.Bold(level.ToString())).ConfigureAwait(false);
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Iam([Remainder] IRole role)
            {
                var guildUser = (IGuildUser)Context.User;

                var (result, autoDelete, extra) = await _service.Assign(guildUser, role);

                IUserMessage msg;
                if (result == SelfAssignedRolesService.AssignResult.Err_Not_Assignable)
                {
                    msg = await ReplyErrorLocalized("self_assign_not").ConfigureAwait(false);
                }
                else if (result == SelfAssignedRolesService.AssignResult.Err_Lvl_Req)
                {
                    msg = await ReplyErrorLocalized("self_assign_not_level", Format.Bold(extra.ToString())).ConfigureAwait(false);
                }
                else if (result == SelfAssignedRolesService.AssignResult.Err_Already_Have)
                {
                    msg = await ReplyErrorLocalized("self_assign_already", Format.Bold(role.Name)).ConfigureAwait(false);
                }
                else if (result == SelfAssignedRolesService.AssignResult.Err_Not_Perms)
                {
                    msg = await ReplyErrorLocalized("self_assign_perms").ConfigureAwait(false);
                }
                else
                {
                    msg = await ReplyConfirmLocalized("self_assign_success", Format.Bold(role.Name)).ConfigureAwait(false);
                }

                if (autoDelete)
                {
                    msg.DeleteAfter(3);
                    Context.Message.DeleteAfter(3);
                }
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Iamnot([Remainder] IRole role)
            {
                var guildUser = (IGuildUser)Context.User;

                var (result, autoDelete) = await _service.Remove(guildUser, role);

                IUserMessage msg;
                if (result == SelfAssignedRolesService.RemoveResult.Err_Not_Assignable)
                {
                    msg = await ReplyErrorLocalized("self_assign_not").ConfigureAwait(false);
                }
                else if (result == SelfAssignedRolesService.RemoveResult.Err_Not_Have)
                {
                    msg = await ReplyErrorLocalized("self_assign_not_have", Format.Bold(role.Name)).ConfigureAwait(false);
                }
                else if (result == SelfAssignedRolesService.RemoveResult.Err_Not_Perms)
                {
                    msg = await ReplyErrorLocalized("self_assign_perms").ConfigureAwait(false);
                }
                else
                {
                    msg = await ReplyConfirmLocalized("self_assign_remove", Format.Bold(role.Name)).ConfigureAwait(false);
                }

                if (autoDelete)
                {
                    msg.DeleteAfter(3);
                    Context.Message.DeleteAfter(3);
                }
            }
        }
    }
}