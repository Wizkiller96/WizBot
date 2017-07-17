using Discord;
using Discord.Commands;
using System.Threading.Tasks;
using WizBot.Common.Attributes;
using WizBot.Modules.Utility.Services;

namespace WizBot.Modules.Utility
{
    public partial class Utility
    {
        public class StreamRoleCommands : WizBotSubmodule<StreamRoleService>
        {
            [WizBotCommand, Usage, Description, Aliases]
            [RequireBotPermission(GuildPermission.ManageRoles)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            [RequireContext(ContextType.Guild)]
            public async Task StreamRole(IRole fromRole, IRole addRole)
            {
                this._service.SetStreamRole(fromRole, addRole);

                await ReplyConfirmLocalized("stream_role_enabled", Format.Bold(fromRole.ToString()), Format.Bold(addRole.ToString())).ConfigureAwait(false);
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireBotPermission(GuildPermission.ManageRoles)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            [RequireContext(ContextType.Guild)]
            public async Task StreamRole()
            {
                this._service.StopStreamRole(Context.Guild.Id);
                await ReplyConfirmLocalized("stream_role_disabled").ConfigureAwait(false);
            }
        }
    }
}