#nullable disable
using NadekoBot.Common.TypeReaders.Models;
using NadekoBot.Modules.Administration.Services;

namespace NadekoBot.Modules.Administration;

public partial class Administration
{
    [Group]
    public partial class MuteCommands : NadekoModule<MuteService>
    {
        private async Task<bool> VerifyMutePermissions(IGuildUser runnerUser, IGuildUser targetUser)
        {
            var runnerUserRoles = runnerUser.GetRoles();
            var targetUserRoles = targetUser.GetRoles();
            if (runnerUser.Id != ctx.Guild.OwnerId
                && runnerUserRoles.Max(x => x.Position) <= targetUserRoles.Max(x => x.Position))
            {
                await ReplyErrorLocalizedAsync(strs.mute_perms);
                return false;
            }

            return true;
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageRoles)]
        public async partial Task MuteRole([Leftover] IRole role = null)
        {
            if (role is null)
            {
                var muteRole = await _service.GetMuteRole(ctx.Guild);
                await ReplyConfirmLocalizedAsync(strs.mute_role(Format.Code(muteRole.Name)));
                return;
            }

            if (ctx.User.Id != ctx.Guild.OwnerId
                && role.Position >= ((SocketGuildUser)ctx.User).Roles.Max(x => x.Position))
            {
                await ReplyErrorLocalizedAsync(strs.insuf_perms_u);
                return;
            }

            await _service.SetMuteRoleAsync(ctx.Guild.Id, role.Name);

            await ReplyConfirmLocalizedAsync(strs.mute_role_set);
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageRoles | GuildPerm.MuteMembers)]
        [Priority(0)]
        public async partial Task Mute(IGuildUser target, [Leftover] string reason = "")
        {
            try
            {
                if (!await VerifyMutePermissions((IGuildUser)ctx.User, target))
                    return;

                await _service.MuteUser(target, ctx.User, reason: reason);
                await ReplyConfirmLocalizedAsync(strs.user_muted(Format.Bold(target.ToString())));
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Exception in the mute command");
                await ReplyErrorLocalizedAsync(strs.mute_error);
            }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageRoles | GuildPerm.MuteMembers)]
        [Priority(1)]
        public async partial Task Mute(StoopidTime time, IGuildUser user, [Leftover] string reason = "")
        {
            if (time.Time < TimeSpan.FromMinutes(1) || time.Time > TimeSpan.FromDays(49))
                return;
            try
            {
                if (!await VerifyMutePermissions((IGuildUser)ctx.User, user))
                    return;

                await _service.TimedMute(user, ctx.User, time.Time, reason: reason);
                await ReplyConfirmLocalizedAsync(strs.user_muted_time(Format.Bold(user.ToString()),
                    (int)time.Time.TotalMinutes));
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error in mute command");
                await ReplyErrorLocalizedAsync(strs.mute_error);
            }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageRoles | GuildPerm.MuteMembers)]
        public async partial Task Unmute(IGuildUser user, [Leftover] string reason = "")
        {
            try
            {
                await _service.UnmuteUser(user.GuildId, user.Id, ctx.User, reason: reason);
                await ReplyConfirmLocalizedAsync(strs.user_unmuted(Format.Bold(user.ToString())));
            }
            catch
            {
                await ReplyErrorLocalizedAsync(strs.mute_error);
            }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageRoles)]
        [Priority(0)]
        public async partial Task ChatMute(IGuildUser user, [Leftover] string reason = "")
        {
            try
            {
                if (!await VerifyMutePermissions((IGuildUser)ctx.User, user))
                    return;

                await _service.MuteUser(user, ctx.User, MuteType.Chat, reason);
                await ReplyConfirmLocalizedAsync(strs.user_chat_mute(Format.Bold(user.ToString())));
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Exception in the chatmute command");
                await ReplyErrorLocalizedAsync(strs.mute_error);
            }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageRoles)]
        [Priority(1)]
        public async partial Task ChatMute(StoopidTime time, IGuildUser user, [Leftover] string reason = "")
        {
            if (time.Time < TimeSpan.FromMinutes(1) || time.Time > TimeSpan.FromDays(49))
                return;
            try
            {
                if (!await VerifyMutePermissions((IGuildUser)ctx.User, user))
                    return;

                await _service.TimedMute(user, ctx.User, time.Time, MuteType.Chat, reason);
                await ReplyConfirmLocalizedAsync(strs.user_chat_mute_time(Format.Bold(user.ToString()),
                    (int)time.Time.TotalMinutes));
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error in chatmute command");
                await ReplyErrorLocalizedAsync(strs.mute_error);
            }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageRoles)]
        public async partial Task ChatUnmute(IGuildUser user, [Leftover] string reason = "")
        {
            try
            {
                await _service.UnmuteUser(user.Guild.Id, user.Id, ctx.User, MuteType.Chat, reason);
                await ReplyConfirmLocalizedAsync(strs.user_chat_unmute(Format.Bold(user.ToString())));
            }
            catch
            {
                await ReplyErrorLocalizedAsync(strs.mute_error);
            }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.MuteMembers)]
        [Priority(0)]
        public async partial Task VoiceMute(IGuildUser user, [Leftover] string reason = "")
        {
            try
            {
                if (!await VerifyMutePermissions((IGuildUser)ctx.User, user))
                    return;

                await _service.MuteUser(user, ctx.User, MuteType.Voice, reason);
                await ReplyConfirmLocalizedAsync(strs.user_voice_mute(Format.Bold(user.ToString())));
            }
            catch
            {
                await ReplyErrorLocalizedAsync(strs.mute_error);
            }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.MuteMembers)]
        [Priority(1)]
        public async partial Task VoiceMute(StoopidTime time, IGuildUser user, [Leftover] string reason = "")
        {
            if (time.Time < TimeSpan.FromMinutes(1) || time.Time > TimeSpan.FromDays(49))
                return;
            try
            {
                if (!await VerifyMutePermissions((IGuildUser)ctx.User, user))
                    return;

                await _service.TimedMute(user, ctx.User, time.Time, MuteType.Voice, reason);
                await ReplyConfirmLocalizedAsync(strs.user_voice_mute_time(Format.Bold(user.ToString()),
                    (int)time.Time.TotalMinutes));
            }
            catch
            {
                await ReplyErrorLocalizedAsync(strs.mute_error);
            }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.MuteMembers)]
        public async partial Task VoiceUnmute(IGuildUser user, [Leftover] string reason = "")
        {
            try
            {
                await _service.UnmuteUser(user.GuildId, user.Id, ctx.User, MuteType.Voice, reason);
                await ReplyConfirmLocalizedAsync(strs.user_voice_unmute(Format.Bold(user.ToString())));
            }
            catch
            {
                await ReplyErrorLocalizedAsync(strs.mute_error);
            }
        }
    }
}