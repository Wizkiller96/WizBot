#nullable disable
using Microsoft.EntityFrameworkCore;
using WizBot.Common.TypeReaders;
using WizBot.Db;
using WizBot.Modules.Permissions.Services;
using WizBot.Db.Models;

namespace WizBot.Modules.Permissions;

public partial class Permissions
{
    [Group]
    public partial class CmdCdsCommands : WizBotModule
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
                await Response().Error(strs.invalid_second_param_between(0, 3600)).SendAsync();
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
                await Response().Confirm(strs.cmdcd_cleared(Format.Bold(name))).SendAsync();
            }
            else
                await Response().Confirm(strs.cmdcd_add(Format.Bold(name), Format.Bold(secs.ToString()))).SendAsync();
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

            var localSet = _service.GetCommandCooldowns(ctx.Guild.Id);

            if (!localSet.Any())
                await Response().Confirm(strs.cmdcd_none).SendAsync();
            else
            {
                await Response()
                      .Paginated()
                      .Items(localSet)
                      .PageSize(15)
                      .CurrentPage(page)
                      .Page((items, _) =>
                      {
                          var output = items.Select(x =>
                              $"{Format.Code(x.CommandName)}: {x.Seconds}s");

                          return _sender.CreateEmbed()
                                 .WithOkColor()
                                 .WithDescription(output.Join("\n"));
                      })
                      .SendAsync();
            }
        }
    }
}