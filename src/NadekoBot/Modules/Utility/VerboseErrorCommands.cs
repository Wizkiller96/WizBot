using Discord;
using Discord.Commands;
using NadekoBot.Common.Attributes;
using NadekoBot.Modules.Utility.Services;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Utility
{
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
                    await ReplyConfirmLocalizedAsync(strs.verbose_errors_enabled).ConfigureAwait(false);
                else
                    await ReplyConfirmLocalizedAsync(strs.verbose_errors_disabled).ConfigureAwait(false);
            }
        }
    }
}
