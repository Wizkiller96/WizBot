using Discord;
using Discord.Commands;
using WizBot.Extensions;
using System.Linq;
using System.Threading.Tasks;
using WizBot.Common.Attributes;
using WizBot.Modules.Administration.Services;

namespace WizBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        public class AutoAssignRoleCommands : WizBotSubmodule<AutoAssignRoleService>
        {
            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageRoles)]
            public async Task AutoAssignRole([Leftover] IRole role = null)
            {
                var guser = (IGuildUser) ctx.User;
                if (role != null)
                {
                    if (role.Id == ctx.Guild.EveryoneRole.Id)
                        return;

                    // the user can't aar the role which is higher or equal to his highest role
                    if (ctx.User.Id != guser.Guild.OwnerId && guser.GetRoles().Max(x => x.Position) <= role.Position)
                        return;

                    _service.EnableAar(ctx.Guild.Id, role.Id);
                    await ReplyConfirmLocalizedAsync("aar_enabled").ConfigureAwait(false);
                }
                else
                {
                    _service.DisableAar(ctx.Guild.Id);
                    await ReplyConfirmLocalizedAsync("aar_disabled").ConfigureAwait(false);
                    return;
                }
            }
        }
    }
}
