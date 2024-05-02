#nullable disable
using Microsoft.EntityFrameworkCore;
using NadekoBot.Db;
using NadekoBot.Modules.Utility.Services;
using NadekoBot.Db.Models;

namespace NadekoBot.Modules.Utility;

public partial class Utility
{
    [Group]
    public partial class CommandMapCommands : NadekoModule<AliasService>
    {
        private readonly DbService _db;
        private readonly DiscordSocketClient _client;

        public CommandMapCommands(DbService db, DiscordSocketClient client)
        {
            _db = db;
            _client = client;
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task AliasesClear()
        {
            var count = _service.ClearAliases(ctx.Guild.Id);
            await Response().Confirm(strs.aliases_cleared(count)).SendAsync();
        }

        [Cmd]
        [UserPerm(GuildPerm.Administrator)]
        [RequireContext(ContextType.Guild)]
        public async Task Alias(string trigger, [Leftover] string mapping = null)
        {
            if (string.IsNullOrWhiteSpace(trigger))
                return;

            trigger = trigger.Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(mapping))
            {
                if (!_service.AliasMaps.TryGetValue(ctx.Guild.Id, out var maps) || !maps.TryRemove(trigger, out _))
                {
                    await Response().Error(strs.alias_remove_fail(Format.Code(trigger))).SendAsync();
                    return;
                }

                await using (var uow = _db.GetDbContext())
                {
                    var config = uow.GuildConfigsForId(ctx.Guild.Id, set => set.Include(x => x.CommandAliases));
                    var tr = config.CommandAliases.FirstOrDefault(x => x.Trigger == trigger);
                    if (tr is not null)
                        uow.Set<CommandAlias>().Remove(tr);
                    uow.SaveChanges();
                }

                await Response().Confirm(strs.alias_removed(Format.Code(trigger))).SendAsync();
                return;
            }

            _service.AliasMaps.AddOrUpdate(ctx.Guild.Id,
                _ =>
                {
                    using (var uow = _db.GetDbContext())
                    {
                        var config = uow.GuildConfigsForId(ctx.Guild.Id, set => set.Include(x => x.CommandAliases));
                        config.CommandAliases.Add(new()
                        {
                            Mapping = mapping,
                            Trigger = trigger
                        });
                        uow.SaveChanges();
                    }

                    return new(new Dictionary<string, string>
                    {
                        { trigger.Trim().ToLowerInvariant(), mapping.ToLowerInvariant() }
                    });
                },
                (_, map) =>
                {
                    using (var uow = _db.GetDbContext())
                    {
                        var config = uow.GuildConfigsForId(ctx.Guild.Id, set => set.Include(x => x.CommandAliases));
                        var toAdd = new CommandAlias
                        {
                            Mapping = mapping,
                            Trigger = trigger
                        };
                        var toRemove = config.CommandAliases.Where(x => x.Trigger == trigger).ToArray();
                        if (toRemove.Any())
                            uow.RemoveRange(toRemove);
                        config.CommandAliases.Add(toAdd);
                        uow.SaveChanges();
                    }

                    map.AddOrUpdate(trigger, mapping, (_, _) => mapping);
                    return map;
                });

            await Response().Confirm(strs.alias_added(Format.Code(trigger), Format.Code(mapping))).SendAsync();
        }


        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task AliasList(int page = 1)
        {
            page -= 1;

            if (page < 0)
                return;

            if (!_service.AliasMaps.TryGetValue(ctx.Guild.Id, out var maps) || !maps.Any())
            {
                await Response().Error(strs.aliases_none).SendAsync();
                return;
            }

            var arr = maps.ToArray();

            await Response()
                  .Paginated()
                  .Items(arr)
                  .PageSize(10)
                  .CurrentPage(page)
                  .Page((items, _) =>
                  {
                      return _sender.CreateEmbed()
                             .WithOkColor()
                             .WithTitle(GetText(strs.alias_list))
                             .WithDescription(string.Join("\n", items.Select(x => $"`{x.Key}` => `{x.Value}`")));
                  })
                  .SendAsync();
        }
    }
}