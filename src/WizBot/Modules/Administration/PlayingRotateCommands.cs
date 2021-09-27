using Discord.Commands;
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
            [WizBotCommand, Aliases]
            [OwnerOnly]
            public async Task RotatePlaying()
            {
                if (_service.ToggleRotatePlaying())
                    await ReplyConfirmLocalizedAsync(strs.ropl_enabled).ConfigureAwait(false);
                else
                    await ReplyConfirmLocalizedAsync(strs.ropl_disabled).ConfigureAwait(false);
            }

            [WizBotCommand, Aliases]
            [OwnerOnly]
            public async Task AddPlaying(ActivityType t, [Leftover] string status)
            {
                await _service.AddPlaying(t, status).ConfigureAwait(false);

                await ReplyConfirmLocalizedAsync(strs.ropl_added).ConfigureAwait(false);
            }

            [WizBotCommand, Aliases]
            [OwnerOnly]
            public async Task ListPlaying()
            {
                var statuses = _service.GetRotatingStatuses();

                if (!statuses.Any())
                {
                    await ReplyErrorLocalizedAsync(strs.ropl_not_set).ConfigureAwait(false);
                }
                else
                {
                    var i = 1;
                    await ReplyConfirmLocalizedAsync(strs.ropl_list(
                        string.Join("\n\t", statuses.Select(rs => $"`{i++}.` *{rs.Type}* {rs.Status}"))));
                }

            }

            [WizBotCommand, Aliases]
            [OwnerOnly]
            public async Task RemovePlaying(int index)
            {
                index -= 1;

                var msg = await _service.RemovePlayingAsync(index).ConfigureAwait(false);

                if (msg is null)
                    return;

                await ReplyConfirmLocalizedAsync(strs.reprm(msg));
            }
        }
    }
}