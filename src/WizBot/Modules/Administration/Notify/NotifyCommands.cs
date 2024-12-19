using WizBot.Db.Models;

namespace WizBot.Modules.Administration;

public partial class Administration
{
    public class NotifyCommands : WizBotModule<NotifyService>
    {
        [Cmd]
        [OwnerOnly]
        public async Task Notify(NotifyType nType, [Leftover] string? message = null)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                await _service.DisableAsync(ctx.Guild.Id, nType);
                await Response().Confirm(strs.notify_off(nType)).SendAsync();
                return;
            }

            await _service.EnableAsync(ctx.Guild.Id, ctx.Channel.Id, nType, message);
            await Response().Confirm(strs.notify_on(nType.ToString())).SendAsync();
        }
    }
}