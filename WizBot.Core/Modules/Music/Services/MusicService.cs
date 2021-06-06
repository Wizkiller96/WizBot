﻿#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using WizBot.Core.Modules.Music;
using WizBot.Core.Services;
using WizBot.Core.Services.Database.Models;
using WizBot.Core.Services.Database.Repositories.Impl;
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
        private readonly ConcurrentDictionary<ulong, (ITextChannel Default, ITextChannel? Override)> _outputChannels;
        private readonly ConcurrentDictionary<ulong, MusicPlayerSettings> _settings;

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
            _outputChannels = new ConcurrentDictionary<ulong, (ITextChannel, ITextChannel?)>();
            _settings = new ConcurrentDictionary<ulong, MusicPlayerSettings>();
            
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

        public async Task<IMusicPlayer?> GetOrCreateMusicPlayerAsync(ITextChannel contextChannel)
        {
            var newPLayer = await CreateMusicPlayerInternalAsync(contextChannel.GuildId, contextChannel);
            if (newPLayer is null)
                return null;
            
            return _players.GetOrAdd(contextChannel.GuildId, newPLayer);
        }

        public bool TryGetMusicPlayer(ulong guildId, out IMusicPlayer musicPlayer)
            => _players.TryGetValue(guildId, out musicPlayer);

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

        private async Task<IMusicPlayer?> CreateMusicPlayerInternalAsync(ulong guildId, ITextChannel defaultChannel)
        {
            var queue = new MusicQueue();
            var resolver = _trackResolveProvider;

            if (!_voiceStateService.TryGetProxy(guildId, out var proxy))
            {
                return null;
            }

            var settings = await GetSettingsInternalAsync(guildId);

            ITextChannel? overrideChannel = null;
            if (settings.MusicChannelId is ulong channelId)
            {
                overrideChannel = _client.GetGuild(guildId)?.GetTextChannel(channelId);

                if (overrideChannel is null)
                {
                    Log.Warning("Saved music output channel doesn't exist, falling back to current channel");
                }
            }
            
            _outputChannels[guildId] = (defaultChannel, overrideChannel);

            var mp = new MusicPlayer(
                queue,
                resolver,
                proxy
            );
            
            mp.SetRepeat(settings.PlayerRepeat);

            if (settings.Volume >= 0 && settings.Volume <= 100)
            {
                mp.SetVolume(settings.Volume);
            }
            else
            {
                Log.Error("Saved Volume is outside of valid range >= 0 && <=100 ({Volume})", settings.Volume);
            }

            mp.OnCompleted += OnTrackCompleted(guildId);
            mp.OnStarted += OnTrackStarted(guildId);
            mp.OnQueueStopped += OnQueueStopped(guildId);

            return mp;
        }

        public Task<IUserMessage?> SendToOutputAsync(ulong guildId, EmbedBuilder embed)
        {
            if (_outputChannels.TryGetValue(guildId, out var chan))
                return (chan.Default ?? chan.Override).EmbedAsync(embed);

            return Task.FromResult<IUserMessage?>(null);
        }

        private Func<IMusicPlayer, IQueuedTrackInfo, Task> OnTrackCompleted(ulong guildId)
        {
            IUserMessage? lastFinishedMessage = null;
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

        private Func<IMusicPlayer, IQueuedTrackInfo, int, Task> OnTrackStarted(ulong guildId)
        {
            IUserMessage? lastPlayingMessage = null;
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

        private Func<IMusicPlayer, Task> OnQueueStopped(ulong guildId)
            => (mp) =>
            {
                if (_settings.TryGetValue(guildId, out var settings))
                {
                    if (settings.AutoDisconnect)
                    {
                        return LeaveVoiceChannelAsync(guildId);
                    }
                }

                return Task.CompletedTask;
            };

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
                if (videos.Count > 0)
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
            
            return Array.Empty<(string, string)>();
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

        #region Settings

        private async Task<MusicPlayerSettings> GetSettingsInternalAsync(ulong guildId)
        {
            if (_settings.TryGetValue(guildId, out var settings))
                return settings;
            
            using var uow = _db.GetDbContext();
            var toReturn = _settings[guildId] = await uow._context.MusicPlayerSettings.ForGuildAsync(guildId);
            await uow.SaveChangesAsync();

            return toReturn;
        }
        
        private async Task ModifySettingsInternalAsync<TState>(
            ulong guildId,
            Action<MusicPlayerSettings, TState> action,
            TState state)
        {
            using var uow = _db.GetDbContext();
            var ms = await uow._context.MusicPlayerSettings.ForGuildAsync(guildId);
            action(ms, state);
            await uow.SaveChangesAsync();
            _settings[guildId] = ms;
        }
        
        public async Task<bool> SetMusicChannelAsync(ulong guildId, ulong? channelId)
        {
            if (channelId is null)
            {
                await UnsetMusicChannelAsync(guildId);
                return true;
            }
            
            var channel = _client.GetGuild(guildId)?.GetTextChannel(channelId.Value);
            if (channel is null)
                return false;

            await ModifySettingsInternalAsync(guildId, (settings, chId) =>
            {
                settings.MusicChannelId = chId;
            }, channelId);

            _outputChannels.AddOrUpdate(guildId,
                (channel, channel),
                (key, old) => (old.Default, channel));
            
            return true;
        }

        public async Task UnsetMusicChannelAsync(ulong guildId)
        {
            await ModifySettingsInternalAsync(guildId, (settings, _) =>
            {
                settings.MusicChannelId = null;
            }, (ulong?)null);

            if (_outputChannels.TryGetValue(guildId, out var old))
                _outputChannels[guildId] = (old.Default, null);
        }

        public async Task SetRepeatAsync(ulong guildId, PlayerRepeatType repeatType)
        {
            await ModifySettingsInternalAsync(guildId, (settings, type) =>
            {
                settings.PlayerRepeat = type;
            }, repeatType);

            if (TryGetMusicPlayer(guildId, out var mp))
                mp.SetRepeat(repeatType);
        }

        public async Task SetVolumeAsync(ulong guildId, int value)
        {
            if (value < 0 || value > 100)
                throw new ArgumentOutOfRangeException(nameof(value));
            
            await ModifySettingsInternalAsync(guildId, (settings, newValue) =>
            {
                settings.Volume = newValue;
            }, value);
            
            if (TryGetMusicPlayer(guildId, out var mp))
                mp.SetVolume(value);
        }

        public async Task<bool> ToggleAutoDisconnectAsync(ulong guildId)
        {
            var newState = false;
            await ModifySettingsInternalAsync(guildId, (settings, _) =>
            {
                newState = settings.AutoDisconnect = !settings.AutoDisconnect;
            }, default(object));

            return newState;
        }

        #endregion
    }
}