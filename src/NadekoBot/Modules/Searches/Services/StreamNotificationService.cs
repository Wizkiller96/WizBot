using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Common;
using NadekoBot.Modules.Searches.Common;
using NadekoBot.Modules.Searches.Common.StreamNotifications;
using NadekoBot.Services;
using NadekoBot.Services.Database.Models;
using NadekoBot.Extensions;
using StackExchange.Redis;
using Discord;
using Discord.WebSocket;
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using NadekoBot.Common.Collections;
using NadekoBot.Common.Replacements;
using NadekoBot.Db;
using NadekoBot.Db.Models;
using Serilog;

namespace NadekoBot.Modules.Searches.Services
{
    public sealed class StreamNotificationService : INService
    {
        private readonly DbService _db;
        private readonly IBotStrings _strings;
        private readonly Random _rng = new NadekoRandom();
        private readonly DiscordSocketClient _client;
        private readonly NotifChecker _streamTracker;

        private readonly object _shardLock = new object();

        private readonly Dictionary<StreamDataKey, HashSet<ulong>> _trackCounter =
            new Dictionary<StreamDataKey, HashSet<ulong>>();

        private readonly Dictionary<StreamDataKey, Dictionary<ulong, HashSet<FollowedStream>>> _shardTrackedStreams;
        private readonly ConcurrentHashSet<ulong> _offlineNotificationServers;
        
        private readonly IBotCredentials _creds;
        private readonly IPubSub _pubSub;
        private readonly IEmbedBuilderService _eb;
        private readonly Timer _notifCleanupTimer;

        private readonly TypedKey<List<StreamData>> _streamsOnlineKey;
        private readonly TypedKey<List<StreamData>> _streamsOfflineKey;
        
        private readonly TypedKey<FollowStreamPubData> _streamFollowKey;
        private readonly TypedKey<FollowStreamPubData> _streamUnfollowKey;

        public StreamNotificationService(
            DbService db,
            DiscordSocketClient client,
            IBotStrings strings,
            ConnectionMultiplexer redis,
            IBotCredentials creds,
            IHttpClientFactory httpFactory,
            Bot bot,
            IPubSub pubSub,
            IEmbedBuilderService eb)
        {
            _db = db;
            _client = client;
            _strings = strings;
            _creds = creds;
            _pubSub = pubSub;
            _eb = eb;
            _streamTracker = new NotifChecker(httpFactory, redis, creds.RedisKey(), client.ShardId == 0);

            _streamsOnlineKey = new("streams.online");
            _streamsOfflineKey = new("streams.offline");

            _streamFollowKey = new("stream.follow");
            _streamUnfollowKey = new("stream.unfollow");

            using (var uow = db.GetDbContext())
            {
                var ids = client.GetGuildIds();
                var guildConfigs = uow.Set<GuildConfig>()
                    .AsQueryable()
                    .Include(x => x.FollowedStreams)
                    .Where(x => ids.Contains(x.GuildId))
                    .ToList();

                _offlineNotificationServers = new ConcurrentHashSet<ulong>(guildConfigs
                    .Where(gc => gc.NotifyStreamOffline)
                    .Select(x => x.GuildId)
                    .ToList());

                var followedStreams = guildConfigs
                    .SelectMany(x => x.FollowedStreams)
                    .ToList();

                _shardTrackedStreams = followedStreams
                    .GroupBy(x => new {Type = x.Type, Name = x.Username.ToLower()})
                    .ToList()
                    .ToDictionary(
                        x => new StreamDataKey(x.Key.Type, x.Key.Name.ToLower()),
                        x => x.GroupBy(y => y.GuildId)
                            .ToDictionary(y => y.Key, y => y.AsEnumerable().ToHashSet()));

                // shard 0 will keep track of when there are no more guilds which track a stream
                if (client.ShardId == 0)
                {
                    var allFollowedStreams = uow.Set<FollowedStream>()
                        .AsQueryable()
                        .ToList();

                    foreach (var fs in allFollowedStreams)
                    {
                        _streamTracker.CacheAddData(fs.CreateKey(), null, replace: false);
                    }

                    _trackCounter = allFollowedStreams
                        .GroupBy(x => new {Type = x.Type, Name = x.Username.ToLower()})
                        .ToDictionary(
                            x => new StreamDataKey(x.Key.Type, x.Key.Name),
                            x => x.Select(fs => fs.GuildId).ToHashSet());
                }
            }

            _pubSub.Sub(_streamsOfflineKey, HandleStreamsOffline);
            _pubSub.Sub(_streamsOnlineKey, HandleStreamsOnline);

            if (client.ShardId == 0)
            {
                // only shard 0 will run the tracker,
                // and then publish updates with redis to other shards 
                _streamTracker.OnStreamsOffline += OnStreamsOffline;
                _streamTracker.OnStreamsOnline += OnStreamsOnline;
                _ = _streamTracker.RunAsync();
                _notifCleanupTimer = new Timer(_ =>
                {
                    try
                    {
                        var errorLimit = TimeSpan.FromHours(12);
                        var failingStreams = _streamTracker.GetFailingStreams(errorLimit, true)
                            .ToList();

                        if (!failingStreams.Any())
                            return;

                        var deleteGroups = failingStreams.GroupBy(x => x.Type)
                            .ToDictionary(x => x.Key, x => x.Select(x => x.Name).ToList());

                        using (var uow = _db.GetDbContext())
                        {
                            foreach (var kvp in deleteGroups)
                            {
                                Log.Information($"Deleting {kvp.Value.Count} {kvp.Key} streams because " +
                                          $"they've been erroring for more than {errorLimit}: {string.Join(", ", kvp.Value)}");

                                var toDelete = uow.Set<FollowedStream>()
                                    .AsQueryable()
                                    .Where(x => x.Type == kvp.Key && kvp.Value.Contains(x.Username))
                                    .ToList();

                                uow.RemoveRange(toDelete);
                                uow.SaveChanges();
                                
                                foreach(var loginToDelete in kvp.Value)
                                    _streamTracker.UntrackStreamByKey(new StreamDataKey(kvp.Key, loginToDelete));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Error cleaning up FollowedStreams");
                        Log.Error(ex.ToString());
                    }
                }, null, TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(30));

                _pubSub.Sub(_streamFollowKey, HandleFollowStream);
                _pubSub.Sub(_streamUnfollowKey, HandleUnfollowStream);
            }

            bot.JoinedGuild += ClientOnJoinedGuild;
            client.LeftGuild += ClientOnLeftGuild;
        }

        /// <summary>
        /// Handles follow stream pubs to keep the counter up to date.
        /// When counter reaches 0, stream is removed from tracking because
        /// that means no guilds are subscribed to that stream anymore 
        /// </summary>
        private ValueTask HandleFollowStream(FollowStreamPubData info)
        {
            _streamTracker.CacheAddData(info.Key, null, replace: false);
            lock (_shardLock)
            {
                var key = info.Key;
                if (_trackCounter.ContainsKey(key))
                {
                    _trackCounter[key].Add(info.GuildId);
                }
                else
                {
                    _trackCounter[key] = new HashSet<ulong>()
                    {
                        info.GuildId
                    };
                }
            }

            return default;
        }

        /// <summary>
        /// Handles unfollow pubs to keep the counter up to date.
        /// When counter reaches 0, stream is removed from tracking because
        /// that means no guilds are subscribed to that stream anymore 
        /// </summary>
        private ValueTask HandleUnfollowStream(FollowStreamPubData info)
        {
            lock (_shardLock)
            {
                var key = info.Key;
                if (!_trackCounter.TryGetValue(key, out var set))
                {
                    // it should've been removed already?
                    _streamTracker.UntrackStreamByKey(in key);
                    return default;
                }

                set.Remove(info.GuildId);
                if (set.Count != 0)
                    return default;

                _trackCounter.Remove(key);
                // if no other guilds are following this stream
                // untrack the stream
                _streamTracker.UntrackStreamByKey(in key);
            }

            return default;
        }

        private async ValueTask HandleStreamsOffline(List<StreamData> offlineStreams)
        {
            foreach (var stream in offlineStreams)
            {
                var key = stream.CreateKey();
                if (_shardTrackedStreams.TryGetValue(key, out var fss))
                {
                    var sendTasks = fss
                        // send offline stream notifications only to guilds which enable it with .stoff
                        .SelectMany(x => x.Value)
                        .Where(x => _offlineNotificationServers.Contains(x.GuildId))
                        .Select(fs => _client.GetGuild(fs.GuildId)
                            ?.GetTextChannel(fs.ChannelId)
                            ?.EmbedAsync(GetEmbed(fs.GuildId, stream)));

                    await Task.WhenAll(sendTasks);
                }
            }
        }

        private async ValueTask HandleStreamsOnline(List<StreamData> onlineStreams)
        {
            foreach (var stream in onlineStreams)
            {
                var key = stream.CreateKey();
                if (_shardTrackedStreams.TryGetValue(key, out var fss))
                {
                    var sendTasks = fss
                        .SelectMany(x => x.Value)
                        .Select(fs =>
                        {
                            var textChannel = _client.GetGuild(fs.GuildId)?.GetTextChannel(fs.ChannelId);
                            
                            if (textChannel is null)
                                return Task.CompletedTask;
                            
                            var rep = new ReplacementBuilder()
                                .WithOverride("%user%", () => fs.Username)
                                .WithOverride("%platform%", () => fs.Type.ToString())
                                .Build();
                            
                            var message = string.IsNullOrWhiteSpace(fs.Message)
                                ? ""
                                : rep.Replace(fs.Message);

                            return textChannel.EmbedAsync(GetEmbed(fs.GuildId, stream), message);
                        });

                    await Task.WhenAll(sendTasks);
                }
            }
        }

        private Task OnStreamsOnline(List<StreamData> data)
            => _pubSub.Pub(_streamsOnlineKey, data);
        
        private Task OnStreamsOffline(List<StreamData> data)
            => _pubSub.Pub(_streamsOfflineKey, data);

        private Task ClientOnJoinedGuild(GuildConfig guildConfig)
        {
            using (var uow = _db.GetDbContext())
            {
                var gc = uow.GuildConfigs
                    .AsQueryable()
                    .Include(x => x.FollowedStreams)
                    .FirstOrDefault(x => x.GuildId == guildConfig.GuildId);

                if (gc is null)
                    return Task.CompletedTask;
                
                if (gc.NotifyStreamOffline)
                    _offlineNotificationServers.Add(gc.GuildId);

                foreach (var followedStream in gc.FollowedStreams)
                {
                    var key = followedStream.CreateKey();
                    var streams = GetLocalGuildStreams(key, gc.GuildId);
                    streams.Add(followedStream);
                    PublishFollowStream(followedStream);
                }
            }

            return Task.CompletedTask;
        }

        private Task ClientOnLeftGuild(SocketGuild guild)
        {
            using (var uow = _db.GetDbContext())
            {
                var gc = uow.GuildConfigsForId(guild.Id, set => set.Include(x => x.FollowedStreams));

                _offlineNotificationServers.TryRemove(gc.GuildId);

                foreach (var followedStream in gc.FollowedStreams)
                {
                    var streams = GetLocalGuildStreams(followedStream.CreateKey(), guild.Id);
                    streams.Remove(followedStream);

                    PublishUnfollowStream(followedStream);
                }
            }

            return Task.CompletedTask;
        }

        public async Task<int> ClearAllStreams(ulong guildId)
        {
            using var uow = _db.GetDbContext();
            var gc = uow.GuildConfigsForId(guildId, set => set.Include(x => x.FollowedStreams));
            uow.RemoveRange(gc.FollowedStreams);

            foreach (var s in gc.FollowedStreams)
                await PublishUnfollowStream(s);
            
            uow.SaveChanges();

            return gc.FollowedStreams.Count;
        }

        public async Task<FollowedStream> UnfollowStreamAsync(ulong guildId, int index)
        {
            FollowedStream fs;
            using (var uow = _db.GetDbContext())
            {
                var fss = uow.Set<FollowedStream>()
                    .AsQueryable()
                    .Where(x => x.GuildId == guildId)
                    .OrderBy(x => x.Id)
                    .ToList();

                // out of range
                if (fss.Count <= index)
                    return null;

                fs = fss[index];
                uow.Remove(fs);

                await uow.SaveChangesAsync();

                // remove from local cache
                lock (_shardLock)
                {
                    var key = fs.CreateKey();
                    var streams = GetLocalGuildStreams(key, guildId);
                    streams.Remove(fs);
                }
            }

            await PublishUnfollowStream(fs);

            return fs;
        }

        private void PublishFollowStream(FollowedStream fs)
            => _pubSub.Pub(_streamFollowKey, new() { Key = fs.CreateKey(), GuildId = fs.GuildId });

        private Task PublishUnfollowStream(FollowedStream fs)
            => _pubSub.Pub(_streamUnfollowKey, new() { Key = fs.CreateKey(), GuildId = fs.GuildId });

        public async Task<StreamData> FollowStream(ulong guildId, ulong channelId, string url)
        {
            // this will 
            var data = await _streamTracker.GetStreamDataByUrlAsync(url);

            if (data is null)
                return null;

            FollowedStream fs;
            using (var uow = _db.GetDbContext())
            {
                var gc = uow.GuildConfigsForId(guildId, set => set.Include(x => x.FollowedStreams));

                // add it to the database
                fs = new FollowedStream()
                {
                    Type = data.StreamType,
                    Username = data.UniqueName,
                    ChannelId = channelId,
                    GuildId = guildId,
                };

                if (gc.FollowedStreams.Count >= 10)
                    return null;
                
                gc.FollowedStreams.Add(fs);
                await uow.SaveChangesAsync();

                // add it to the local cache of tracked streams
                // this way this shard will know it needs to post a message to discord
                // when shard 0 publishes stream status changes for this stream 
                lock (_shardLock)
                {
                    var key = data.CreateKey();
                    var streams = GetLocalGuildStreams(key, guildId);
                    streams.Add(fs);
                }
            }

            PublishFollowStream(fs);

            return data;
        }

        public IEmbedBuilder GetEmbed(ulong guildId, StreamData status)
        {
            var embed = _eb.Create()
                .WithTitle(status.Name)
                .WithUrl(status.StreamUrl)
                .WithDescription(status.StreamUrl)
                .AddField(GetText(guildId, strs.status), status.IsLive ? "🟢 Online" : "🔴 Offline", true)
                .AddField(GetText(guildId, strs.viewers), status.IsLive ? status.Viewers.ToString() : "-", true);

            if (status.IsLive)
                embed = embed.WithOkColor();
            else
                embed = embed.WithErrorColor();
            
            if (!string.IsNullOrWhiteSpace(status.Title))
                embed.WithAuthor(status.Title);

            if (!string.IsNullOrWhiteSpace(status.Game))
                embed.AddField(GetText(guildId, strs.streaming), status.Game, true);

            if (!string.IsNullOrWhiteSpace(status.AvatarUrl))
                embed.WithThumbnailUrl(status.AvatarUrl);

            if (!string.IsNullOrWhiteSpace(status.Preview))
                embed.WithImageUrl(status.Preview + "?dv=" + _rng.Next());

            return embed;
        }

        private string GetText(ulong guildId, LocStr str)
            => _strings.GetText(str, guildId);

        public bool ToggleStreamOffline(ulong guildId)
        {
            bool newValue;
            using (var uow = _db.GetDbContext())
            {
                var gc = uow.GuildConfigsForId(guildId, set => set);
                newValue = gc.NotifyStreamOffline = !gc.NotifyStreamOffline;
                uow.SaveChanges();

                if (newValue)
                {
                    _offlineNotificationServers.Add(guildId);
                }
                else
                {
                    _offlineNotificationServers.TryRemove(guildId);
                }
            }

            return newValue;
        }

        public Task<StreamData> GetStreamDataAsync(string url)
        {
            return _streamTracker.GetStreamDataByUrlAsync(url);
        }

        private HashSet<FollowedStream> GetLocalGuildStreams(in StreamDataKey key, ulong guildId)
        {
            if (_shardTrackedStreams.TryGetValue(key, out var map))
            {
                if (map.TryGetValue(guildId, out var set))
                {
                    return set;
                }
                else
                {
                    return map[guildId] = new HashSet<FollowedStream>();
                }
            }
            else
            {
                _shardTrackedStreams[key] = new Dictionary<ulong, HashSet<FollowedStream>>()
                {
                    {guildId, new HashSet<FollowedStream>()}
                };
                return _shardTrackedStreams[key][guildId];
            }
        }

        public bool SetStreamMessage(ulong guildId, int index, string message, out FollowedStream fs)
        {
            using (var uow = _db.GetDbContext())
            {
                var fss = uow.Set<FollowedStream>()
                    .AsQueryable()
                    .Where(x => x.GuildId == guildId)
                    .OrderBy(x => x.Id)
                    .ToList();

                if (fss.Count <= index)
                {
                    fs = null;
                    return false;
                }

                fs = fss[index];
                fs.Message = message;
                lock (_shardLock)
                {
                    var streams = GetLocalGuildStreams(fs.CreateKey(), guildId);

                    // message doesn't participate in equality checking
                    // removing and adding = update
                    streams.Remove(fs);
                    streams.Add(fs);
                }

                uow.SaveChanges();
            }

            return true;
        }

        public int SetStreamMessageForAll(ulong guildId, string message)
        {
            using var uow = _db.GetDbContext();

            var all = uow.Set<FollowedStream>()
                .ToList();

            if (all.Count == 0)
                return 0;

            all.ForEach(x => x.Message = message);

            uow.SaveChanges();

            return all.Count;
        }

        public sealed class FollowStreamPubData
        {
            public StreamDataKey Key { get; init; }
            public ulong GuildId { get; init; }
        }
    }
}