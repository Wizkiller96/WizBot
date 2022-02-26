#nullable disable
using NadekoBot.Modules.Administration.Services;

namespace NadekoBot.Modules.Administration;

public partial class Administration
{
    [Group]
    public partial class PlayingRotateCommands : NadekoModule<PlayingRotateService>
    {
        [Cmd]
        [OwnerOnly]
        public async partial Task RotatePlaying()
        {
            if (_service.ToggleRotatePlaying())
                await ReplyConfirmLocalizedAsync(strs.ropl_enabled);
            else
                await ReplyConfirmLocalizedAsync(strs.ropl_disabled);
        }

        [Cmd]
        [OwnerOnly]
        public async partial Task AddPlaying(ActivityType t, [Leftover] string status)
        {
            await _service.AddPlaying(t, status);

            await ReplyConfirmLocalizedAsync(strs.ropl_added);
        }

        [Cmd]
        [OwnerOnly]
        public async partial Task ListPlaying()
        {
            var statuses = _service.GetRotatingStatuses();

            if (!statuses.Any())
                await ReplyErrorLocalizedAsync(strs.ropl_not_set);
            else
            {
                var i = 1;
                await ReplyConfirmLocalizedAsync(strs.ropl_list(string.Join("\n\t",
                    statuses.Select(rs => $"`{i++}.` *{rs.Type}* {rs.Status}"))));
            }
        }

        [Cmd]
        [OwnerOnly]
        public async partial Task RemovePlaying(int index)
        {
            index -= 1;

            var msg = await _service.RemovePlayingAsync(index);

            if (msg is null)
                return;

            await ReplyConfirmLocalizedAsync(strs.reprm(msg));
        }
    }
}