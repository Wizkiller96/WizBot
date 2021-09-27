using System;
using Discord;
using Discord.Commands;
using WizBot.Common.Attributes;
using WizBot.Common.TypeReaders;
using WizBot.Services.Database.Models;
using WizBot.Modules.Permissions.Services;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using WizBot.Extensions;
using Serilog;

namespace WizBot.Modules.Permissions
{
    public partial class Permissions
    {
        [Group]
        public class BlacklistCommands : WizBotSubmodule<BlacklistService>
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
                        return _eb.Create()
                            .WithOkColor()
                            .WithTitle(title)
                            .WithDescription(GetText(strs.empty_page));
                    }
                    
                    return _eb.Create()
                        .WithTitle(title)
                        .WithDescription(pageItems.JoinWith('\n'))
                        .WithOkColor();
                }, items.Length, 10);
            }
            
            [WizBotCommand, Aliases]
            [OwnerOnly]
            public Task UserBlacklist(int page = 1)
            {
                if (--page < 0)
                    return Task.CompletedTask;
                
                return ListBlacklistInternal(GetText(strs.blacklisted_users), BlacklistType.User, page);
            }
            
            [WizBotCommand, Aliases]
            [OwnerOnly]
            public Task ChannelBlacklist(int page = 1)
            {
                if (--page < 0)
                    return Task.CompletedTask;
                
                return ListBlacklistInternal(GetText(strs.blacklisted_channels), BlacklistType.Channel, page);
            }
            
            [WizBotCommand, Aliases]
            [OwnerOnly]
            public Task ServerBlacklist(int page = 1)
            {
                if (--page < 0)
                    return Task.CompletedTask;
                
                return ListBlacklistInternal(GetText(strs.blacklisted_servers), BlacklistType.Server, page);
            }

            [WizBotCommand, Aliases]
            [OwnerOnly]
            public Task UserBlacklist(AddRemove action, ulong id)
                => Blacklist(action, id, BlacklistType.User);

            [WizBotCommand, Aliases]
            [OwnerOnly]
            public Task UserBlacklist(AddRemove action, IUser usr)
                => Blacklist(action, usr.Id, BlacklistType.User);

            [WizBotCommand, Aliases]
            [OwnerOnly]
            public Task ChannelBlacklist(AddRemove action, ulong id)
                => Blacklist(action, id, BlacklistType.Channel);

            [WizBotCommand, Aliases]
            [OwnerOnly]
            public Task ServerBlacklist(AddRemove action, ulong id)
                => Blacklist(action, id, BlacklistType.Server);

            [WizBotCommand, Aliases]
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
                    await ReplyConfirmLocalizedAsync(strs.blacklisted(Format.Code(type.ToString()),
                        Format.Code(id.ToString())));
                else
                    await ReplyConfirmLocalizedAsync(strs.unblacklisted(Format.Code(type.ToString()),
                        Format.Code(id.ToString())));
            }
        }
    }
}
