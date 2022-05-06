﻿#nullable disable
using CodeHollow.FeedReader;
using CodeHollow.FeedReader.Feeds;
using Microsoft.EntityFrameworkCore;
using WizBot.Db;
using WizBot.Services.Database.Models;

namespace WizBot.Modules.Searches.Services;

public class FeedsService : INService
{
    private readonly DbService _db;
    private readonly ConcurrentDictionary<string, HashSet<FeedSub>> _subs;
    private readonly DiscordSocketClient _client;
    private readonly IEmbedBuilderService _eb;

    private readonly ConcurrentDictionary<string, DateTime> _lastPosts = new();

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
                       .ToDictionary(x => x.Key, x => x.ToHashSet())
                       .ToConcurrent();
        }

        _client = client;
        _eb = eb;

        _ = Task.Run(TrackFeeds);
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
                        var feedSendTasks = kvp.Value.Where(x => x.GuildConfig is not null)
                                               .Select(x => _client.GetGuild(x.GuildConfig.GuildId)
                                                                   ?.GetTextChannel(x.ChannelId))
                                               .Where(x => x is not null)
                                               .Select(x => x.EmbedAsync(embed));

                        allSendTasks.Add(feedSendTasks.WhenAll());
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning("An error occured while getting rss stream: {Message}", ex.Message);
                }
            }

            await Task.WhenAll(Task.WhenAll(allSendTasks), Task.Delay(10000));
        }
    }

    public List<FeedSub> GetFeeds(ulong guildId)
    {
        using var uow = _db.GetDbContext();
        return uow.GuildConfigsForId(guildId, set => set.Include(x => x.FeedSubs))
                  .FeedSubs.OrderBy(x => x.Id)
                  .ToList();
    }

    public bool AddFeed(ulong guildId, ulong channelId, string rssFeed)
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
            return false;
        if (gc.FeedSubs.Count >= 10)
            return false;

        gc.FeedSubs.Add(fs);
        uow.SaveChanges();
        //adding all, in case bot wasn't on this guild when it started
        foreach (var feed in gc.FeedSubs)
        {
            _subs.AddOrUpdate(feed.Url.ToLower(),
                new HashSet<FeedSub>
                {
                    feed
                },
                (_, old) =>
                {
                    old.Add(feed);
                    return old;
                });
        }

        return true;
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
            new HashSet<FeedSub>(),
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