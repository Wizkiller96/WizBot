using Discord;
using Discord.Commands;
using WizBot.Attributes;
using WizBot.Services;
using WizBot.Services.Database.Models;
using WizBot.Services.Permissions;
using System.Threading.Tasks;

namespace WizBot.Modules.Permissions.Commands
{
    public partial class Permissions
    {
        [Group]
        public class ResetPermissionsCommands : WizBotSubmodule
        {
            private readonly PermissionsService _service;
            private readonly DbHandler _db;
            private readonly GlobalPermissionService _globalPerms;

            public ResetPermissionsCommands(PermissionsService service, GlobalPermissionService globalPerms, DbHandler db)
            {
                _service = service;
                _db = db;
                _globalPerms = globalPerms;
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.Administrator)]
            public async Task ResetPermissions()
            {
                //todo 80 move to service
                using (var uow = _db.UnitOfWork)
                {
                    var config = uow.GuildConfigs.GcWithPermissionsv2For(Context.Guild.Id);
                    config.Permissions = Permissionv2.GetDefaultPermlist;
                    await uow.CompleteAsync();
                    _service.UpdateCache(config);
                }
                await ReplyConfirmLocalized("perms_reset").ConfigureAwait(false);
            }

            [WizBotCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task ResetGlobalPermissions()
            {
                //todo 80 move to service
                using (var uow = _db.UnitOfWork)
                {
                    var gc = uow.BotConfig.GetOrCreate();
                    gc.BlockedCommands.Clear();
                    gc.BlockedModules.Clear();

                    _globalPerms.BlockedCommands.Clear();
                    _globalPerms.BlockedModules.Clear();
                    await uow.CompleteAsync();
                }
                await ReplyConfirmLocalized("global_perms_reset").ConfigureAwait(false);
            }
        }
    }
}