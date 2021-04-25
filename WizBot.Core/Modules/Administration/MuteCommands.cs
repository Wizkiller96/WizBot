using Discord;
using Discord.Commands;
using WizBot.Common.Attributes;
using WizBot.Core.Common.TypeReaders.Models;
using WizBot.Modules.Administration.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using WizBot.Extensions;

namespace WizBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        public class MuteCommands : WizBotSubmodule<MuteService>
        {
            private async Task<bool> VerifyMutePermissions(IGuildUser runnerUser, IGuildUser targetUser)
            {
                var runnerUserRoles = runnerUser.GetRoles();
                var targetUserRoles = targetUser.GetRoles();
                if (runnerUser.Id != ctx.Guild.OwnerId &&
                    runnerUserRoles.Max(x => x.Position) <= targetUserRoles.Max(x => x.Position))
                {
                    await ReplyErrorLocalizedAsync("mute_perms").ConfigureAwait(false);
                    return false;
                }

                return true;
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageRoles)]
            public async Task MuteRole([Leftover] IRole role = null)
            {
                if (role is null)
                {
                    var muteRole = await _service.GetMuteRole(ctx.Guild).ConfigureAwait(false);
                    await ReplyConfirmLocalizedAsync("mute_role", Format.Code(muteRole.Name)).ConfigureAwait(false);
                    return;
                }

                if (Context.User.Id != Context.Guild.OwnerId &&
                    role.Position >= ((SocketGuildUser)Context.User).Roles.Max(x => x.Position))
                {
                    await ReplyErrorLocalizedAsync("insuf_perms_u").ConfigureAwait(false);

                    return;
                }

                await _service.SetMuteRoleAsync(ctx.Guild.Id, role.Name).ConfigureAwait(false);

                await ReplyConfirmLocalizedAsync("mute_role_set").ConfigureAwait(false);
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageRoles | GuildPerm.MuteMembers)]
            [Priority(0)]
            public async Task Mute(IGuildUser target, [Leftover] string reason = "")
            {
                try
                {
                    if (!await VerifyMutePermissions((IGuildUser)ctx.User, target))
                        return;

                    await _service.MuteUser(target, ctx.User, reason: reason).ConfigureAwait(false);
                    await ReplyConfirmLocalizedAsync("user_muted", Format.Bold(target.ToString())).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    await ReplyErrorLocalizedAsync("mute_error").ConfigureAwait(false);
                }
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageRoles | GuildPerm.MuteMembers)]
            [Priority(1)]
            public async Task Mute(StoopidTime time, IGuildUser user, [Leftover] string reason = "")
            {
                if (time.Time < TimeSpan.FromMinutes(1) || time.Time > TimeSpan.FromDays(1))
                    return;
                try
                {
                    if (!await VerifyMutePermissions((IGuildUser)ctx.User, user))
                        return;

                    await _service.TimedMute(user, ctx.User, time.Time, reason: reason).ConfigureAwait(false);
                    await ReplyConfirmLocalizedAsync("user_muted_time", Format.Bold(user.ToString()), (int)time.Time.TotalMinutes).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _log.Warn(ex);
                    await ReplyErrorLocalizedAsync("mute_error").ConfigureAwait(false);
                }
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageRoles | GuildPerm.MuteMembers)]
            public async Task Unmute(IGuildUser user, [Leftover] string reason = "")
            {
                try
                {
                    await _service.UnmuteUser(user.GuildId, user.Id, ctx.User, reason: reason).ConfigureAwait(false);
                    await ReplyConfirmLocalizedAsync("user_unmuted", Format.Bold(user.ToString())).ConfigureAwait(false);
                }
                catch
                {
                    await ReplyErrorLocalizedAsync("mute_error").ConfigureAwait(false);
                }
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageRoles)]
            public async Task ChatMute(IGuildUser user, [Leftover] string reason = "")
            {
                try
                {
                    if (!await VerifyMutePermissions((IGuildUser)ctx.User, user))
                        return;

                    await _service.MuteUser(user, ctx.User, MuteType.Chat, reason: reason).ConfigureAwait(false);
                    await ReplyConfirmLocalizedAsync("user_chat_mute", Format.Bold(user.ToString())).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    await ReplyErrorLocalizedAsync("mute_error").ConfigureAwait(false);
                }
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageRoles)]
            public async Task ChatUnmute(IGuildUser user, [Leftover] string reason = "")
            {
                try
                {
                    await _service.UnmuteUser(user.Guild.Id, user.Id, ctx.User, MuteType.Chat, reason: reason).ConfigureAwait(false);
                    await ReplyConfirmLocalizedAsync("user_chat_unmute", Format.Bold(user.ToString())).ConfigureAwait(false);
                }
                catch
                {
                    await ReplyErrorLocalizedAsync("mute_error").ConfigureAwait(false);
                }
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.MuteMembers)]
            public async Task VoiceMute(IGuildUser user, [Leftover] string reason = "")
            {
                try
                {
                    if (!await VerifyMutePermissions((IGuildUser)ctx.User, user))
                        return;

                    await _service.MuteUser(user, ctx.User, MuteType.Voice, reason: reason).ConfigureAwait(false);
                    await ReplyConfirmLocalizedAsync("user_voice_mute", Format.Bold(user.ToString())).ConfigureAwait(false);
                }
                catch
                {
                    await ReplyErrorLocalizedAsync("mute_error").ConfigureAwait(false);
                }
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.MuteMembers)]
            public async Task VoiceUnmute(IGuildUser user, [Leftover] string reason = "")
            {
                try
                {
                    await _service.UnmuteUser(user.GuildId, user.Id, ctx.User, MuteType.Voice, reason: reason).ConfigureAwait(false);
                    await ReplyConfirmLocalizedAsync("user_voice_unmute", Format.Bold(user.ToString())).ConfigureAwait(false);
                }
                catch
                {
                    await ReplyErrorLocalizedAsync("mute_error").ConfigureAwait(false);
                }
            }
        }
    }
}
