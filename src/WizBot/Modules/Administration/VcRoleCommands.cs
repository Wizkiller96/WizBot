using System.Collections.Concurrent;
using System.Linq;
using Discord;
using Discord.Commands;
using System.Threading.Tasks;
using Discord.WebSocket;
using WizBot.Common.Attributes;
using WizBot.Extensions;
using WizBot.Modules.Administration.Services;

namespace WizBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        public class VcRoleCommands : WizBotSubmodule<VcRoleService>
        {
            [WizBotCommand, Aliases]
            [UserPerm(GuildPerm.ManageRoles)]
            [BotPerm(GuildPerm.ManageRoles)]
            [RequireContext(ContextType.Guild)]
            public async Task VcRoleRm(ulong vcId)
            {
                if (_service.RemoveVcRole(ctx.Guild.Id, vcId))
                {
                    await ReplyConfirmLocalizedAsync(strs.vcrole_removed(Format.Bold(vcId.ToString()))).ConfigureAwait(false);
                }
                else
                {
                    await ReplyErrorLocalizedAsync(strs.vcrole_not_found).ConfigureAwait(false);
                }
            }

            [WizBotCommand, Aliases]
            [UserPerm(GuildPerm.ManageRoles)]
            [BotPerm(GuildPerm.ManageRoles)]
            [RequireContext(ContextType.Guild)]
            public async Task VcRole([Leftover] IRole role = null)
            {
                var user = (IGuildUser)ctx.User;

                var vc = user.VoiceChannel;

                if (vc is null || vc.GuildId != user.GuildId)
                {
                    await ReplyErrorLocalizedAsync(strs.must_be_in_voice).ConfigureAwait(false);
                    return;
                }

                if (role is null)
                {
                    if (_service.RemoveVcRole(ctx.Guild.Id, vc.Id))
                    {
                        await ReplyConfirmLocalizedAsync(strs.vcrole_removed(Format.Bold(vc.Name))).ConfigureAwait(false);
                    }
                }
                else
                {
                    _service.AddVcRole(ctx.Guild.Id, role, vc.Id);
                    await ReplyConfirmLocalizedAsync(strs.vcrole_added(Format.Bold(vc.Name), Format.Bold(role.Name))).ConfigureAwait(false);
                }
            }

            [WizBotCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task VcRoleList()
            {
                var guild = (SocketGuild)ctx.Guild;
                string text;
                if (_service.VcRoles.TryGetValue(ctx.Guild.Id, out ConcurrentDictionary<ulong, IRole> roles))
                {
                    if (!roles.Any())
                    {
                        text = GetText(strs.no_vcroles);
                    }
                    else
                    {
                        text = string.Join("\n", roles.Select(x =>
                            $"{Format.Bold(guild.GetVoiceChannel(x.Key)?.Name ?? x.Key.ToString())} => {x.Value}"));
                    }
                }
                else
                {
                    text = GetText(strs.no_vcroles);
                }
                await ctx.Channel.EmbedAsync(_eb.Create().WithOkColor()
                        .WithTitle(GetText(strs.vc_role_list))
                        .WithDescription(text))
                    .ConfigureAwait(false);
            }
        }
    }
}
