using CodeHollow.FeedReader.Feeds;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using WizBot.Core.Services;
using WizBot.Core.Services.Database.Models;
using WizBot.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WizBot.Modules.Searches.Services
{
    public class FeedsService : INService
    {
        private readonly DbService _db;
        private readonly ConcurrentDictionary<string, HashSet<FeedSub>> _subs;
        private readonly DiscordSocketClient _client;
        private readonly ConcurrentDictionary<string, DateTime> _lastPosts =
            new ConcurrentDictionary<string, DateTime>();

        public FeedsService(WizBot bot, DbService db, DiscordSocketClient client)
        {
            _db = db;

            _subs = bot
                .AllGuildConfigs
                .SelectMany(x => x.FeedSubs)
                .GroupBy(x => x.Url)
                .ToDictionary(x => x.Key, x => x.ToHashSet())
                .ToConcurrent();

            _client = client;

            foreach (var kvp in _subs)
            {
                // to make sure rss feeds don't post right away, but
                // only the updates from after the bot has started
                _lastPosts.AddOrUpdate(kvp.Key, DateTime.UtcNow, (k, old) => DateTime.UtcNow);
            }

            var _ = Task.Run(TrackFeeds);

        }

        public async Task<EmbedBuilder> TrackFeeds()
        {
            while (true)
            {
                foreach (var kvp in _subs)
                {
                    if (kvp.Value.Count == 0)
                        continue;

                    if (!_lastPosts.TryGetValue(kvp.Key, out DateTime lastTime))
                        lastTime = _lastPosts.AddOrUpdate(kvp.Key, DateTime.UtcNow, (k, old) => DateTime.UtcNow);

                    var rssUrl = kvp.Key;
                    try
                    {
                        var feed = await CodeHollow.FeedReader.FeedReader.ReadAsync(rssUrl).ConfigureAwait(false);
                        //Log.Information(rssUrl);
                        //Log.Information("Last updated: {Last} {Str}", feed.LastUpdatedDate, feed.LastUpdatedDateString);

                        var embed = new EmbedBuilder()
                            .WithFooter(rssUrl);

                        foreach (var item in feed.Items.Take(1))
                        {

                            //Log.Information("{Title} - {Link}", i.Title, i.Link);
                            var maybePub = item.PublishingDate
                                ?? (item.SpecificItem as AtomFeedItem)?.UpdatedDate;
                            //Log.Warning(maybePub?.ToString() ?? "-");
                            if (!(maybePub is DateTime pub) || pub <= lastTime)
                            {
                                continue;
                            }

                            var desc = item.Description?.StripHTML();
                            if (!string.IsNullOrWhiteSpace(item.Description))
                                embed.WithDescription(desc);

                            var link = item.SpecificItem.Link;
                            if (!string.IsNullOrWhiteSpace(link) && Uri.IsWellFormedUriString(link, UriKind.Absolute))
                                embed.WithUrl(link);

                            var title = string.IsNullOrWhiteSpace(item.Title)
                                ? "-"
                                : item.Title;

                            if (item.SpecificItem is MediaRssFeedItem mrfi && (mrfi.Enclosure?.MediaType.StartsWith("image/") ?? false))
                            {
                                var imgUrl = mrfi.Enclosure.Url;
                                if (!string.IsNullOrWhiteSpace(imgUrl) && Uri.IsWellFormedUriString(imgUrl, UriKind.Absolute))
                                {
                                    embed.WithImageUrl(imgUrl);
                                }
                            }

                            //// old image retreiving code
                            //var img = (item as Rss20Feed).Items.FirstOrDefault(x => x.Element.Name == "enclosure") ...FirstOrDefault(x => x.RelationshipType == "enclosure")?.Uri.AbsoluteUri
                            //    ?? Regex.Match(item.Description, @"src=""(?<src>.*?)""").Groups["src"].ToString();

                            //if (!string.IsNullOrWhiteSpace(img) && Uri.IsWellFormedUriString(img, UriKind.Absolute))
                            //    embed.WithImageUrl(img);

                            embed.WithTitle(title)
                                .WithDescription(desc);

                            _lastPosts.AddOrUpdate(rssUrl, pub, (k, old) => pub);
                            //send the created embed to all subscribed channels
                            var sendTasks = kvp.Value
                                .Where(x => x.GuildConfig != null)
                                .Select(x => _client.GetGuild(x.GuildConfig.GuildId)
                                    ?.GetTextChannel(x.ChannelId))
                                .Where(x => x != null)
                                .Select(x => x.EmbedAsync(embed));

                            await Task.WhenAll(sendTasks).ConfigureAwait(false);
                        }
                    }
                    catch { }
                }

                await Task.Delay(10000).ConfigureAwait(false);
            }
        }

        public List<FeedSub> GetFeeds(ulong guildId)
        {
            using (var uow = _db.GetDbContext())
            {
                return uow.GuildConfigs.ForId(guildId, set => set.Include(x => x.FeedSubs))
                    .FeedSubs
                    .OrderBy(x => x.Id)
                    .ToList();
            }
        }

        public bool AddFeed(ulong guildId, ulong channelId, string rssFeed)
        {
            rssFeed.ThrowIfNull(nameof(rssFeed));

            var fs = new FeedSub()
            {
                ChannelId = channelId,
                Url = rssFeed.Trim().ToLowerInvariant(),
            };

            using (var uow = _db.GetDbContext())
            {
                var gc = uow.GuildConfigs.ForId(guildId, set => set.Include(x => x.FeedSubs));

                if (gc.FeedSubs.Contains(fs))
                {
                    return false;
                }
                else if (gc.FeedSubs.Count >= 5)
                {
                    return false;
                }

                gc.FeedSubs.Add(fs);

                //adding all, in case bot wasn't on this guild when it started
                foreach (var f in gc.FeedSubs)
                {
                    _subs.AddOrUpdate(f.Url, new HashSet<FeedSub>(), (k, old) =>
                    {
                        old.Add(f);
                        return old;
                    });
                }

                uow.SaveChanges();
            }

            return true;
        }

        public bool RemoveFeed(ulong guildId, int index)
        {
            if (index < 0)
                return false;

            using (var uow = _db.GetDbContext())
            {
                var items = uow.GuildConfigs.ForId(guildId, set => set.Include(x => x.FeedSubs))
                    .FeedSubs
                    .OrderBy(x => x.Id)
                    .ToList();

                if (items.Count <= index)
                    return false;
                var toRemove = items[index];
                _subs.AddOrUpdate(toRemove.Url, new HashSet<FeedSub>(), (key, old) =>
                {
                    old.Remove(toRemove);
                    return old;
                });
                uow._context.Remove(toRemove);
                uow.SaveChanges();
            }
            return true;
        }
    }
}
