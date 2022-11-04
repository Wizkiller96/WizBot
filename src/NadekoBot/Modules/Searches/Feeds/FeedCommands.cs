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
            new(@"youtube\.com\/(?:c\/|channel\/|user\/)?(?<channelid>[a-zA-Z0-9\-_]{1,})");

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public Task YtUploadNotif(string url, [Leftover] ITextChannel channel = null)
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
        public async Task Feed(string url, [Leftover] ITextChannel channel = null)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) 
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                await ReplyErrorLocalizedAsync(strs.feed_invalid_url);
                return;
            }

            channel ??= (ITextChannel)ctx.Channel;
            try
            {
                await FeedReader.ReadAsync(url);
            }
            catch (Exception ex)
            {
                Log.Information(ex, "Unable to get feeds from that url");
                await ReplyErrorLocalizedAsync(strs.feed_cant_parse);
                return;
            }

            var result = _service.AddFeed(ctx.Guild.Id, channel.Id, url);
            if (result == FeedAddResult.Success)
            {
                await ReplyConfirmLocalizedAsync(strs.feed_added);
                return;
            }

            if (result == FeedAddResult.Duplicate)
            {
                await ReplyErrorLocalizedAsync(strs.feed_duplicate);
                return;
            }

            if (result == FeedAddResult.LimitReached)
            {
                await ReplyErrorLocalizedAsync(strs.feed_limit_reached);
                return;
            }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task FeedRemove(int index)
        {
            if (_service.RemoveFeed(ctx.Guild.Id, --index))
                await ReplyConfirmLocalizedAsync(strs.feed_removed);
            else
                await ReplyErrorLocalizedAsync(strs.feed_out_of_range);
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task FeedList()
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