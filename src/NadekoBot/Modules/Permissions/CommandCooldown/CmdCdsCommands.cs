#nullable disable
using Microsoft.EntityFrameworkCore;
using NadekoBot.Common.TypeReaders;
using NadekoBot.Db;
using NadekoBot.Modules.Permissions.Services;
using NadekoBot.Services.Database.Models;

namespace NadekoBot.Modules.Permissions;

public partial class Permissions
{
    [Group]
    public partial class CmdCdsCommands : NadekoModule
    {
        private ConcurrentDictionary<ulong, ConcurrentHashSet<CommandCooldown>> CommandCooldowns
            => _service.CommandCooldowns;

        private ConcurrentDictionary<ulong, ConcurrentHashSet<ActiveCooldown>> ActiveCooldowns
            => _service.ActiveCooldowns;

        private readonly DbService _db;
        private readonly CmdCdService _service;

        public CmdCdsCommands(CmdCdService service, DbService db)
        {
            _service = service;
            _db = db;
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async partial Task CmdCooldown(CommandOrCrInfo command, int secs)
        {
            var channel = (ITextChannel)ctx.Channel;
            if (secs is < 0 or > 3600)
            {
                await ReplyErrorLocalizedAsync(strs.invalid_second_param_between(0, 3600));
                return;
            }

            var name = command.Name.ToLowerInvariant();
            await using (var uow = _db.GetDbContext())
            {
                var config = uow.GuildConfigsForId(channel.Guild.Id, set => set.Include(gc => gc.CommandCooldowns));
                var localSet = CommandCooldowns.GetOrAdd(channel.Guild.Id, new ConcurrentHashSet<CommandCooldown>());

                var toDelete = config.CommandCooldowns.FirstOrDefault(cc => cc.CommandName == name);
                if (toDelete is not null)
                    uow.Set<CommandCooldown>().Remove(toDelete);
                localSet.RemoveWhere(cc => cc.CommandName == name);
                if (secs != 0)
                {
                    var cc = new CommandCooldown
                    {
                        CommandName = name,
                        Seconds = secs
                    };
                    config.CommandCooldowns.Add(cc);
                    localSet.Add(cc);
                }

                await uow.SaveChangesAsync();
            }

            if (secs == 0)
            {
                var activeCds = ActiveCooldowns.GetOrAdd(channel.Guild.Id, new ConcurrentHashSet<ActiveCooldown>());
                activeCds.RemoveWhere(ac => ac.Command == name);
                await ReplyConfirmLocalizedAsync(strs.cmdcd_cleared(Format.Bold(name)));
            }
            else
                await ReplyConfirmLocalizedAsync(strs.cmdcd_add(Format.Bold(name), Format.Bold(secs.ToString())));
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async partial Task AllCmdCooldowns()
        {
            var channel = (ITextChannel)ctx.Channel;
            var localSet = CommandCooldowns.GetOrAdd(channel.Guild.Id, new ConcurrentHashSet<CommandCooldown>());

            if (!localSet.Any())
                await ReplyConfirmLocalizedAsync(strs.cmdcd_none);
            else
            {
                await channel.SendTableAsync("",
                    localSet.Select(c => c.CommandName + ": " + c.Seconds + GetText(strs.sec)),
                    s => $"{s,-30}",
                    2);
            }
        }
    }
}