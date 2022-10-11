#nullable disable
using CodeHollow.FeedReader;
using CodeHollow.FeedReader.Feeds;
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Nadeko.Common;
using NadekoBot.Db;
using NadekoBot.Services.Database.Models;

namespace NadekoBot.Modules.Searches.Services;

public class FeedsService : INService
{
    private readonly DbService _db;
    private readonly ConcurrentDictionary<string, List<FeedSub>> _subs;
    private readonly DiscordSocketClient _client;
    private readonly IEmbedBuilderService _eb;

    private readonly ConcurrentDictionary<string, DateTime> _lastPosts = new();
    private readonly Dictionary<string, uint> _errorCounters = new();

    public FeedsService(
        Bot bot,
        DbService db,
        DiscordSocketClient client,
        IEmbedBuilderService eb)
    {
        _db = db;

        using (var uow = db.GetDbContext())
        {
            var guildConfigIds = bot.AllGuildConfigs.Select(x => x.Id).ToList();
            _subs = uow.GuildConfigs.AsQueryable()
                       .Where(x => guildConfigIds.Contains(x.Id))
                       .Include(x => x.FeedSubs)
                       .ToList()
                       .SelectMany(x => x.FeedSubs)
                       .GroupBy(x => x.Url.ToLower())
                       .ToDictionary(x => x.Key, x => x.ToList())
                       .ToConcurrent();
        }

        _client = client;
        _eb = eb;

        _ = Task.Run(TrackFeeds);
    }

    private void ClearErrors(string url)
        => _errorCounters.Remove(url);

    private async Task<uint> AddError(string url, List<int> ids)
    {
        try
        {
            var newValue = _errorCounters[url] = _errorCounters.GetValueOrDefault(url) + 1;

            if (newValue >= 100)
            {
                // remove from db
                await using var ctx = _db.GetDbContext();
                await ctx.GetTable<FeedSub>()
                         .DeleteAsync(x => ids.Contains(x.Id));
                
                // remove from the local cache
                _subs.TryRemove(url, out _);

                // reset the error counter
                ClearErrors(url);
            }

            return newValue;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error adding rss errors...");
            return 0;
        }
    }

    public async Task<EmbedBuilder> TrackFeeds()
    {
        while (true)
        {
            var allSendTasks = new List<Task>(_subs.Count);
            foreach (var kvp in _subs)
            {
                if (kvp.Value.Count == 0)
                    continue;

                var rssUrl = kvp.Value.First().Url;
                try
                {
                    var feed = await FeedReader.ReadAsync(rssUrl);

                    var items = feed
                                .Items.Select(item => (Item: item,
                                    LastUpdate: item.PublishingDate?.ToUniversalTime()
                                                ?? (item.SpecificItem as AtomFeedItem)?.UpdatedDate?.ToUniversalTime()))
                                .Where(data => data.LastUpdate is not null)
                                .Select(data => (data.Item, LastUpdate: (DateTime)data.LastUpdate))
                                .OrderByDescending(data => data.LastUpdate)
                                .Reverse() // start from the oldest
                                .ToList();

                    if (!_lastPosts.TryGetValue(kvp.Key, out var lastFeedUpdate))
                    {
                        lastFeedUpdate = _lastPosts[kvp.Key] =
                            items.Any() ? items[items.Count - 1].LastUpdate : DateTime.UtcNow;
                    }

                    foreach (var (feedItem, itemUpdateDate) in items)
                    {
                        if (itemUpdateDate <= lastFeedUpdate)
                            continue;

                        var embed = _eb.Create().WithFooter(rssUrl);

                        _lastPosts[kvp.Key] = itemUpdateDate;

                        var link = feedItem.SpecificItem.Link;
                        if (!string.IsNullOrWhiteSpace(link) && Uri.IsWellFormedUriString(link, UriKind.Absolute))
                            embed.WithUrl(link);

                        var title = string.IsNullOrWhiteSpace(feedItem.Title) ? "-" : feedItem.Title;

                        var gotImage = false;
                        if (feedItem.SpecificItem is MediaRssFeedItem mrfi
                            && (mrfi.Enclosure?.MediaType?.StartsWith("image/") ?? false))
                        {
                            var imgUrl = mrfi.Enclosure.Url;
                            if (!string.IsNullOrWhiteSpace(imgUrl)
                                && Uri.IsWellFormedUriString(imgUrl, UriKind.Absolute))
                            {
                                embed.WithImageUrl(imgUrl);
                                gotImage = true;
                            }
                        }

                        if (!gotImage && feedItem.SpecificItem is AtomFeedItem afi)
                        {
                            var previewElement = afi.Element.Elements()
                                                    .FirstOrDefault(x => x.Name.LocalName == "preview");

                            if (previewElement is null)
                            {
                                previewElement = afi.Element.Elements()
                                                    .FirstOrDefault(x => x.Name.LocalName == "thumbnail");
                            }

                            if (previewElement is not null)
                            {
                                var urlAttribute = previewElement.Attribute("url");
                                if (urlAttribute is not null
                                    && !string.IsNullOrWhiteSpace(urlAttribute.Value)
                                    && Uri.IsWellFormedUriString(urlAttribute.Value, UriKind.Absolute))
                                {
                                    embed.WithImageUrl(urlAttribute.Value);
                                    gotImage = true;
                                }
                            }
                        }


                        embed.WithTitle(title.TrimTo(256));

                        var desc = feedItem.Description?.StripHtml();
                        if (!string.IsNullOrWhiteSpace(feedItem.Description))
                            embed.WithDescription(desc.TrimTo(2048));

                        //send the created embed to all subscribed channels
                        var feedSendTasks = kvp.Value
                                               .Where(x => x.GuildConfig is not null)
                                               .Select(x => _client.GetGuild(x.GuildConfig.GuildId)
                                                                   ?.GetTextChannel(x.ChannelId))
                                               .Where(x => x is not null)
                                               .Select(x => x.EmbedAsync(embed));

                        allSendTasks.Add(feedSendTasks.WhenAll());

                        // as data retrieval was sucessful, reset error counter
                        ClearErrors(rssUrl);
                    }
                }
                catch (Exception ex)
                {
                    var errorCount = await AddError(rssUrl, kvp.Value.Select(x => x.Id).ToList());
                    
                    Log.Warning("An error occured while getting rss stream ({ErrorCount} / 100) {RssFeed}"
                                + "\n {Message}",
                        errorCount,
                        rssUrl,
                        $"[{ex.GetType().Name}]: {ex.Message}");
                }
            }

            await Task.WhenAll(Task.WhenAll(allSendTasks), Task.Delay(30000));
        }
    }

    public List<FeedSub> GetFeeds(ulong guildId)
    {
        using var uow = _db.GetDbContext();
        return uow.GuildConfigsForId(guildId, set => set.Include(x => x.FeedSubs))
                  .FeedSubs.OrderBy(x => x.Id)
                  .ToList();
    }

    public FeedAddResult AddFeed(ulong guildId, ulong channelId, string rssFeed)
    {
        ArgumentNullException.ThrowIfNull(rssFeed, nameof(rssFeed));

        var fs = new FeedSub
        {
            ChannelId = channelId,
            Url = rssFeed.Trim()
        };

        using var uow = _db.GetDbContext();
        var gc = uow.GuildConfigsForId(guildId, set => set.Include(x => x.FeedSubs));

        if (gc.FeedSubs.Any(x => x.Url.ToLower() == fs.Url.ToLower()))
            return FeedAddResult.Duplicate;
        if (gc.FeedSubs.Count >= 10)
            return FeedAddResult.LimitReached;

        gc.FeedSubs.Add(fs);
        uow.SaveChanges();
        //adding all, in case bot wasn't on this guild when it started
        foreach (var feed in gc.FeedSubs)
        {
            _subs.AddOrUpdate(feed.Url.ToLower(),
                new List<FeedSub>
                {
                    feed
                },
                (_, old) =>
                {
                    old.Add(feed);
                    return old;
                });
        }

        return FeedAddResult.Success;
    }

    public bool RemoveFeed(ulong guildId, int index)
    {
        if (index < 0)
            return false;

        using var uow = _db.GetDbContext();
        var items = uow.GuildConfigsForId(guildId, set => set.Include(x => x.FeedSubs))
                       .FeedSubs.OrderBy(x => x.Id)
                       .ToList();

        if (items.Count <= index)
            return false;
        var toRemove = items[index];
        _subs.AddOrUpdate(toRemove.Url.ToLower(),
            new List<FeedSub>(),
            (_, old) =>
            {
                old.Remove(toRemove);
                return old;
            });
        uow.Remove(toRemove);
        uow.SaveChanges();

        return true;
    }
}

public enum FeedAddResult
{
    Success,
    LimitReached,
    Invalid,
    Duplicate,
}