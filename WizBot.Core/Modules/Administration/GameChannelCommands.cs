﻿using Discord;
using Discord.Commands;
using WizBot.Core.Services;
using System.Threading.Tasks;
using WizBot.Common.Attributes;
using WizBot.Modules.Administration.Services;

namespace WizBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        public class GameChannelCommands : WizBotSubmodule<GameVoiceChannelService>
        {
            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.Administrator)]
            [RequireBotPermission(GuildPermission.MoveMembers)]
            public async Task GameVoiceChannel()
            {
                var vch = ((IGuildUser)Context.User).VoiceChannel;

                if (vch == null)
                {
                    await ReplyErrorLocalizedAsync("not_in_voice").ConfigureAwait(false);
                    return;
                }
                var id = _service.ToggleGameVoiceChannel(Context.Guild.Id, vch.Id);

                if (id == null)
                {
                    await ReplyConfirmLocalizedAsync("gvc_disabled").ConfigureAwait(false);
                }
                else
                {
                    _service.GameVoiceChannels.Add(vch.Id);
                    await ReplyConfirmLocalizedAsync("gvc_enabled", Format.Bold(vch.Name)).ConfigureAwait(false);
                }
            }
        }
    }
}
