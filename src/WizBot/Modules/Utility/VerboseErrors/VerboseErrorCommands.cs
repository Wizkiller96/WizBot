#nullable disable
using WizBot.Modules.Utility.Services;

namespace WizBot.Modules.Utility;

public partial class Utility
{
    [Group]
    public partial class VerboseErrorCommands : WizBotModule<VerboseErrorsService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async partial Task VerboseError(bool? newstate = null)
        {
            var state = _service.ToggleVerboseErrors(ctx.Guild.Id, newstate);

            if (state)
                await ReplyConfirmLocalizedAsync(strs.verbose_errors_enabled);
            else
                await ReplyConfirmLocalizedAsync(strs.verbose_errors_disabled);
        }
    }
}