using System;
using Discord;
using Discord.Commands;
using NadekoBot.Common.Attributes;
using NadekoBot.Common.TypeReaders;
using NadekoBot.Core.Services.Database.Models;
using NadekoBot.Modules.Permissions.Services;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using NadekoBot.Extensions;
using Serilog;

namespace NadekoBot.Modules.Permissions
{
    public partial class Permissions
    {
        [Group]
        public class BlacklistCommands : NadekoSubmodule<BlacklistService>
        {
            private readonly DiscordSocketClient _client;

            public BlacklistCommands(DiscordSocketClient client)
            {
                _client = client;
            }
            
            private async Task ListBlacklistInternal(string title, BlacklistType type, int page = 0)
            {
                if (page < 0)
                    throw new ArgumentOutOfRangeException(nameof(page));
                
                var list = _service.GetBlacklist();
                var items = await list
                    .Where(x => x.Type == type)
                    .Select(async i =>
                    {
                        try
                        {
                            return i.Type switch
                            {
                                BlacklistType.Channel => Format.Code(i.ItemId.ToString())
                                                         + " " + (_client.GetChannel(i.ItemId)?.ToString() ?? ""),
                                BlacklistType.User => Format.Code(i.ItemId.ToString())
                                                      + " " +
                                                      ((await _client.Rest.GetUserAsync(i.ItemId))?.ToString() ?? ""),
                                BlacklistType.Server => Format.Code(i.ItemId.ToString())
                                                        + " " + (_client.GetGuild(i.ItemId)?.ToString() ?? ""),
                                _ => Format.Code(i.ItemId.ToString())
                            };
                        }
                        catch
                        {
                            Log.Warning("Can't get {BlacklistType} [{BlacklistItemId}]", i.Type, i.ItemId);
                            return Format.Code(i.ItemId.ToString());
                        }
                    })
                    .WhenAll();

                await ctx.SendPaginatedConfirmAsync(page, (int curPage) =>
                {
                    var pageItems = items
                        .Skip(10 * curPage)
                        .Take(10)
                        .ToList();
                    
                    if (pageItems.Count == 0)
                    {
                        return new EmbedBuilder()
                            .WithOkColor()
                            .WithTitle(title)
                            .WithDescription(GetText("empty_page"));
                    }
                    
                    return new EmbedBuilder()
                        .WithTitle(title)
                        .WithDescription(pageItems.JoinWith('\n'))
                        .WithOkColor();
                }, items.Length, 10);
            }
            
            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public Task UserBlacklist(int page = 1)
            {
                if (--page < 0)
                    return Task.CompletedTask;
                
                return ListBlacklistInternal(GetText("blacklisted_users"), BlacklistType.User, page);
            }
            
            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public Task ChannelBlacklist(int page = 1)
            {
                if (--page < 0)
                    return Task.CompletedTask;
                
                return ListBlacklistInternal(GetText("blacklisted_channels"), BlacklistType.Channel, page);
            }
            
            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public Task ServerBlacklist(int page = 1)
            {
                if (--page < 0)
                    return Task.CompletedTask;
                
                return ListBlacklistInternal(GetText("blacklisted_servers"), BlacklistType.Server, page);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public Task UserBlacklist(AddRemove action, ulong id)
                => Blacklist(action, id, BlacklistType.User);

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public Task UserBlacklist(AddRemove action, IUser usr)
                => Blacklist(action, usr.Id, BlacklistType.User);

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public Task ChannelBlacklist(AddRemove action, ulong id)
                => Blacklist(action, id, BlacklistType.Channel);

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public Task ServerBlacklist(AddRemove action, ulong id)
                => Blacklist(action, id, BlacklistType.Server);

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public Task ServerBlacklist(AddRemove action, IGuild guild)
                => Blacklist(action, guild.Id, BlacklistType.Server);

            private async Task Blacklist(AddRemove action, ulong id, BlacklistType type)
            {
                if (action == AddRemove.Add)
                {
                    _service.Blacklist(type, id);
                }
                else
                {
                    _service.UnBlacklist(type, id);
                }

                if (action == AddRemove.Add)
                    await ReplyConfirmLocalizedAsync("blacklisted", Format.Code(type.ToString()),
                        Format.Code(id.ToString())).ConfigureAwait(false);
                else
                    await ReplyConfirmLocalizedAsync("unblacklisted", Format.Code(type.ToString()),
                        Format.Code(id.ToString())).ConfigureAwait(false);
            }
        }
    }
}
