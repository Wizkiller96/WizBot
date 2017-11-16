using Discord;
using Discord.Commands;
using System.Threading.Tasks;
using WizBot.Common.Attributes;
using WizBot.Modules.Permissions.Services;

namespace WizBot.Modules.Permissions
{
    public partial class Permissions
    {
        [Group]
        public class ResetPermissionsCommands : WizBotSubmodule
        {
            private readonly ResetPermissionsService _service;

            public ResetPermissionsCommands(ResetPermissionsService service)
            {
                _service = service;
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.Administrator)]
            public async Task ResetPermissions()
            {
                await _service.ResetPermissions(Context.Guild.Id).ConfigureAwait(false);
                await ReplyConfirmLocalized("perms_reset").ConfigureAwait(false);
            }

            [WizBotCommand, Usage, Description, Aliases]
            [OwnerOnly]
            [AdminOnly]
            public async Task ResetGlobalPermissions()
            {
                await _service.ResetGlobalPermissions().ConfigureAwait(false);
                await ReplyConfirmLocalized("global_perms_reset").ConfigureAwait(false);
            }
        }
    }
}
