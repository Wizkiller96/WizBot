#nullable disable
using WizBot.Modules.Administration.Services;

namespace WizBot.Modules.Administration;

public partial class Administration
{
    [Group]
    public partial class GameVoiceChannelCommands : WizModule<GameVoiceChannelService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        [BotPerm(GuildPerm.MoveMembers)]
        public async Task GameVoiceChannel()
        {
            var vch = ((IGuildUser)ctx.User).VoiceChannel;

            if (vch is null)
            {
                await Response().Error(strs.not_in_voice).SendAsync();
                return;
            }

            var id = _service.ToggleGameVoiceChannel(ctx.Guild.Id, vch.Id);

            if (id is null)
            {
                await Response().Confirm(strs.gvc_disabled).SendAsync();
            }
            else
            {
                await Response().Confirm(strs.gvc_enabled(Format.Bold(vch.Name))).SendAsync();
            }
        }
    }
}