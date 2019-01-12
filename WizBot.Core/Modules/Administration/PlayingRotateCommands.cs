﻿using Discord.Commands;
using WizBot.Core.Services;
using System.Linq;
using System.Threading.Tasks;
using WizBot.Common.Attributes;
using WizBot.Modules.Administration.Services;
using Discord;

namespace WizBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        public class PlayingRotateCommands : WizBotSubmodule<PlayingRotateService>
        {
            [WizBotCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task RotatePlaying()
            {
                if (_service.ToggleRotatePlaying())
                    await ReplyConfirmLocalizedAsync("ropl_enabled").ConfigureAwait(false);
                else
                    await ReplyConfirmLocalizedAsync("ropl_disabled").ConfigureAwait(false);
            }

            [WizBotCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task AddPlaying(ActivityType t, [Remainder] string status)
            {
                await _service.AddPlaying(t, status).ConfigureAwait(false);

                await ReplyConfirmLocalizedAsync("ropl_added").ConfigureAwait(false);
            }

            [WizBotCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task ListPlaying()
            {
                if (!_service.BotConfig.RotatingStatusMessages.Any())
                    await ReplyErrorLocalizedAsync("ropl_not_set").ConfigureAwait(false);
                else
                {
                    var i = 1;
                    await ReplyConfirmLocalizedAsync("ropl_list",
                            string.Join("\n\t", _service.BotConfig.RotatingStatusMessages.Select(rs => $"`{i++}.` *{rs.Type}* {rs.Status}")))
                        .ConfigureAwait(false);
                }

            }

            [WizBotCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task RemovePlaying(int index)
            {
                index -= 1;

                var msg = await _service.RemovePlayingAsync(index).ConfigureAwait(false);

                if (msg == null)
                    return;

                await ReplyConfirmLocalizedAsync("reprm", msg).ConfigureAwait(false);
            }
        }
    }
}