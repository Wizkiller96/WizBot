#nullable disable
using WizBot.Modules.Administration.Services;

namespace WizBot.Modules.Administration;

public partial class Administration
{
    [Group]
    public partial class PlayingRotateCommands : WizBotModule<PlayingRotateService>
    {
        [Cmd]
        [OwnerOnly]
        public async Task RotatePlaying()
        {
            if (_service.ToggleRotatePlaying())
                await Response().Confirm(strs.ropl_enabled).SendAsync();
            else
                await Response().Confirm(strs.ropl_disabled).SendAsync();
        }

        [Cmd]
        [OwnerOnly]
        public async Task AddPlaying(ActivityType t, [Leftover] string status)
        {
            await _service.AddPlaying(t, status);

            await Response().Confirm(strs.ropl_added).SendAsync();
        }

        [Cmd]
        [OwnerOnly]
        public async Task ListPlaying()
        {
            var statuses = _service.GetRotatingStatuses();

            if (!statuses.Any())
                await Response().Error(strs.ropl_not_set).SendAsync();
            else
            {
                var i = 1;
                await Response()
                      .Confirm(strs.ropl_list(string.Join("\n\t",
                          statuses.Select(rs => $"`{i++}.` *{rs.Type}* {rs.Status}"))))
                      .SendAsync();
            }
        }

        [Cmd]
        [OwnerOnly]
        public async Task RemovePlaying(int index)
        {
            index -= 1;

            var msg = await _service.RemovePlayingAsync(index);

            if (msg is null)
                return;

            await Response().Confirm(strs.reprm(msg)).SendAsync();
        }
    }
}