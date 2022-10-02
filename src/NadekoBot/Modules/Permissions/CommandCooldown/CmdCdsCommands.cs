#nullable disable
using Humanizer.Localisation;
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
        private readonly DbService _db;
        private readonly CmdCdService _service;

        public CmdCdsCommands(CmdCdService service, DbService db)
        {
            _service = service;
            _db = db;
        }

        private async Task CmdCooldownInternal(string cmdName, int secs)
        {
            var channel = (ITextChannel)ctx.Channel;
            if (secs is < 0 or > 3600)
            {
                await ReplyErrorLocalizedAsync(strs.invalid_second_param_between(0, 3600));
                return;
            }

            var name = cmdName.ToLowerInvariant();
            await using (var uow = _db.GetDbContext())
            {
                var config = uow.GuildConfigsForId(channel.Guild.Id, set => set.Include(gc => gc.CommandCooldowns));

                var toDelete = config.CommandCooldowns.FirstOrDefault(cc => cc.CommandName == name);
                if (toDelete is not null)
                    uow.Set<CommandCooldown>().Remove(toDelete);
                if (secs != 0)
                {
                    var cc = new CommandCooldown
                    {
                        CommandName = name,
                        Seconds = secs
                    };
                    config.CommandCooldowns.Add(cc);
                    _service.AddCooldown(channel.Guild.Id, name, secs);
                }

                await uow.SaveChangesAsync();
            }

            if (secs == 0)
            {
                _service.ClearCooldowns(ctx.Guild.Id, cmdName);
                await ReplyConfirmLocalizedAsync(strs.cmdcd_cleared(Format.Bold(name)));
            }
            else
                await ReplyConfirmLocalizedAsync(strs.cmdcd_add(Format.Bold(name), Format.Bold(secs.ToString())));
        }
        
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [Priority(0)]
        public Task CmdCooldown(CleverBotResponseStr command, int secs)
            => CmdCooldownInternal(CleverBotResponseStr.CLEVERBOT_RESPONSE, secs);

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [Priority(1)]
        public Task CmdCooldown(CommandOrExprInfo command, int secs)
            => CmdCooldownInternal(command.Name, secs);

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task AllCmdCooldowns(int page = 1)
        {
            if (--page < 0)
                return;
            
            var channel = (ITextChannel)ctx.Channel;
            var localSet = _service.GetCommandCooldowns(ctx.Guild.Id);

            if (!localSet.Any())
                await ReplyConfirmLocalizedAsync(strs.cmdcd_none);
            else
            {
                await ctx.SendPaginatedConfirmAsync(page, curPage =>
                {
                    var items = localSet.Skip(curPage * 15)
                        .Take(15)
                        .Select(x => $"{Format.Code(x.CommandName)}: {x.Seconds.Seconds().Humanize(maxUnit: TimeUnit.Second, culture: Culture)}");

                    return _eb.Create(ctx)
                        .WithOkColor()
                        .WithDescription(items.Join("\n"));

                }, localSet.Count, 15);
            }
        }
    }
}