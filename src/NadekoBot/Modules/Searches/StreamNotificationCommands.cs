using Discord.Commands;
using Discord;
using System.Linq;
using System.Threading.Tasks;
using NadekoBot.Services;
using System.Collections.Generic;
using NadekoBot.Services.Database.Models;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Common.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Modules.Searches.Services;
using Discord.WebSocket;
using NadekoBot.Db;
using NadekoBot.Modules.Administration;
using NadekoBot.Db.Models;

namespace NadekoBot.Modules.Searches
{
    public partial class Searches
    {
        [Group]
        public class StreamNotificationCommands : NadekoSubmodule<StreamNotificationService>
        {
            private readonly DbService _db;

            public StreamNotificationCommands(DbService db)
            {
                _db = db;
            }

            // private static readonly Regex picartoRegex = new Regex(@"picarto.tv/(?<name>.+[^/])/?",
            //     RegexOptions.Compiled | RegexOptions.IgnoreCase);

            [NadekoCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageMessages)]
            public async Task StreamAdd(string link)
            {
                var data = await _service.FollowStream(ctx.Guild.Id, ctx.Channel.Id, link);
                if (data is null)
                {
                    await ReplyErrorLocalizedAsync(strs.stream_not_added).ConfigureAwait(false);
                    return;
                }

                var embed = _service.GetEmbed(ctx.Guild.Id, data);
                await ctx.Channel.EmbedAsync(embed, GetText(strs.stream_tracked)).ConfigureAwait(false);
            }

            [NadekoCommand, Aliases]
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
                    await ReplyErrorLocalizedAsync(strs.stream_no).ConfigureAwait(false);
                    return;
                }

                await ReplyConfirmLocalizedAsync(
                    strs.stream_removed(
                        Format.Bold(fs.Username),
                        fs.Type));
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.Administrator)]
            public async Task StreamsClear()
            {
                var count = _service.ClearAllStreams(ctx.Guild.Id);
                await ReplyConfirmLocalizedAsync(strs.streams_cleared);
            }

            [NadekoCommand, Aliases]
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
                    var all = uow
                        .GuildConfigsForId(ctx.Guild.Id, set => set.Include(gc => gc.FollowedStreams))
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
                        return _eb.Create()
                            .WithDescription(GetText(strs.streams_none))
                            .WithErrorColor();
                    }

                    var eb = _eb.Create()
                        .WithTitle(GetText(strs.streams_follow_title))
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

            [NadekoCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageMessages)]
            public async Task StreamOffline()
            {
                var newValue = _service.ToggleStreamOffline(ctx.Guild.Id);
                if (newValue)
                {
                    await ReplyConfirmLocalizedAsync(strs.stream_off_enabled).ConfigureAwait(false);
                }
                else
                {
                    await ReplyConfirmLocalizedAsync(strs.stream_off_disabled).ConfigureAwait(false);
                }
            }

            [NadekoCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageMessages)]
            public async Task StreamMessage(int index, [Leftover] string message)
            {
                if (--index < 0)
                    return;
                
                if (!_service.SetStreamMessage(ctx.Guild.Id, index, message, out var fs))
                {
                    await ReplyConfirmLocalizedAsync(strs.stream_not_following).ConfigureAwait(false);
                    return;
                }
            
                if (string.IsNullOrWhiteSpace(message))
                {
                    await ReplyConfirmLocalizedAsync(strs.stream_message_reset(Format.Bold(fs.Username)));
                }
                else
                {
                    await ReplyConfirmLocalizedAsync(strs.stream_message_set(Format.Bold(fs.Username)));
                }
            }
            
            [NadekoCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageMessages)]
            public async Task StreamMessageAll([Leftover] string message)
            {
                var count = _service.SetStreamMessageForAll(ctx.Guild.Id, message);

                if (count == 0)
                {
                    await ReplyConfirmLocalizedAsync(strs.stream_not_following_any);
                    return;
                }

                await ReplyConfirmLocalizedAsync(strs.stream_message_set_all(count));
            }
            
            [NadekoCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task StreamCheck(string url)
            {
                try
                {
                    var data = await _service.GetStreamDataAsync(url).ConfigureAwait(false);
                    if (data is null)
                    {
                        await ReplyErrorLocalizedAsync(strs.no_channel_found).ConfigureAwait(false);
                        return;
                    }
                    
                    if (data.IsLive)
                    {
                        await ReplyConfirmLocalizedAsync(strs.streamer_online(
                            Format.Bold(data.Name),
                            Format.Bold(data.Viewers.ToString())));
                    }
                    else
                    {
                        await ReplyConfirmLocalizedAsync(strs.streamer_offline(data.Name));
                    }
                }
                catch
                {
                    await ReplyErrorLocalizedAsync(strs.no_channel_found).ConfigureAwait(false);
                }
            }
        }
    }
}