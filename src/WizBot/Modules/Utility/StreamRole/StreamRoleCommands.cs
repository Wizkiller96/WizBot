﻿#nullable disable
using WizBot.Modules.Utility.Common;
using WizBot.Modules.Utility.Services;

namespace WizBot.Modules.Utility;

public partial class Utility
{
    public partial class StreamRoleCommands : WizBotModule<StreamRoleService>
    {
        [Cmd]
        [BotPerm(GuildPerm.ManageRoles)]
        [UserPerm(GuildPerm.ManageRoles)]
        [RequireContext(ContextType.Guild)]
        public async Task StreamRole(IRole fromRole, IRole addRole)
        {
            await _service.SetStreamRole(fromRole, addRole);

            await ReplyConfirmLocalizedAsync(strs.stream_role_enabled(Format.Bold(fromRole.ToString()),
                Format.Bold(addRole.ToString())));
        }

        [Cmd]
        [BotPerm(GuildPerm.ManageRoles)]
        [UserPerm(GuildPerm.ManageRoles)]
        [RequireContext(ContextType.Guild)]
        public async Task StreamRole()
        {
            await _service.StopStreamRole(ctx.Guild);
            await ReplyConfirmLocalizedAsync(strs.stream_role_disabled);
        }

        [Cmd]
        [BotPerm(GuildPerm.ManageRoles)]
        [UserPerm(GuildPerm.ManageRoles)]
        [RequireContext(ContextType.Guild)]
        public async Task StreamRoleKeyword([Leftover] string keyword = null)
        {
            var kw = await _service.SetKeyword(ctx.Guild, keyword);

            if (string.IsNullOrWhiteSpace(keyword))
                await ReplyConfirmLocalizedAsync(strs.stream_role_kw_reset);
            else
                await ReplyConfirmLocalizedAsync(strs.stream_role_kw_set(Format.Bold(kw)));
        }

        [Cmd]
        [BotPerm(GuildPerm.ManageRoles)]
        [UserPerm(GuildPerm.ManageRoles)]
        [RequireContext(ContextType.Guild)]
        public async Task StreamRoleBlacklist(AddRemove action, [Leftover] IGuildUser user)
        {
            var success = await _service.ApplyListAction(StreamRoleListType.Blacklist,
                ctx.Guild,
                action,
                user.Id,
                user.ToString());

            if (action == AddRemove.Add)
            {
                if (success)
                    await ReplyConfirmLocalizedAsync(strs.stream_role_bl_add(Format.Bold(user.ToString())));
                else
                    await ReplyConfirmLocalizedAsync(strs.stream_role_bl_add_fail(Format.Bold(user.ToString())));
            }
            else if (success)
                await ReplyConfirmLocalizedAsync(strs.stream_role_bl_rem(Format.Bold(user.ToString())));
            else
                await ReplyErrorLocalizedAsync(strs.stream_role_bl_rem_fail(Format.Bold(user.ToString())));
        }

        [Cmd]
        [BotPerm(GuildPerm.ManageRoles)]
        [UserPerm(GuildPerm.ManageRoles)]
        [RequireContext(ContextType.Guild)]
        public async Task StreamRoleWhitelist(AddRemove action, [Leftover] IGuildUser user)
        {
            var success = await _service.ApplyListAction(StreamRoleListType.Whitelist,
                ctx.Guild,
                action,
                user.Id,
                user.ToString());

            if (action == AddRemove.Add)
            {
                if (success)
                    await ReplyConfirmLocalizedAsync(strs.stream_role_wl_add(Format.Bold(user.ToString())));
                else
                    await ReplyConfirmLocalizedAsync(strs.stream_role_wl_add_fail(Format.Bold(user.ToString())));
            }
            else if (success)
                await ReplyConfirmLocalizedAsync(strs.stream_role_wl_rem(Format.Bold(user.ToString())));
            else
                await ReplyErrorLocalizedAsync(strs.stream_role_wl_rem_fail(Format.Bold(user.ToString())));
        }
    }
}