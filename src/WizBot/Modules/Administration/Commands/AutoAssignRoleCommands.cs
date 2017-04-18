using Discord;
using Discord.Commands;
using WizBot.Attributes;
using WizBot.Extensions;
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
            private static ConcurrentDictionary<ulong, ulong> AutoAssignedRoles { get; }

            static AutoAssignRoleCommands()
            {
                var log = LogManager.GetCurrentClassLogger();

                AutoAssignedRoles = new ConcurrentDictionary<ulong, ulong>(WizBot.AllGuildConfigs.Where(x => x.AutoAssignRoleId != 0)
                    .ToDictionary(k => k.GuildId, v => v.AutoAssignRoleId));
                WizBot.Client.UserJoined += async (user) =>
                {
                    try
                    {
                        AutoAssignedRoles.TryGetValue(user.Guild.Id, out ulong roleId);

                        if (roleId == 0)
                            return;

                        var role = user.Guild.Roles.FirstOrDefault(r => r.Id == roleId);

                        if (role != null)
                            await user.AddRoleAsync(role).ConfigureAwait(false);
                    }
                    catch (Exception ex) { log.Warn(ex); }
                };
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

                using (var uow = DbHandler.UnitOfWork())
                {
                    var conf = uow.GuildConfigs.For(Context.Guild.Id, set => set);
                    if (role == null)
                    {
                        conf.AutoAssignRoleId = 0;
                        AutoAssignedRoles.TryRemove(Context.Guild.Id, out ulong throwaway);
                    }
                    else
                    {
                        conf.AutoAssignRoleId = role.Id;
                        AutoAssignedRoles.AddOrUpdate(Context.Guild.Id, role.Id, (key, val) => role.Id);
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
