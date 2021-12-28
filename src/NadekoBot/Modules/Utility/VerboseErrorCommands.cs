#nullable disable
using NadekoBot.Modules.Utility.Services;

namespace NadekoBot.Modules.Utility;

public partial class Utility
{
    [Group]
    public class VerboseErrorCommands : NadekoSubmodule<VerboseErrorsService>
    {
        [NadekoCommand, Aliases]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task VerboseError(bool? newstate = null)
        {
            var state = _service.ToggleVerboseErrors(ctx.Guild.Id, newstate);

            if (state)
                await ReplyConfirmLocalizedAsync(strs.verbose_errors_enabled);
            else
                await ReplyConfirmLocalizedAsync(strs.verbose_errors_disabled);
        }
    }
}
