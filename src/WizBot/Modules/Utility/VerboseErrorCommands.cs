using Discord.Commands;
using System.Threading.Tasks;
using WizBot.Common.Attributes;
using WizBot.Modules.Utility.Services;

namespace WizBot.Modules.Utility
{
    public partial class Utility
    {
        [Group]
        public class VerboseErrorCommands : WizBotSubmodule<VerboseErrorsService>
        {
            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(Discord.GuildPermission.ManageMessages)]
            public async Task VerboseError()
            {
                var state = _service.ToggleVerboseErrors(Context.Guild.Id);

                if (state)
                    await ReplyConfirmLocalized("verbose_errors_enabled").ConfigureAwait(false);
                else
                    await ReplyConfirmLocalized("verbose_errors_disabled").ConfigureAwait(false);
            }
        }
    }
}
