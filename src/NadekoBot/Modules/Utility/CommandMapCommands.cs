using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Extensions;
using NadekoBot.Db.Models;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NadekoBot.Common.Attributes;
using NadekoBot.Services;
using NadekoBot.Services.Database.Models;
using NadekoBot.Db;
using NadekoBot.Modules.Administration;
using NadekoBot.Modules.Utility.Services;

namespace NadekoBot.Modules.Utility
{
    public partial class Utility
    {
        [Group]
        public class CommandMapCommands : NadekoSubmodule<CommandMapService>
        {
            private readonly DbService _db;
            private readonly DiscordSocketClient _client;

            public CommandMapCommands(DbService db, DiscordSocketClient client)
            {
                _db = db;
                _client = client;
            }

            [NadekoCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.Administrator)]
            public async Task AliasesClear()
            {
                var count = _service.ClearAliases(ctx.Guild.Id);
                await ReplyConfirmLocalizedAsync(strs.aliases_cleared(count));
            }

            [NadekoCommand, Aliases]
            [UserPerm(GuildPerm.Administrator)]
            [RequireContext(ContextType.Guild)]
            public async Task Alias(string trigger, [Leftover] string mapping = null)
            {
                var channel = (ITextChannel)ctx.Channel;

                if (string.IsNullOrWhiteSpace(trigger))
                    return;

                trigger = trigger.Trim().ToLowerInvariant();

                if (string.IsNullOrWhiteSpace(mapping))
                {
                    if (!_service.AliasMaps.TryGetValue(ctx.Guild.Id, out var maps) ||
                        !maps.TryRemove(trigger, out _))
                    {
                        await ReplyErrorLocalizedAsync(strs.alias_remove_fail(Format.Code(trigger))).ConfigureAwait(false);
                        return;
                    }

                    using (var uow = _db.GetDbContext())
                    {
                        var config = uow.GuildConfigsForId(ctx.Guild.Id, set => set.Include(x => x.CommandAliases));
                        var toAdd = new CommandAlias()
                        {
                            Mapping = mapping,
                            Trigger = trigger
                        };
                        var tr = config.CommandAliases.FirstOrDefault(x => x.Trigger == trigger);
                        if (tr != null)
                            uow.Set<CommandAlias>().Remove(tr);
                        uow.SaveChanges();
                    }

                    await ReplyConfirmLocalizedAsync(strs.alias_removed(Format.Code(trigger))).ConfigureAwait(false);
                    return;
                }
                _service.AliasMaps.AddOrUpdate(ctx.Guild.Id, (_) =>
                {
                    using (var uow = _db.GetDbContext())
                    {
                        var config = uow.GuildConfigsForId(ctx.Guild.Id, set => set.Include(x => x.CommandAliases));
                        config.CommandAliases.Add(new CommandAlias()
                        {
                            Mapping = mapping,
                            Trigger = trigger
                        });
                        uow.SaveChanges();
                    }
                    return new ConcurrentDictionary<string, string>(new Dictionary<string, string>() {
                        {trigger.Trim().ToLowerInvariant(), mapping.ToLowerInvariant() },
                    });
                }, (_, map) =>
                {
                    using (var uow = _db.GetDbContext())
                    {
                        var config = uow.GuildConfigsForId(ctx.Guild.Id, set => set.Include(x => x.CommandAliases));
                        var toAdd = new CommandAlias()
                        {
                            Mapping = mapping,
                            Trigger = trigger
                        };
                        var toRemove = config.CommandAliases.Where(x => x.Trigger == trigger);
                        if (toRemove.Any())
                            uow.RemoveRange(toRemove.ToArray());
                        config.CommandAliases.Add(toAdd);
                        uow.SaveChanges();
                    }
                    map.AddOrUpdate(trigger, mapping, (key, old) => mapping);
                    return map;
                });

                await ReplyConfirmLocalizedAsync(strs.alias_added(Format.Code(trigger), Format.Code(mapping))).ConfigureAwait(false);
            }


            [NadekoCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task AliasList(int page = 1)
            {
                var channel = (ITextChannel)ctx.Channel;
                page -= 1;

                if (page < 0)
                    return;

                if (!_service.AliasMaps.TryGetValue(ctx.Guild.Id, out var maps) || !maps.Any())
                {
                    await ReplyErrorLocalizedAsync(strs.aliases_none).ConfigureAwait(false);
                    return;
                }

                var arr = maps.ToArray();

                await ctx.SendPaginatedConfirmAsync(page, (curPage) =>
                {
                    return _eb.Create().WithOkColor()
                    .WithTitle(GetText(strs.alias_list))
                    .WithDescription(string.Join("\n",
                        arr.Skip(curPage * 10).Take(10).Select(x => $"`{x.Key}` => `{x.Value}`")));

                }, arr.Length, 10).ConfigureAwait(false);
            }
        }
    }
}