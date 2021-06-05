﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using WizBot.Core.Modules.Music;
using WizBot.Core.Services;
using WizBot.Extensions;
using Serilog;

namespace WizBot.Modules.Music.Services
{
    public sealed class MusicService : IMusicService
    {
        private readonly AyuVoiceStateService _voiceStateService;
        private readonly ITrackResolveProvider _trackResolveProvider;
        private readonly DbService _db;
        private readonly IYoutubeResolver _ytResolver;
        private readonly ILocalTrackResolver _localResolver;
        private readonly ISoundcloudResolver _scResolver;
        private readonly DiscordSocketClient _client;
        private readonly IBotStrings _strings;
        private readonly IGoogleApiService _googleApiService;
        private readonly YtLoader _ytLoader;

        private readonly ConcurrentDictionary<ulong, IMusicPlayer> _players;
        private readonly ConcurrentDictionary<ulong, ITextChannel> _outputChannels;

        public MusicService(AyuVoiceStateService voiceStateService, ITrackResolveProvider trackResolveProvider,
            DbService db, IYoutubeResolver ytResolver, ILocalTrackResolver localResolver, ISoundcloudResolver scResolver,
            DiscordSocketClient client, IBotStrings strings, IGoogleApiService googleApiService, YtLoader ytLoader)
        {
            _voiceStateService = voiceStateService;
            _trackResolveProvider = trackResolveProvider;
            _db = db;
            _ytResolver = ytResolver;
            _localResolver = localResolver;
            _scResolver = scResolver;
            _client = client;
            _strings = strings;
            _googleApiService = googleApiService;
            _ytLoader = ytLoader;

            _players = new ConcurrentDictionary<ulong, IMusicPlayer>();
            _outputChannels = new ConcurrentDictionary<ulong, ITextChannel>();
            
            _client.LeftGuild += ClientOnLeftGuild;
        }
        
        private void DisposeMusicPlayer(IMusicPlayer musicPlayer)
        {
            musicPlayer.Kill();
            _ = Task.Delay(10_000).ContinueWith(_ => musicPlayer.Dispose());
        }

        private void RemoveMusicPlayer(ulong guildId)
        {
            _outputChannels.TryRemove(guildId, out _);
            if (_players.TryRemove(guildId, out var mp))
            {
                DisposeMusicPlayer(mp);
            }
        }

        private Task ClientOnLeftGuild(SocketGuild guild)
        {
            RemoveMusicPlayer(guild.Id);
            return Task.CompletedTask;
        }

        public async Task LeaveVoiceChannelAsync(ulong guildId)
        {
            RemoveMusicPlayer(guildId);
            await _voiceStateService.LeaveVoiceChannel(guildId);
        }

        public Task JoinVoiceChannelAsync(ulong guildId, ulong voiceChannelId) 
            => _voiceStateService.JoinVoiceChannel(guildId, voiceChannelId);

        public IMusicPlayer GetOrCreateMusicPlayer(ITextChannel contextChannel)
        {
            var newPLayer = CreateMusicPlayerInternal(contextChannel.GuildId, contextChannel);
            if (newPLayer is null)
                return null;
            
            return _players.GetOrAdd(contextChannel.GuildId, newPLayer);
        }

        public bool TryGetMusicPlayer(ulong guildId, out IMusicPlayer musicPlayer)
            => _players.TryGetValue(guildId, out musicPlayer);

        public void SetDefaultVolume(ulong guildId, int val)
        {
            using var uow = _db.GetDbContext();
            uow.GuildConfigs.ForId(guildId, set => set).DefaultMusicVolume = val / 100.0f;
            uow.SaveChanges();
        }

        public async Task<int> EnqueueYoutubePlaylistAsync(IMusicPlayer mp, string query, string queuer)
        {
            var count = 0;
            await foreach (var track in _ytResolver.ResolveTracksFromPlaylistAsync(query))
            {
                if (mp.IsKilled)
                    break;

                mp.EnqueueTrack(track, queuer);
                ++count;
            }

            return count;
        }

        public async Task EnqueueDirectoryAsync(IMusicPlayer mp, string dirPath, string queuer)
        {
            await foreach (var track in _localResolver.ResolveDirectoryAsync(dirPath))
            {
                if (mp.IsKilled)
                    break;
                
                mp.EnqueueTrack(track, queuer);
            }
        }

        public async Task<int> EnqueueSoundcloudPlaylistAsync(IMusicPlayer mp, string playlist, string queuer)
        {
            var i = 0;
            await foreach (var track in _scResolver.ResolvePlaylistAsync(playlist))
            {
                if (mp.IsKilled)
                    break;
                
                mp.EnqueueTrack(track, queuer);
                ++i;
            }

            return i;
        }

        private IMusicPlayer CreateMusicPlayerInternal(ulong guildId, ITextChannel currentOutputChannel)
        {
            var queue = new MusicQueue();
            var resolver = _trackResolveProvider;

            if (!_voiceStateService.TryGetProxy(guildId, out var proxy))
            {
                return null;
            }

            using var uow = _db.GetDbContext();
            var gc = uow.GuildConfigs.ForId(guildId, set => set.Include(x => x.MusicSettings));

            var outputChannel = currentOutputChannel;
            if (gc.MusicSettings.MusicChannelId is ulong channelId)
            {
                var savedChannel = _client.GetGuild(guildId)?.GetTextChannel(channelId);

                if (savedChannel is null)
                {
                    Log.Warning("Saved music output channel doesn't exist, falling back to current channel");
                }
            }
            
            _outputChannels[guildId] = outputChannel ?? currentOutputChannel;

            var mp = new MusicPlayer(
                queue,
                resolver,
                proxy
            );

            mp.OnCompleted += OnTrackCompleted(guildId);
            mp.OnStarted += OnTrackStarted(guildId);

            if (gc.DefaultMusicVolume >= 0 && gc.DefaultMusicVolume <= 1)
            {
                mp.SetVolume((int)(gc.DefaultMusicVolume * 100));
            }
            else
            {
                Log.Error("DefaultMusicVolume is outside of valid range >= 0 && <=1 ({DefaultMusicVolume})", gc.DefaultMusicVolume);
            }

            return mp;
        }

        public Func<IMusicPlayer, IQueuedTrackInfo, Task> OnTrackCompleted(ulong guildId)
        {
            IUserMessage lastFinishedMessage = null;
            return async (mp, trackInfo) =>
            {
                _ = lastFinishedMessage?.DeleteAsync();
                var embed = new EmbedBuilder()
                    .WithOkColor()
                    .WithAuthor(eab => eab.WithName(GetText(guildId, "finished_song")).WithMusicIcon())
                    .WithDescription(trackInfo.PrettyName())
                    .WithFooter(trackInfo.PrettyTotalTime());

                lastFinishedMessage = await SendToOutputAsync(guildId, embed);
            };
        }
        
        public Func<IMusicPlayer, IQueuedTrackInfo, int, Task> OnTrackStarted(ulong guildId)
        {
            IUserMessage lastPlayingMessage = null;
            return async (mp, trackInfo, index) =>
            {
                _ = lastPlayingMessage?.DeleteAsync();
                var embed = new EmbedBuilder().WithOkColor()
                    .WithAuthor(eab => eab.WithName(GetText(guildId, "playing_song", index + 1)).WithMusicIcon())
                    .WithDescription(trackInfo.PrettyName())
                    .WithFooter(ef => ef.WithText($"{mp.PrettyVolume()} | {trackInfo.PrettyInfo()}"));

                lastPlayingMessage = await SendToOutputAsync(guildId, embed);
            };
        }

        public Task<IUserMessage> SendToOutputAsync(ulong guildId, EmbedBuilder embed)
        {
            if (_outputChannels.TryGetValue(guildId, out var textChannel))
                return textChannel.EmbedAsync(embed);

            return Task.FromResult<IUserMessage>(null);
        }

        public bool SetMusicChannel(ulong guildId, ulong channelId)
        {
            var channel = _client.GetGuild(guildId)?.GetTextChannel(channelId);
            if (channel is null)
                return false;
            
            using var uow = _db.GetDbContext();
            var ms = uow.GuildConfigs.ForId(guildId, set => set.Include(x => x.MusicSettings)).MusicSettings;
            ms.MusicChannelId = channelId;
            uow.SaveChanges();
            
            _outputChannels[guildId] = channel;
            return true;
        }

        public void UnsetMusicChannel(ulong guildId)
        {
            using var uow = _db.GetDbContext();
            var ms = uow.GuildConfigs.ForId(guildId, set => set.Include(x => x.MusicSettings)).MusicSettings;
            ms.MusicChannelId = null;
            uow.SaveChanges();
        }

        // this has to be done because dragging bot to another vc isn't supported yet
        public async Task<bool> PlayAsync(ulong guildId, ulong voiceChannelId)
        {
            if (!TryGetMusicPlayer(guildId, out var mp))
            {
                return false;
            }

            if (mp.IsStopped)
            {
                if (!_voiceStateService.TryGetProxy(guildId, out var proxy) 
                    || proxy.State == VoiceProxy.VoiceProxyState.Stopped)
                {
                    await JoinVoiceChannelAsync(guildId, voiceChannelId);
                }
            }

            mp.Next();
            return true;
        }

        private async Task<IList<(string Title, string Url)>> SearchYtLoaderVideosAsync(string query)
        {
            var result = await _ytLoader.LoadResultsAsync(query);
            return result.Select(x => (x.Title, x.Url)).ToList();
        }
        
        private async Task<IList<(string Title, string Url)>> SearchGoogleApiVideosAsync(string query)
        {
            var result = await _googleApiService.GetVideoInfosByKeywordAsync(query, 5);
            return result.Select(x => (x.Name, x.Url)).ToList();
        }
        
        public async Task<IList<(string Title, string Url)>> SearchVideosAsync(string query)
        {
            try
            {
                IList<(string, string)> videos = await SearchYtLoaderVideosAsync(query);
                if (!(videos is null))
                {
                    return videos;
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Failed geting videos with YtLoader: {ErrorMessage}", ex.Message);
            }

            try
            {
                return await SearchGoogleApiVideosAsync(query);
            }
            catch (Exception ex)
            {
                Log.Warning("Failed getting video results with Google Api. " +
                            "Probably google api key missing: {ErrorMessage}", ex.Message);
            }
            
            return default;
        }

        private string GetText(ulong guildId, string key, params object[] args)
            => _strings.GetText(key, guildId, args);
        
        public IEnumerable<(string Name, Func<string> Func)> GetPlaceholders()
        {
            // random song that's playing
            yield return ("%music.playing%", () =>
            {
                var randomPlayingTrack = _players
                    .Select(x => x.Value.GetCurrentTrack(out _))
                    .Where(x => !(x is null))
                    .Shuffle()
                    .FirstOrDefault();

                if (randomPlayingTrack is null)
                    return "-";

                return randomPlayingTrack.Title;
            });

            // number of servers currently listening to music
            yield return ("%music.servers%", () =>
            {
                var count = _players
                    .Select(x => x.Value.GetCurrentTrack(out _))
                    .Count(x => !(x is null));

                return count.ToString();
            });
            
            yield return ("%music.queued%", () =>
            {
                var count = _players
                    .Sum(x => x.Value.GetQueuedTracks().Count);

                return count.ToString();
            });
        }
    }
}