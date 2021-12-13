﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using WizBot.Common;
using WizBot.Common.Attributes;
using WizBot.Services.Database.Models;
using WizBot.Extensions;
using WizBot.Modules.Administration.Services;
using WizBot.Modules.Music.Services;

namespace WizBot.Modules.Music
{
    [NoPublicBot]
    public sealed partial class Music : WizBotModule<IMusicService>
    {
        public const string MusicIconUrl = "http://i.imgur.com/nhKS3PT.png";
        private readonly ILogCommandService _logService;

        public Music(ILogCommandService _logService)
        {
            this._logService = _logService;
        }
        
        private async Task<bool> ValidateAsync()
        {
            var user = (IGuildUser) ctx.User;
            var userVoiceChannelId = user.VoiceChannel?.Id;
            
            if (userVoiceChannelId is null)
            {
                await ReplyErrorLocalizedAsync(strs.must_be_in_voice);
                return false;
            }

            var currentUser = await ctx.Guild.GetCurrentUserAsync();
            if (currentUser.VoiceChannel?.Id != userVoiceChannelId)
            {
                await ReplyErrorLocalizedAsync(strs.not_with_bot_in_voice);
                return false;
            }

            return true;
        }

        private static readonly SemaphoreSlim voiceChannelLock = new SemaphoreSlim(1, 1);
        private async Task EnsureBotInVoiceChannelAsync(ulong voiceChannelId, IGuildUser botUser = null)
        {
            botUser ??= await ctx.Guild.GetCurrentUserAsync();
            await voiceChannelLock.WaitAsync();
            try
            {
                if (botUser.VoiceChannel?.Id is null || !_service.TryGetMusicPlayer(ctx.Guild.Id, out _))
                    await _service.JoinVoiceChannelAsync(ctx.Guild.Id, voiceChannelId);
            }
            finally
            {
                voiceChannelLock.Release();
            }
        }
        
        private async Task<bool> QueuePreconditionInternalAsync()
        {
            var user = (IGuildUser) ctx.User;
            var voiceChannelId = user.VoiceChannel?.Id;
            
            if (voiceChannelId is null)
            {
                await ReplyErrorLocalizedAsync(strs.must_be_in_voice);
                return false;
            }

            _ = ctx.Channel.TriggerTypingAsync();
            
            var botUser = await ctx.Guild.GetCurrentUserAsync();
            await EnsureBotInVoiceChannelAsync(voiceChannelId!.Value, botUser);
            
            if (botUser.VoiceChannel?.Id != voiceChannelId)
            {
                await ReplyErrorLocalizedAsync(strs.not_with_bot_in_voice);
                return false;
            }

            return true;
        }

        private async Task QueueByQuery(string query, bool asNext = false, MusicPlatform? forcePlatform = null)
        {
            var succ = await QueuePreconditionInternalAsync();
            if (!succ)
                return;
            
            var mp = await _service.GetOrCreateMusicPlayerAsync((ITextChannel) ctx.Channel);
            if (mp is null)
            {
                await ReplyErrorLocalizedAsync(strs.no_player);
                return;
            }
            
            var (trackInfo, index) = await mp.TryEnqueueTrackAsync(query, 
                ctx.User.ToString(),
                asNext,
                forcePlatform);
            if (trackInfo is null)
            {
                await ReplyErrorLocalizedAsync(strs.song_not_found);
                return;
            }

            try
            {
                var embed = _eb.Create()
                    .WithOkColor()
                    .WithAuthor(GetText(strs.queued_song) + " #" + (index + 1), MusicIconUrl)
                    .WithDescription($"{trackInfo.PrettyName()}\n{GetText(strs.queue)} ")
                    .WithFooter(trackInfo.Platform.ToString());

                if (!string.IsNullOrWhiteSpace(trackInfo.Thumbnail))
                    embed.WithThumbnailUrl(trackInfo.Thumbnail);

                var queuedMessage = await _service.SendToOutputAsync(ctx.Guild.Id, embed).ConfigureAwait(false);
                queuedMessage?.DeleteAfter(10, _logService);
                if (mp.IsStopped)
                {
                    var msg = await ReplyPendingLocalizedAsync(strs.queue_stopped(Format.Code(Prefix + "play")));
                    msg.DeleteAfter(10, _logService);
                }
            }
            catch
            {
                // ignored
            }
        }

        private async Task MoveToIndex(int index)
        {
            if (--index < 0)
                return;
            
            var succ = await QueuePreconditionInternalAsync();
            if (!succ)
                return;
            
            var mp = await _service.GetOrCreateMusicPlayerAsync((ITextChannel) ctx.Channel);
            if (mp is null)
            {
                await ReplyErrorLocalizedAsync(strs.no_player);
                return;
            }

            mp.MoveTo(index);
        }
        
        // join vc
        [WizBotCommand, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Join()
        {
            var user = (IGuildUser) ctx.User;

            var voiceChannelId = user.VoiceChannel?.Id;

            if (voiceChannelId is null)
            {
                await ReplyErrorLocalizedAsync(strs.must_be_in_voice);
                return;
            }

            await _service.JoinVoiceChannelAsync(user.GuildId, voiceChannelId.Value);
        }

        // leave vc (destroy)
        [WizBotCommand, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Destroy()
        {
            var valid = await ValidateAsync();
            if (!valid)
                return;

            await _service.LeaveVoiceChannelAsync(ctx.Guild.Id);
        }
        
        // play - no args = next
        [WizBotCommand, Aliases]
        [RequireContext(ContextType.Guild)]
        [Priority(2)]
        public Task Play()
            => Next();
        
        // play - index = skip to that index
        [WizBotCommand, Aliases]
        [RequireContext(ContextType.Guild)]
        [Priority(1)]
        public Task Play(int index)
            => MoveToIndex(index);
        
        // play - query = q(query)
        [WizBotCommand, Aliases]
        [RequireContext(ContextType.Guild)]
        [Priority(0)]
        public Task Play([Leftover] string query)
            => QueueByQuery(query); 
        
        [WizBotCommand, Aliases]
        [RequireContext(ContextType.Guild)]
        public Task Queue([Leftover] string query)
            => QueueByQuery(query);
        
        [WizBotCommand, Aliases]
        [RequireContext(ContextType.Guild)]
        public Task QueueNext([Leftover] string query)
            => QueueByQuery(query, asNext: true);

        [WizBotCommand, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Volume(int vol)
        {
            if (vol < 0 || vol > 100)
            {
                await ReplyErrorLocalizedAsync(strs.volume_input_invalid);
                return;
            }
            
            var valid = await ValidateAsync();
            if (!valid)
                return;

            await _service.SetVolumeAsync(ctx.Guild.Id, vol);
            await ReplyConfirmLocalizedAsync(strs.volume_set(vol));
        }

        [WizBotCommand, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Next()
        {
            var valid = await ValidateAsync();
            if (!valid)
                return;

            var success = await _service.PlayAsync(ctx.Guild.Id, ((IGuildUser)ctx.User).VoiceChannel.Id);
            if (!success)
            {
                await ReplyErrorLocalizedAsync(strs.no_player);
                return;
            }
        }

        private const int LQ_ITEMS_PER_PAGE = 9;
        
        // list queue, relevant page
        [WizBotCommand, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task ListQueue()
        {
            // show page with the current song
            if (!_service.TryGetMusicPlayer(ctx.Guild.Id, out var mp))
            {
                await ReplyErrorLocalizedAsync(strs.no_player);
                return;
            }
            
            await ListQueue(mp.CurrentIndex / LQ_ITEMS_PER_PAGE + 1);
        }
        
        // list queue, specify page
        [WizBotCommand, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task ListQueue(int page)
        {
            if (--page < 0)
                return;

            IReadOnlyCollection<IQueuedTrackInfo> tracks;
            if (!_service.TryGetMusicPlayer(ctx.Guild.Id, out var mp) || (tracks = mp.GetQueuedTracks()).Count == 0)
            {
                await ReplyErrorLocalizedAsync(strs.no_player);
                return;
            }
            
            IEmbedBuilder printAction(int curPage)
            {
                string desc = string.Empty;
                var current = mp.GetCurrentTrack(out var currentIndex);
                if (current is not null)
                {
                    desc = $"`🔊` {current.PrettyFullName()}\n\n" + desc;
                }

                var repeatType = mp.Repeat;
                var add = "";
                if (mp.IsStopped)
                    add += Format.Bold(GetText(strs.queue_stopped(Format.Code(Prefix + "play")))) + "\n";
                 // var mps = mp.MaxPlaytimeSeconds;
                 // if (mps > 0)
                 //     add += Format.Bold(GetText(strs.song_skips_after(TimeSpan.FromSeconds(mps).ToString("HH\\:mm\\:ss")))) + "\n";
                 if (repeatType == PlayerRepeatType.Track)
                 {
                     add += "🔂 " + GetText(strs.repeating_track) + "\n";
                 }
                 else
                 {
                     // if (mp.Autoplay)
                     //     add += "↪ " + GetText(strs.autoplaying) + "\n";
                     // if (mp.FairPlay && !mp.Autoplay)
                     //     add += " " + GetText(strs.fairplay) + "\n";
                     if (repeatType == PlayerRepeatType.Queue)
                         add += "🔁 " + GetText(strs.repeating_queue) + "\n";
                 }


                desc += tracks
                    .Skip(LQ_ITEMS_PER_PAGE * curPage)
                    .Take(LQ_ITEMS_PER_PAGE)
                    .Select((v, index) =>
                    {
                        index += LQ_ITEMS_PER_PAGE * curPage;
                        if (index == currentIndex)
                             return $"**⇒**`{index + 1}.` {v.PrettyFullName()}";
                         
                        return $"`{index + 1}.` {v.PrettyFullName()}";
                     })
                    .JoinWith('\n');
                 
                if (!string.IsNullOrWhiteSpace(add))
                    desc = add + "\n" + desc;

                var embed = _eb.Create()
                    .WithAuthor(GetText(strs.player_queue(curPage + 1, (tracks.Count / LQ_ITEMS_PER_PAGE) + 1)),
                        MusicIconUrl)
                    .WithDescription(desc)
                    .WithFooter($"  {mp.PrettyVolume()}  |  🎶 {tracks.Count}  |  ⌛ {mp.PrettyTotalTime()}  ")
                    .WithOkColor();

                return embed;
             }

            await ctx.SendPaginatedConfirmAsync(
                page,
                printAction,
                tracks.Count,
                LQ_ITEMS_PER_PAGE,
                false);
        }

        // search
        [WizBotCommand, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task QueueSearch([Leftover] string query)
        {
            _ = ctx.Channel.TriggerTypingAsync();

            var videos = await _service.SearchVideosAsync(query);

            if (videos is null || videos.Count == 0)
            {
                await ReplyErrorLocalizedAsync(strs.song_not_found).ConfigureAwait(false);
                return;
            }

            var resultsString = videos
                .Select((x, i) => $"`{i + 1}.`\n\t{Format.Bold(x.Title)}\n\t{x.Url}")
                .JoinWith('\n');
            
            var msg = await SendConfirmAsync(resultsString);

            try
            {
                var input = await GetUserInputAsync(ctx.User.Id, ctx.Channel.Id).ConfigureAwait(false);
                if (input is null
                    || !int.TryParse(input, out var index)
                    || (index -= 1) < 0
                    || index >= videos.Count)
                {
                    _logService.AddDeleteIgnore(msg.Id);
                    try
                    {
                        await msg.DeleteAsync().ConfigureAwait(false);
                    }
                    catch
                    {
                    }

                    return;
                }
                query = videos[index].Url;

                await Play(query);
            }
            finally
            {
                _logService.AddDeleteIgnore(msg.Id);
                try
                {
                    await msg.DeleteAsync().ConfigureAwait(false);
                }
                catch
                {
                }
            }
        }

        [WizBotCommand, Aliases]
        [RequireContext(ContextType.Guild)]
        [Priority(1)]
        public async Task TrackRemove(int index)
        {
            if (index < 1)
            {
                await ReplyErrorLocalizedAsync(strs.removed_song_error).ConfigureAwait(false);
                return;
            }
            
            var valid = await ValidateAsync();
            if (!valid)
                return;

            if (!_service.TryGetMusicPlayer(ctx.Guild.Id, out var mp))
            {
                await ReplyErrorLocalizedAsync(strs.no_player);
                return;
            }
            
            if (!mp.TryRemoveTrackAt(index - 1, out var song))
            {
                await ReplyErrorLocalizedAsync(strs.removed_song_error).ConfigureAwait(false);
                return;
            }
            
            var embed = _eb.Create()
                .WithAuthor(GetText(strs.removed_song) + " #" + (index), MusicIconUrl)
                .WithDescription(song.PrettyName())
                .WithFooter(song.PrettyInfo())
                .WithErrorColor();

            await _service.SendToOutputAsync(ctx.Guild.Id, embed);
        }

         public enum All { All = -1 }
         [WizBotCommand, Aliases]
         [RequireContext(ContextType.Guild)]
         [Priority(0)]
         public async Task TrackRemove(All _ = All.All)
         {
             var valid = await ValidateAsync();
             if (!valid)
                 return;

             if (!_service.TryGetMusicPlayer(ctx.Guild.Id, out var mp))
             {
                 await ReplyErrorLocalizedAsync(strs.no_player);
                 return;
             }
             
             mp.Clear();
             await ReplyConfirmLocalizedAsync(strs.queue_cleared).ConfigureAwait(false);
         }
         
         [WizBotCommand, Aliases]
         [RequireContext(ContextType.Guild)]
         public async Task Stop()
         {
             var valid = await ValidateAsync();
             if (!valid)
                 return;

             if (!_service.TryGetMusicPlayer(ctx.Guild.Id, out var mp))
             {
                 await ReplyErrorLocalizedAsync(strs.no_player);
                 return;
             }
             
             mp.Stop();
         }

         public enum InputRepeatType
         {
             N = 0, No = 0, None = 0,
             T = 1, Track = 1, S = 1, Song = 1,
             Q = 2, Queue = 2, Playlist = 2, Pl = 2,
         }

         private PlayerRepeatType InputToDbType(InputRepeatType type) => type switch
         {
             InputRepeatType.None => PlayerRepeatType.None,
             InputRepeatType.Queue => PlayerRepeatType.Queue,
             InputRepeatType.Track => PlayerRepeatType.Track,
             _ => PlayerRepeatType.Queue
         };
         
         [WizBotCommand, Aliases]
         [RequireContext(ContextType.Guild)]
         public async Task QueueRepeat(InputRepeatType type = InputRepeatType.Queue)
         {
             var valid = await ValidateAsync();
             if (!valid)
                 return;
             
             await _service.SetRepeatAsync(ctx.Guild.Id, InputToDbType(type));

             if (type == InputRepeatType.None)
                 await ReplyConfirmLocalizedAsync(strs.repeating_none);
             else if (type == InputRepeatType.Queue)
                 await ReplyConfirmLocalizedAsync(strs.repeating_queue);
             else
                 await ReplyConfirmLocalizedAsync(strs.repeating_track);
         }
         
         [WizBotCommand, Aliases]
         [RequireContext(ContextType.Guild)]
         public async Task ReptCurSong()
         {
             await ReplyPendingLocalizedAsync(strs.obsolete_use($"`{Prefix}qrp song`"));
             await QueueRepeat(InputRepeatType.Song);
         }
         
         [WizBotCommand, Aliases]
         [RequireContext(ContextType.Guild)]
         public async Task Pause()
         {
             var valid = await ValidateAsync();
             if (!valid)
                 return;

             if (!_service.TryGetMusicPlayer(ctx.Guild.Id, out var mp) || mp.GetCurrentTrack(out _) is null)
             {
                 await ReplyErrorLocalizedAsync(strs.no_player);
                 return;
             }

             mp.TogglePause();
         }
         
         [WizBotCommand, Aliases]
         [RequireContext(ContextType.Guild)]
         public Task Radio(string radioLink)
             => QueueByQuery(radioLink, false, MusicPlatform.Radio);

         [WizBotCommand, Aliases]
         [RequireContext(ContextType.Guild)]
         [OwnerOnly]
         public Task Local([Leftover] string path)
             => QueueByQuery(path, false, MusicPlatform.Local);

         [WizBotCommand, Aliases]
         [RequireContext(ContextType.Guild)]
         [OwnerOnly]
         public async Task LocalPlaylist([Leftover] string dirPath)
         {
             if (string.IsNullOrWhiteSpace(dirPath))
                 return;

             var user = (IGuildUser) ctx.User;
             var voiceChannelId = user.VoiceChannel?.Id;
        
             if (voiceChannelId is null)
             {
                 await ReplyErrorLocalizedAsync(strs.must_be_in_voice);
                 return;
             }

             _ = ctx.Channel.TriggerTypingAsync();
        
             var botUser = await ctx.Guild.GetCurrentUserAsync();
             await EnsureBotInVoiceChannelAsync(voiceChannelId!.Value, botUser);
        
             if (botUser.VoiceChannel?.Id != voiceChannelId)
             {
                 await ReplyErrorLocalizedAsync(strs.not_with_bot_in_voice);
                 return;
             }
            
             var mp = await _service.GetOrCreateMusicPlayerAsync((ITextChannel) ctx.Channel);
             if (mp is null)
             {
                 await ReplyErrorLocalizedAsync(strs.no_player);
                 return;
             }
             
             await _service.EnqueueDirectoryAsync(mp, dirPath, ctx.User.ToString());
             
             await ReplyConfirmLocalizedAsync(strs.dir_queue_complete).ConfigureAwait(false);
         }
         
         [WizBotCommand, Aliases]
         [RequireContext(ContextType.Guild)]
         public async Task MoveSong(int from, int to)
         {
             if (--from < 0 || --to < 0 || from == to)
             {
                 await ReplyErrorLocalizedAsync(strs.invalid_input).ConfigureAwait(false);
                 return;
             }

             var valid = await ValidateAsync();
             if (!valid)
                 return;
             
             var mp = await _service.GetOrCreateMusicPlayerAsync((ITextChannel) ctx.Channel);
             if (mp is null)
             {
                 await ReplyErrorLocalizedAsync(strs.no_player);
                 return;
             }

             var track = mp.MoveTrack(from, to);
             if (track is null)
             {
                 await ReplyErrorLocalizedAsync(strs.invalid_input).ConfigureAwait(false);
                 return;
             }
             
             var embed = _eb.Create()
                 .WithTitle(track.Title.TrimTo(65))
                 .WithAuthor(GetText(strs.song_moved), MusicIconUrl)
                 .AddField(GetText(strs.from_position), $"#{from + 1}", true)
                 .AddField(GetText(strs.to_position), $"#{to + 1}", true)
                 .WithOkColor();

             if (Uri.IsWellFormedUriString(track.Url, UriKind.Absolute))
                 embed.WithUrl(track.Url);

             await ctx.Channel.EmbedAsync(embed).ConfigureAwait(false);
         }

         [WizBotCommand, Aliases]
         [RequireContext(ContextType.Guild)]
         public Task SoundCloudQueue([Leftover] string query)
             => QueueByQuery(query, false, MusicPlatform.SoundCloud);
         
         [WizBotCommand, Aliases]
         [RequireContext(ContextType.Guild)]
         public async Task SoundCloudPl([Leftover] string playlist)
         {
             if (string.IsNullOrWhiteSpace(playlist))
                 return;

             var succ = await QueuePreconditionInternalAsync();
             if (!succ)
                 return;

             var mp = await _service.GetOrCreateMusicPlayerAsync((ITextChannel) ctx.Channel);
             if (mp is null)
             {
                 await ReplyErrorLocalizedAsync(strs.no_player);
                 return;
             }
             
             _ = ctx.Channel.TriggerTypingAsync();

             await _service.EnqueueSoundcloudPlaylistAsync(mp, playlist, ctx.User.ToString());

             await ctx.OkAsync();
         }

         [WizBotCommand, Aliases]
         [RequireContext(ContextType.Guild)]
         public async Task Playlist([Leftover] string playlistQuery)
         {
             if (string.IsNullOrWhiteSpace(playlistQuery))
                 return;

             var succ = await QueuePreconditionInternalAsync();
             if (!succ)
                 return;

             var mp = await _service.GetOrCreateMusicPlayerAsync((ITextChannel) ctx.Channel);
             if (mp is null)
             {
                 await ReplyErrorLocalizedAsync(strs.no_player);
                 return;
             }

             _ = ctx.Channel.TriggerTypingAsync();


             var queuedCount = await _service.EnqueueYoutubePlaylistAsync(mp, playlistQuery, ctx.User.ToString());
             if (queuedCount == 0)
             {
                 await ReplyErrorLocalizedAsync(strs.no_search_results).ConfigureAwait(false);
                 return;
             }
             await ctx.OkAsync();
         }

         [WizBotCommand, Aliases]
         [RequireContext(ContextType.Guild)]
         public async Task NowPlaying()
         {
             var mp = await _service.GetOrCreateMusicPlayerAsync((ITextChannel) ctx.Channel);
             if (mp is null)
             {
                 await ReplyErrorLocalizedAsync(strs.no_player);
                 return;
             }

             var currentTrack = mp.GetCurrentTrack(out _);
             if (currentTrack is null)
                 return;

             var embed = _eb.Create().WithOkColor()
                 .WithAuthor(GetText(strs.now_playing), MusicIconUrl)
                 .WithDescription(currentTrack.PrettyName())
                 .WithThumbnailUrl(currentTrack.Thumbnail)
                 .WithFooter($"{mp.PrettyVolume()} | {mp.PrettyTotalTime()} | {currentTrack.Platform} | {currentTrack.Queuer}");

             await ctx.Channel.EmbedAsync(embed).ConfigureAwait(false);
         }

         [WizBotCommand, Aliases]
         [RequireContext(ContextType.Guild)]
         public async Task PlaylistShuffle()
         {
             var valid = await ValidateAsync();
             if (!valid)
                 return;
             
             var mp = await _service.GetOrCreateMusicPlayerAsync((ITextChannel) ctx.Channel);
             if (mp is null)
             {
                 await ReplyErrorLocalizedAsync(strs.no_player);
                 return;
             }
             
             mp.ShuffleQueue();
             await ReplyConfirmLocalizedAsync(strs.queue_shuffled);
         }

         [WizBotCommand, Aliases]
         [RequireContext(ContextType.Guild)]
         [UserPerm(GuildPerm.ManageMessages)]
         public async Task SetMusicChannel()
         {
             await _service.SetMusicChannelAsync(ctx.Guild.Id, ctx.Channel.Id);

             await ReplyConfirmLocalizedAsync(strs.set_music_channel);
         }
         
         [WizBotCommand, Aliases]
         [RequireContext(ContextType.Guild)]
         [UserPerm(GuildPerm.ManageMessages)]
         public async Task UnsetMusicChannel()
         {
             await _service.SetMusicChannelAsync(ctx.Guild.Id, null);

             await ReplyConfirmLocalizedAsync(strs.unset_music_channel);
         }

         [WizBotCommand, Aliases]
         [RequireContext(ContextType.Guild)]
         public async Task AutoDisconnect()
         {
             var newState = await _service.ToggleAutoDisconnectAsync(ctx.Guild.Id);

             if(newState)
                await ReplyConfirmLocalizedAsync(strs.autodc_enable);
             else
                await ReplyConfirmLocalizedAsync(strs.autodc_disable);
         }

         [WizBotCommand, Aliases]
         [RequireContext(ContextType.Guild)]
         [UserPerm(GuildPerm.Administrator)]
         public async Task MusicQuality()
         {
             var quality = await _service.GetMusicQualityAsync(ctx.Guild.Id);
             await ReplyConfirmLocalizedAsync(strs.current_music_quality(Format.Bold(quality.ToString())));
         }
         
         [WizBotCommand, Aliases]
         [RequireContext(ContextType.Guild)]
         [UserPerm(GuildPerm.Administrator)]
         public async Task MusicQuality(QualityPreset preset)
         {
             await _service.SetMusicQualityAsync(ctx.Guild.Id, preset);
             await ReplyConfirmLocalizedAsync(strs.music_quality_set(Format.Bold(preset.ToString())));
         }
    }
}