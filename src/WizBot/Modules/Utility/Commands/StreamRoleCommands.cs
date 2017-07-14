using Discord;
using Discord.Commands;
using WizBot.Attributes;
using WizBot.Services.Utility;
using System.Threading.Tasks;

namespace WizBot.Modules.Utility
{
    public partial class Utility
    {
        public class StreamRoleCommands : WizBotSubModule
        {
            private readonly StreamRoleService service;

            public StreamRoleCommands(StreamRoleService service)
            {
                this.service = service;
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireBotPermission(GuildPermission.ManageRoles)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            [RequireContext(ContextType.Guild)]
            public async Task StreamRole(IRole fromRole, IRole addRole)
            {
                this.service.SetStreamRole(fromRole, addRole);

                await ReplyConfirmLocalized("stream_role_enabled", Format.Bold(fromRole.ToString()), Format.Bold(addRole.ToString())).ConfigureAwait(false);
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireBotPermission(GuildPermission.ManageRoles)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            [RequireContext(ContextType.Guild)]
            public async Task StreamRole()
            {
                this.service.StopStreamRole(Context.Guild.Id);
                await ReplyConfirmLocalized("stream_role_disabled").ConfigureAwait(false);
            }
        }
    }
}