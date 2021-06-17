﻿using Discord.Commands;
using Discord;
using System.Linq;
using System.Threading.Tasks;
using WizBot.Core.Services;
using System.Collections.Generic;
using WizBot.Core.Services.Database.Models;
using Microsoft.EntityFrameworkCore;
using WizBot.Common.Attributes;
using WizBot.Extensions;
using WizBot.Modules.Searches.Services;
using Discord.WebSocket;

namespace WizBot.Modules.Searches
{
    public partial class Searches
    {
        [Group]
        public class StreamNotificationCommands : WizBotSubmodule<StreamNotificationService>
        {
            private readonly DbService _db;

            public StreamNotificationCommands(DbService db)
            {
                _db = db;
            }

            // private static readonly Regex picartoRegex = new Regex(@"picarto.tv/(?<name>.+[^/])/?",
            //     RegexOptions.Compiled | RegexOptions.IgnoreCase);

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageMessages)]
            public async Task StreamAdd(string link)
            {
                var data = await _service.FollowStream(ctx.Guild.Id, ctx.Channel.Id, link);
                if (data is null)
                {
                    await ReplyErrorLocalizedAsync("stream_not_added").ConfigureAwait(false);
                    return;
                }

                var embed = _service.GetEmbed(ctx.Guild.Id, data);
                await ctx.Channel.EmbedAsync(embed, GetText("stream_tracked")).ConfigureAwait(false);
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageMessages)]
            [Priority(1)]
            public async Task StreamRemove(int index)
            {
                if (--index < 0)
                    return;
                
                var fs = await _service.UnfollowStreamAsync(ctx.Guild.Id, index);
                if (fs is null)
                {
                    await ReplyErrorLocalizedAsync("stream_no").ConfigureAwait(false);
                    return;
                }
            
                await ReplyConfirmLocalizedAsync(
                    "stream_removed", 
                    Format.Bold(fs.Username), 
                    fs.Type).ConfigureAwait(false);
            }

            // [WizBotCommand, Usage, Description, Aliases]
            // [RequireContext(ContextType.Guild)]
            // [UserPerm(GuildPerm.Administrator)]
            // public async Task StreamsClear()
            // {
            //     var count = _service.ClearAllStreams(ctx.Guild.Id);
            //     await ReplyConfirmLocalizedAsync("streams_cleared", count).ConfigureAwait(false);
            // }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task StreamList(int page = 1)
            {
                if (page-- < 1)
                {
                    return;
                }

                List<FollowedStream> streams = new List<FollowedStream>();
                using (var uow = _db.GetDbContext())
                {
                    var all = uow.GuildConfigs
                        .ForId(ctx.Guild.Id, set => set.Include(gc => gc.FollowedStreams))
                        .FollowedStreams
                        .OrderBy(x => x.Id)
                        .ToList();

                    for (var index = all.Count - 1; index >= 0; index--)
                    {
                        var fs = all[index];
                        if (((SocketGuild) ctx.Guild).GetTextChannel(fs.ChannelId) is null)
                        {
                            await _service.UnfollowStreamAsync(fs.GuildId, index);
                        }
                        else
                        {
                            streams.Insert(0, fs);
                        }
                    }
                }

                await ctx.SendPaginatedConfirmAsync(page, (cur) =>
                {
                    var elements = streams.Skip(cur * 12).Take(12)
                        .ToList();

                    if (elements.Count == 0)
                    {
                        return new EmbedBuilder()
                            .WithDescription(GetText("streams_none"))
                            .WithErrorColor();
                    }

                    var eb = new EmbedBuilder()
                        .WithTitle(GetText("streams_follow_title"))
                        .WithOkColor();
                    for (var index = 0; index < elements.Count; index++)
                    {
                        var elem = elements[index];
                        eb.AddField(
                            $"**#{(index + 1) + (12 * cur)}** {elem.Username.ToLower()}",
                            $"【{elem.Type}】\n<#{elem.ChannelId}>\n{elem.Message?.TrimTo(50)}",
                            true);
                    }

                    return eb;
                }, streams.Count, 12).ConfigureAwait(false);
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageMessages)]
            public async Task StreamOffline()
            {
                var newValue = _service.ToggleStreamOffline(ctx.Guild.Id);
                if (newValue)
                {
                    await ReplyConfirmLocalizedAsync("stream_off_enabled").ConfigureAwait(false);
                }
                else
                {
                    await ReplyConfirmLocalizedAsync("stream_off_disabled").ConfigureAwait(false);
                }
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageMessages)]
            public async Task StreamMessage(int index, [Leftover] string message)
            {
                if (--index < 0)
                    return;
                
                if (!_service.SetStreamMessage(ctx.Guild.Id, index, message, out var fs))
                {
                    await ReplyConfirmLocalizedAsync("stream_not_following").ConfigureAwait(false);
                    return;
                }
            
                if (string.IsNullOrWhiteSpace(message))
                {
                    await ReplyConfirmLocalizedAsync("stream_message_reset", Format.Bold(fs.Username))
                        .ConfigureAwait(false);
                }
                else
                {
                    await ReplyConfirmLocalizedAsync("stream_message_set", Format.Bold(fs.Username))
                        .ConfigureAwait(false);
                }
            }
            
            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageMessages)]
            public async Task StreamMessageAll([Leftover] string message)
            {
                var count = _service.SetStreamMessageForAll(ctx.Guild.Id, message);

                if (count == 0)
                {
                    await ReplyConfirmLocalizedAsync("stream_not_following_any");
                    return;
                }

                await ReplyConfirmLocalizedAsync("stream_message_set_all", count);
            }
            
            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task StreamCheck(string url)
            {
                try
                {
                    var data = await _service.GetStreamDataAsync(url).ConfigureAwait(false);
                    if (data is null)
                    {
                        await ReplyErrorLocalizedAsync("no_channel_found").ConfigureAwait(false);
                        return;
                    }
                    
                    if (data.IsLive)
                    {
                        await ReplyConfirmLocalizedAsync("streamer_online",
                                Format.Bold(data.Name),
                                Format.Bold(data.Viewers.ToString()))
                            .ConfigureAwait(false);
                    }
                    else
                    {
                        await ReplyConfirmLocalizedAsync("streamer_offline", data.Name)
                            .ConfigureAwait(false);
                    }
                }
                catch
                {
                    await ReplyErrorLocalizedAsync("no_channel_found").ConfigureAwait(false);
                }
            }
        }
    }
}