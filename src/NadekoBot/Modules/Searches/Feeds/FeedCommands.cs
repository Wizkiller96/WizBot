#nullable disable
using CodeHollow.FeedReader;
using NadekoBot.Modules.Searches.Services;
using System.Text.RegularExpressions;

namespace NadekoBot.Modules.Searches;

public partial class Searches
{
    [Group]
    public partial class FeedCommands : NadekoModule<FeedsService>
    {
        private static readonly Regex _ytChannelRegex =
            new(@"youtube\.com\/(?:c\/|channel\/|user\/)?(?<channelid>[a-zA-Z0-9\-]{1,})");

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public partial Task YtUploadNotif(string url, [Leftover] ITextChannel channel = null)
        {
            var m = _ytChannelRegex.Match(url);
            if (!m.Success)
                return ReplyErrorLocalizedAsync(strs.invalid_input);

            var channelId = m.Groups["channelid"].Value;

            return Feed("https://www.youtube.com/feeds/videos.xml?channel_id=" + channelId, channel);
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async partial Task Feed(string url, [Leftover] ITextChannel channel = null)
        {
            var success = Uri.TryCreate(url, UriKind.Absolute, out var uri)
                          && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
            if (success)
            {
                channel ??= (ITextChannel)ctx.Channel;
                try
                {
                    await FeedReader.ReadAsync(url);
                }
                catch (Exception ex)
                {
                    Log.Information(ex, "Unable to get feeds from that url");
                    success = false;
                }
            }

            if (success)
            {
                success = _service.AddFeed(ctx.Guild.Id, channel.Id, url);
                if (success)
                {
                    await ReplyConfirmLocalizedAsync(strs.feed_added);
                    return;
                }
            }

            await ReplyConfirmLocalizedAsync(strs.feed_not_valid);
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async partial Task FeedRemove(int index)
        {
            if (_service.RemoveFeed(ctx.Guild.Id, --index))
                await ReplyConfirmLocalizedAsync(strs.feed_removed);
            else
                await ReplyErrorLocalizedAsync(strs.feed_out_of_range);
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async partial Task FeedList()
        {
            var feeds = _service.GetFeeds(ctx.Guild.Id);

            if (!feeds.Any())
            {
                await ctx.Channel.EmbedAsync(_eb.Create().WithOkColor().WithDescription(GetText(strs.feed_no_feed)));
                return;
            }

            await ctx.SendPaginatedConfirmAsync(0,
                cur =>
                {
                    var embed = _eb.Create().WithOkColor();
                    var i = 0;
                    var fs = string.Join("\n",
                        feeds.Skip(cur * 10).Take(10).Select(x => $"`{(cur * 10) + ++i}.` <#{x.ChannelId}> {x.Url}"));

                    return embed.WithDescription(fs);
                },
                feeds.Count,
                10);
        }
    }
}