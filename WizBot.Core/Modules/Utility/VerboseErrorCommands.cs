using Discord;
using Discord.Commands;
using WizBot.Common.Attributes;
using WizBot.Modules.Utility.Services;
using System.Threading.Tasks;

namespace WizBot.Modules.Utility
{
    public partial class Utility
    {
        [Group]
        public class VerboseErrorCommands : WizBotSubmodule<VerboseErrorsService>
        {
            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageMessages)]
            public async Task VerboseError(bool? newstate = null)
            {
                var state = _service.ToggleVerboseErrors(ctx.Guild.Id, newstate);

                if (state)
                    await ReplyConfirmLocalizedAsync("verbose_errors_enabled").ConfigureAwait(false);
                else
                    await ReplyConfirmLocalizedAsync("verbose_errors_disabled").ConfigureAwait(false);
            }
        }
    }
}
