using Discord;
using Discord.Commands;
using WizBot.Extensions;
using WizBot.Core.Services;
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
            private readonly DbService _db;

            public AutoAssignRoleCommands(DbService db)
            {
                _db = db;
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            public async Task AutoAssignRole([Remainder] IRole role = null)
            {
                var guser = (IGuildUser)Context.User;
                if (role != null)
                    if (Context.User.Id != guser.Guild.OwnerId && guser.GetRoles().Max(x => x.Position) <= role.Position)
                        return;

                using (var uow = _db.UnitOfWork)
                {
                    var conf = uow.GuildConfigs.For(Context.Guild.Id, set => set);
                    if (role == null)
                    {
                        conf.AutoAssignRoleId = 0;
                        _service.AutoAssignedRoles.TryRemove(Context.Guild.Id, out _);
                    }
                    else
                    {
                        conf.AutoAssignRoleId = role.Id;
                        _service.AutoAssignedRoles.AddOrUpdate(Context.Guild.Id, role.Id, (key, val) => role.Id);
                    }

                    await uow.CompleteAsync().ConfigureAwait(false);
                }

                if (role == null)
                {
                    await ReplyConfirmLocalized("aar_disabled").ConfigureAwait(false);
                    return;
                }

                await ReplyConfirmLocalized("aar_enabled").ConfigureAwait(false);
            }
        }
    }
}
