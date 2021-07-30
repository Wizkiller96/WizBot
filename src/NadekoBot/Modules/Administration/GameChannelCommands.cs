using Discord;
using Discord.Commands;
using System.Threading.Tasks;
using NadekoBot.Common.Attributes;
using NadekoBot.Modules.Administration.Services;

namespace NadekoBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        public class GameChannelCommands : NadekoSubmodule<GameVoiceChannelService>
        {
            [NadekoCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.Administrator)]
            [BotPerm(GuildPerm.MoveMembers)]
            public async Task GameVoiceChannel()
            {
                var vch = ((IGuildUser)ctx.User).VoiceChannel;

                if (vch is null)
                {
                    await ReplyErrorLocalizedAsync(strs.not_in_voice).ConfigureAwait(false);
                    return;
                }
                var id = _service.ToggleGameVoiceChannel(ctx.Guild.Id, vch.Id);

                if (id is null)
                {
                    await ReplyConfirmLocalizedAsync(strs.gvc_disabled).ConfigureAwait(false);
                }
                else
                {
                    _service.GameVoiceChannels.Add(vch.Id);
                    await ReplyConfirmLocalizedAsync(strs.gvc_enabled(Format.Bold(vch.Name))).ConfigureAwait(false);
                }
            }
        }
    }
}
