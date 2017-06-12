using Discord.Commands;
using WizBot.Attributes;
using WizBot.Services.Utility;
using System.Threading.Tasks;

namespace WizBot.Modules.Utility
{
    public partial class Utility
    {
        [Group]
        public class VerboseCommandErrors : WizBotSubModule
        {
            private readonly VerboseErrorsService _ves;

            public VerboseCommandErrors(VerboseErrorsService ves)
            {
                _ves = ves;
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(Discord.GuildPermission.ManageMessages)]
            public async Task VerboseError()
            {
                var state = _ves.ToggleVerboseErrors(Context.Guild.Id);

                if (state)
                    await ReplyConfirmLocalized("verbose_errors_enabled").ConfigureAwait(false);
                else
                    await ReplyConfirmLocalized("verbose_errors_disabled").ConfigureAwait(false);
            }
        }
    }
}