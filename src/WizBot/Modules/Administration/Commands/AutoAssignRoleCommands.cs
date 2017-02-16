using Discord;
using Discord.Commands;
using WizBot.Attributes;
using WizBot.Services;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace WizBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        public class AutoAssignRoleCommands : WizBotSubmodule
        {
            //guildid/roleid
            private static ConcurrentDictionary<ulong, ulong> autoAssignedRoles { get; }

            static AutoAssignRoleCommands()
            {
                var log = LogManager.GetCurrentClassLogger();

                autoAssignedRoles = new ConcurrentDictionary<ulong, ulong>(WizBot.AllGuildConfigs.Where(x => x.AutoAssignRoleId != 0)
                    .ToDictionary(k => k.GuildId, v => v.AutoAssignRoleId));
                WizBot.Client.UserJoined += async (user) =>
                {
                    try
                    {
                        ulong roleId;
                        autoAssignedRoles.TryGetValue(user.Guild.Id, out roleId);

                        if (roleId == 0)
                            return;

                        var role = user.Guild.Roles.FirstOrDefault(r => r.Id == roleId);

                        if (role != null)
                            await user.AddRolesAsync(role).ConfigureAwait(false);
                    }
                    catch (Exception ex) { log.Warn(ex); }
                };
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            public async Task AutoAssignRole([Remainder] IRole role = null)
            {
                using (var uow = DbHandler.UnitOfWork())
                {
                    var conf = uow.GuildConfigs.For(Context.Guild.Id, set => set);
                    if (role == null)
                    {
                        conf.AutoAssignRoleId = 0;
                        ulong throwaway;
                        autoAssignedRoles.TryRemove(Context.Guild.Id, out throwaway);
                    }
                    else
                    {
                        conf.AutoAssignRoleId = role.Id;
                        autoAssignedRoles.AddOrUpdate(Context.Guild.Id, role.Id, (key, val) => role.Id);
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