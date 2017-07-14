using Discord;
using Discord.Commands;
using WizBot.Attributes;
using WizBot.Services.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WizBot.Modules.Utility.Commands
{
    public class StreamRoleCommands  : WizBotSubModule
    {
        private readonly StreamRoleService service;

        public StreamRoleCommands(StreamRoleService service)
        {
            this.service = service;
        }

        [WizBotCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task StreamRole(IRole fromRole, IRole addRole)
        {
            this.service.SetStreamRole(fromRole, addRole);

            await ReplyConfirmLocalized("stream_role_enabled", Format.Bold(fromRole.ToString()), Format.Bold(addRole.ToString())).ConfigureAwait(false);
        }

        [WizBotCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task StreamRole()
        {
            this.service.StopStreamRole(Context.Guild.Id);
            await ReplyConfirmLocalized("stream_role_disabled").ConfigureAwait(false);
        }
    }
}