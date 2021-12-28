#nullable disable
using Microsoft.EntityFrameworkCore;
using NadekoBot.Modules.Searches.Services;
using NadekoBot.Db;
using NadekoBot.Db.Models;

namespace NadekoBot.Modules.Searches;

public partial class Searches
{
    [Group]
    public class StreamNotificationCommands : NadekoSubmodule<StreamNotificationService>
    {
        private readonly DbService _db;

        public StreamNotificationCommands(DbService db)
            => _db = db;

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
                await ReplyErrorLocalizedAsync(strs.stream_not_added);
                return;
            }

            var embed = _service.GetEmbed(ctx.Guild.Id, data);
            await ctx.Channel.EmbedAsync(embed, GetText(strs.stream_tracked));
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
                await ReplyErrorLocalizedAsync(strs.stream_no);
                return;
            }

            await ReplyConfirmLocalizedAsync(
                strs.stream_removed(
                    Format.Bold(fs.Username),
                    fs.Type));
        }

        [NadekoCommand, Aliases]
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

            var streams = new List<FollowedStream>();
            await using (var uow = _db.GetDbContext())
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

            await ctx.SendPaginatedConfirmAsync(page, cur =>
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
                        $"**#{index + 1 + (12 * cur)}** {elem.Username.ToLower()}",
                        $"【{elem.Type}】\n<#{elem.ChannelId}>\n{elem.Message?.TrimTo(50)}",
                        true);
                }

                return eb;
            }, streams.Count, 12);
        }

        [NadekoCommand, Aliases]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task StreamOffline()
        {
            var newValue = _service.ToggleStreamOffline(ctx.Guild.Id);
            if (newValue)
            {
                await ReplyConfirmLocalizedAsync(strs.stream_off_enabled);
            }
            else
            {
                await ReplyConfirmLocalizedAsync(strs.stream_off_disabled);
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
                await ReplyConfirmLocalizedAsync(strs.stream_not_following);
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
                var data = await _service.GetStreamDataAsync(url);
                if (data is null)
                {
                    await ReplyErrorLocalizedAsync(strs.no_channel_found);
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
                await ReplyErrorLocalizedAsync(strs.no_channel_found);
            }
        }
    }
}
