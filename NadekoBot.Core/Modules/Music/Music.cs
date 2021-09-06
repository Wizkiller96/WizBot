using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using NadekoBot.Common;
using NadekoBot.Common.Attributes;
using NadekoBot.Core.Services.Database.Models;
using NadekoBot.Extensions;
using NadekoBot.Modules;
using NadekoBot.Modules.Administration.Services;
using NadekoBot.Modules.Music.Services;

namespace NadekoBot.Core.Modules.Music
{
    [NoPublicBot]
    public sealed partial class Music : NadekoModule<IMusicService>
    {
        private readonly LogCommandService _logService;

        public Music(LogCommandService _logService)
        {
            this._logService = _logService;
        }
        
        private async Task<bool> ValidateAsync()
        {
            var user = (IGuildUser) ctx.User;
            var userVoiceChannelId = user.VoiceChannel?.Id;
            
            if (userVoiceChannelId is null)
            {
                await ReplyErrorLocalizedAsync("must_be_in_voice");
                return false;
            }

            var currentUser = await ctx.Guild.GetCurrentUserAsync();
            if (currentUser.VoiceChannel?.Id != userVoiceChannelId)
            {
                await ReplyErrorLocalizedAsync("not_with_bot_in_voice");
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
                if (botUser.VoiceChannel?.Id is null || !_service.TryGetMusicPlayer(Context.Guild.Id, out _))
                    await _service.JoinVoiceChannelAsync(ctx.Guild.Id, voiceChannelId);
            }
            finally
            {
                voiceChannelLock.Release();
            }
        }
        
        private async Task<bool> QueuePreconditionInternalAsync()
        {
            var user = (IGuildUser) Context.User;
            var voiceChannelId = user.VoiceChannel?.Id;
            
            if (voiceChannelId is null)
            {
                await ReplyErrorLocalizedAsync("must_be_in_voice");
                return false;
            }

            _ = ctx.Channel.TriggerTypingAsync();
            
            var botUser = await ctx.Guild.GetCurrentUserAsync();
            await EnsureBotInVoiceChannelAsync(voiceChannelId!.Value, botUser);
            
            if (botUser.VoiceChannel?.Id != voiceChannelId)
            {
                await ReplyErrorLocalizedAsync("not_with_bot_in_voice");
                return false;
            }

            return true;
        }

        private async Task QueueByQuery(string query, bool asNext = false, MusicPlatform? forcePlatform = null)
        {
            var succ = await QueuePreconditionInternalAsync();
            if (!succ)
                return;
            
            var mp = await _service.GetOrCreateMusicPlayerAsync((ITextChannel) Context.Channel);
            if (mp is null)
            {
                await ReplyErrorLocalizedAsync("no_player");
                return;
            }
            
            var (trackInfo, index) = await mp.TryEnqueueTrackAsync(query, 
                Context.User.ToString(),
                asNext,
                forcePlatform);
            if (trackInfo is null)
            {
                await ReplyErrorLocalizedAsync("song_not_found");
                return;
            }

            try
            {
                var embed = new EmbedBuilder()
                    .WithOkColor()
                    .WithAuthor(eab => eab.WithName(GetText("queued_song") + " #" + (index + 1)).WithMusicIcon())
                    .WithDescription($"{trackInfo.PrettyName()}\n{GetText("queue")} ")
                    .WithFooter(ef => ef.WithText(trackInfo.Platform.ToString()));

                if (!string.IsNullOrWhiteSpace(trackInfo.Thumbnail))
                    embed.WithThumbnailUrl(trackInfo.Thumbnail);

                var queuedMessage = await _service.SendToOutputAsync(Context.Guild.Id, embed).ConfigureAwait(false);
                queuedMessage?.DeleteAfter(10, _logService);
                if (mp.IsStopped)
                {
                    var msg = await ReplyErrorLocalizedAsync("queue_stopped", Format.Code(Prefix + "play"));
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
            
            var mp = await _service.GetOrCreateMusicPlayerAsync((ITextChannel) Context.Channel);
            if (mp is null)
            {
                await ReplyErrorLocalizedAsync("no_player");
                return;
            }

            mp.MoveTo(index);
        }
        
        // join vc
        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Join()
        {
            var user = (IGuildUser) Context.User;

            var voiceChannelId = user.VoiceChannel?.Id;

            if (voiceChannelId is null)
            {
                await ReplyErrorLocalizedAsync("must_be_in_voice");
                return;
            }

            await _service.JoinVoiceChannelAsync(user.GuildId, voiceChannelId.Value);
        }

        // leave vc (destroy)
        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Destroy()
        {
            var valid = await ValidateAsync();
            if (!valid)
                return;

            await _service.LeaveVoiceChannelAsync(Context.Guild.Id);
        }
        
        // play - no args = next
        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [Priority(2)]
        public Task Play()
            => Next();
        
        // play - index = skip to that index
        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [Priority(1)]
        public Task Play(int index)
            => MoveToIndex(index);
        
        // play - query = q(query)
        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [Priority(0)]
        public Task Play([Leftover] string query)
            => QueueByQuery(query); 
        
        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public Task Queue([Leftover] string query)
            => QueueByQuery(query);
        
        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public Task QueueNext([Leftover] string query)
            => QueueByQuery(query, asNext: true);

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Volume(int vol)
        {
            if (vol < 0 || vol > 100)
            {
                await ReplyErrorLocalizedAsync("volume_input_invalid");
                return;
            }
            
            var valid = await ValidateAsync();
            if (!valid)
                return;

            await _service.SetVolumeAsync(ctx.Guild.Id, vol);
            await ReplyConfirmLocalizedAsync("volume_set", vol);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Next()
        {
            var valid = await ValidateAsync();
            if (!valid)
                return;

            var success = await _service.PlayAsync(Context.Guild.Id, ((IGuildUser)Context.User).VoiceChannel.Id);
            if (!success)
            {
                await ReplyErrorLocalizedAsync("no_player");
                return;
            }
        }

        private const int LQ_ITEMS_PER_PAGE = 9;
        
        // list queue, relevant page
        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task ListQueue()
        {
            // show page with the current song
            if (!_service.TryGetMusicPlayer(ctx.Guild.Id, out var mp))
            {
                await ReplyErrorLocalizedAsync("no_player");
                return;
            }
            
            await ListQueue(mp.CurrentIndex / LQ_ITEMS_PER_PAGE + 1);
        }
        
        // list queue, specify page
        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task ListQueue(int page)
        {
            if (--page < 0)
                return;

            IReadOnlyCollection<IQueuedTrackInfo> tracks;
            if (!_service.TryGetMusicPlayer(ctx.Guild.Id, out var mp) || (tracks = mp.GetQueuedTracks()).Count == 0)
            {
                await ReplyErrorLocalizedAsync("no_player");
                return;
            }
            
            EmbedBuilder printAction(int curPage)
            {
                string desc = string.Empty;
                var current = mp.GetCurrentTrack(out var currentIndex);
                if (!(current is null))
                {
                    desc = $"`🔊` {current.PrettyFullName()}\n\n" + desc;
                }

                var repeatType = mp.Repeat;
                var add = "";
                if (mp.IsStopped)
                    add += Format.Bold(GetText("queue_stopped", Format.Code(Prefix + "play"))) + "\n";
                 // var mps = mp.MaxPlaytimeSeconds;
                 // if (mps > 0)
                 //     add += Format.Bold(GetText("song_skips_after", TimeSpan.FromSeconds(mps).ToString("HH\\:mm\\:ss"))) + "\n";
                 if (repeatType == PlayerRepeatType.Track)
                 {
                     add += "🔂 " + GetText("repeating_track") + "\n";
                 }
                 else
                 {
                     // if (mp.Autoplay)
                     //     add += "↪ " + GetText("autoplaying") + "\n";
                     // if (mp.FairPlay && !mp.Autoplay)
                     //     add += " " + GetText("fairplay") + "\n";
                     if (repeatType == PlayerRepeatType.Queue)
                         add += "🔁 " + GetText("repeating_queue") + "\n";
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

                var embed = new EmbedBuilder()
                    .WithAuthor(eab => eab
                        .WithName(GetText("player_queue", curPage + 1, (tracks.Count / LQ_ITEMS_PER_PAGE) + 1))
                        .WithMusicIcon())
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
        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task QueueSearch([Leftover] string query)
        {
            _ = ctx.Channel.TriggerTypingAsync();

            var videos = await _service.SearchVideosAsync(query);

            if (videos is null || videos.Count == 0)
            {
                await ReplyErrorLocalizedAsync("song_not_found").ConfigureAwait(false);
                return;
            }

            var resultsString = videos
                .Select((x, i) => $"`{i + 1}.`\n\t{Format.Bold(x.Title)}\n\t{x.Url}")
                .JoinWith('\n');
            
            var msg = await ctx.Channel.SendConfirmAsync(resultsString);

            try
            {
                var input = await GetUserInputAsync(ctx.User.Id, ctx.Channel.Id).ConfigureAwait(false);
                if (input == null
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

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [Priority(1)]
        public async Task TrackRemove(int index)
        {
            if (index < 1)
            {
                await ReplyErrorLocalizedAsync("removed_song_error").ConfigureAwait(false);
                return;
            }
            
            var valid = await ValidateAsync();
            if (!valid)
                return;

            if (!_service.TryGetMusicPlayer(ctx.Guild.Id, out var mp))
            {
                await ReplyErrorLocalizedAsync("no_player");
                return;
            }
            
            if (!mp.TryRemoveTrackAt(index - 1, out var song))
            {
                await ReplyErrorLocalizedAsync("removed_song_error").ConfigureAwait(false);
                return;
            }
            
            var embed = new EmbedBuilder()
                .WithAuthor(eab => eab.WithName(GetText("removed_song") + " #" + (index)).WithMusicIcon())
                .WithDescription(song.PrettyName())
                .WithFooter(ef => ef.WithText(song.PrettyInfo()))
                .WithErrorColor();

            await _service.SendToOutputAsync(Context.Guild.Id, embed);
        }

         public enum All { All = -1 }
         [NadekoCommand, Usage, Description, Aliases]
         [RequireContext(ContextType.Guild)]
         [Priority(0)]
         public async Task TrackRemove(All _ = All.All)
         {
             var valid = await ValidateAsync();
             if (!valid)
                 return;

             if (!_service.TryGetMusicPlayer(ctx.Guild.Id, out var mp))
             {
                 await ReplyErrorLocalizedAsync("no_player");
                 return;
             }
             
             mp.Clear();
             await ReplyConfirmLocalizedAsync("queue_cleared").ConfigureAwait(false);
         }
         
         [NadekoCommand, Usage, Description, Aliases]
         [RequireContext(ContextType.Guild)]
         public async Task Stop()
         {
             var valid = await ValidateAsync();
             if (!valid)
                 return;

             if (!_service.TryGetMusicPlayer(ctx.Guild.Id, out var mp))
             {
                 await ReplyErrorLocalizedAsync("no_player");
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
         
         [NadekoCommand, Usage, Description, Aliases]
         [RequireContext(ContextType.Guild)]
         public async Task QueueRepeat(InputRepeatType type = InputRepeatType.Queue)
         {
             var valid = await ValidateAsync();
             if (!valid)
                 return;
             
             await _service.SetRepeatAsync(ctx.Guild.Id, InputToDbType(type));

             if (type == InputRepeatType.None)
                 await ReplyConfirmLocalizedAsync("repeating_none");
             else if (type == InputRepeatType.Queue)
                 await ReplyConfirmLocalizedAsync("repeating_queue");
             else
                 await ReplyConfirmLocalizedAsync("repeating_track");
         }
         
         [NadekoCommand, Usage, Description, Aliases]
         [RequireContext(ContextType.Guild)]
         public async Task ReptCurSong()
         {
             await ReplyPendingLocalizedAsync("obsolete_use", $"`{Prefix}qrp song`");
             await QueueRepeat(InputRepeatType.Song);
         }
         
         [NadekoCommand, Usage, Description, Aliases]
         [RequireContext(ContextType.Guild)]
         public async Task Pause()
         {
             var valid = await ValidateAsync();
             if (!valid)
                 return;

             if (!_service.TryGetMusicPlayer(ctx.Guild.Id, out var mp) || mp.GetCurrentTrack(out _) is null)
             {
                 await ReplyErrorLocalizedAsync("no_player");
                 return;
             }

             mp.TogglePause();
         }
         
         [NadekoCommand, Usage, Description, Aliases]
         [RequireContext(ContextType.Guild)]
         public Task Radio(string radioLink)
             => QueueByQuery(radioLink, false, MusicPlatform.Radio);

         [NadekoCommand, Usage, Description, Aliases]
         [RequireContext(ContextType.Guild)]
         [OwnerOnly]
         public Task Local([Leftover] string path)
             => QueueByQuery(path, false, MusicPlatform.Local);

         [NadekoCommand, Usage, Description, Aliases]
         [RequireContext(ContextType.Guild)]
         [OwnerOnly]
         public async Task LocalPlaylist([Leftover] string dirPath)
         {
             if (string.IsNullOrWhiteSpace(dirPath))
                 return;

             var user = (IGuildUser) Context.User;
             var voiceChannelId = user.VoiceChannel?.Id;
        
             if (voiceChannelId is null)
             {
                 await ReplyErrorLocalizedAsync("must_be_in_voice");
                 return;
             }

             _ = ctx.Channel.TriggerTypingAsync();
        
             var botUser = await ctx.Guild.GetCurrentUserAsync();
             await EnsureBotInVoiceChannelAsync(voiceChannelId!.Value, botUser);
        
             if (botUser.VoiceChannel?.Id != voiceChannelId)
             {
                 await ReplyErrorLocalizedAsync("not_with_bot_in_voice");
                 return;
             }
            
             var mp = await _service.GetOrCreateMusicPlayerAsync((ITextChannel) Context.Channel);
             if (mp is null)
             {
                 await ReplyErrorLocalizedAsync("no_player");
                 return;
             }
             
             await _service.EnqueueDirectoryAsync(mp, dirPath, ctx.User.ToString());
             
             await ReplyConfirmLocalizedAsync("dir_queue_complete").ConfigureAwait(false);
         }
         
         [NadekoCommand, Usage, Description, Aliases]
         [RequireContext(ContextType.Guild)]
         public async Task MoveSong(int from, int to)
         {
             if (--from < 0 || --to < 0 || from == to)
             {
                 await ReplyErrorLocalizedAsync("invalid_input").ConfigureAwait(false);
                 return;
             }

             var valid = await ValidateAsync();
             if (!valid)
                 return;
             
             var mp = await _service.GetOrCreateMusicPlayerAsync((ITextChannel) Context.Channel);
             if (mp is null)
             {
                 await ReplyErrorLocalizedAsync("no_player");
                 return;
             }

             var track = mp.MoveTrack(from, to);
             if (track is null)
             {
                 await ReplyErrorLocalizedAsync("invalid_input").ConfigureAwait(false);
                 return;
             }
             
             var embed = new EmbedBuilder()
                 .WithTitle(track.Title.TrimTo(65))
                 .WithAuthor(eab => eab.WithName(GetText("song_moved")).WithIconUrl("https://cdn.discordapp.com/attachments/155726317222887425/258605269972549642/music1.png"))
                 .AddField(fb => fb.WithName(GetText("from_position")).WithValue($"#{from + 1}").WithIsInline(true))
                 .AddField(fb => fb.WithName(GetText("to_position")).WithValue($"#{to + 1}").WithIsInline(true))
                 .WithColor(NadekoBot.OkColor);

             if (Uri.IsWellFormedUriString(track.Url, UriKind.Absolute))
                 embed.WithUrl(track.Url);

             await ctx.Channel.EmbedAsync(embed).ConfigureAwait(false);
         }

         [NadekoCommand, Usage, Description, Aliases]
         [RequireContext(ContextType.Guild)]
         public Task SoundCloudQueue([Leftover] string query)
             => QueueByQuery(query, false, MusicPlatform.SoundCloud);
         
         [NadekoCommand, Usage, Description, Aliases]
         [RequireContext(ContextType.Guild)]
         public async Task SoundCloudPl([Leftover] string playlist)
         {
             if (string.IsNullOrWhiteSpace(playlist))
                 return;

             var succ = await QueuePreconditionInternalAsync();
             if (!succ)
                 return;

             var mp = await _service.GetOrCreateMusicPlayerAsync((ITextChannel) Context.Channel);
             if (mp is null)
             {
                 await ReplyErrorLocalizedAsync("no_player");
                 return;
             }
             
             _ = ctx.Channel.TriggerTypingAsync();

             await _service.EnqueueSoundcloudPlaylistAsync(mp, playlist, ctx.User.ToString());

             await ctx.OkAsync();
         }

         [NadekoCommand, Usage, Description, Aliases]
         [RequireContext(ContextType.Guild)]
         public async Task Playlist([Leftover] string playlistQuery)
         {
             if (string.IsNullOrWhiteSpace(playlistQuery))
                 return;

             var succ = await QueuePreconditionInternalAsync();
             if (!succ)
                 return;

             var mp = await _service.GetOrCreateMusicPlayerAsync((ITextChannel) Context.Channel);
             if (mp is null)
             {
                 await ReplyErrorLocalizedAsync("no_player");
                 return;
             }

             _ = Context.Channel.TriggerTypingAsync();


             var queuedCount = await _service.EnqueueYoutubePlaylistAsync(mp, playlistQuery, ctx.User.ToString());
             if (queuedCount == 0)
             {
                 await ReplyErrorLocalizedAsync("no_search_results").ConfigureAwait(false);
                 return;
             }
             await ctx.OkAsync();
         }

         [NadekoCommand, Usage, Description, Aliases]
         [RequireContext(ContextType.Guild)]
         public async Task NowPlaying()
         {
             var mp = await _service.GetOrCreateMusicPlayerAsync((ITextChannel) Context.Channel);
             if (mp is null)
             {
                 await ReplyErrorLocalizedAsync("no_player");
                 return;
             }

             var currentTrack = mp.GetCurrentTrack(out _);
             if (currentTrack == null)
                 return;

             var embed = new EmbedBuilder().WithOkColor()
                 .WithAuthor(eab => eab.WithName(GetText("now_playing")).WithMusicIcon())
                 .WithDescription(currentTrack.PrettyName())
                 .WithThumbnailUrl(currentTrack.Thumbnail)
                 .WithFooter($"{mp.PrettyVolume()} | {mp.PrettyTotalTime()} | {currentTrack.Platform} | {currentTrack.Queuer}");

             await ctx.Channel.EmbedAsync(embed).ConfigureAwait(false);
         }

         [NadekoCommand, Usage, Description, Aliases]
         [RequireContext(ContextType.Guild)]
         public async Task PlaylistShuffle()
         {
             var valid = await ValidateAsync();
             if (!valid)
                 return;
             
             var mp = await _service.GetOrCreateMusicPlayerAsync((ITextChannel) Context.Channel);
             if (mp is null)
             {
                 await ReplyErrorLocalizedAsync("no_player");
                 return;
             }
             
             mp.ShuffleQueue();
             await ReplyConfirmLocalizedAsync("queue_shuffled");
         }

         [NadekoCommand, Usage, Description, Aliases]
         [RequireContext(ContextType.Guild)]
         [UserPerm(GuildPerm.ManageMessages)]
         public async Task SetMusicChannel()
         {
             await _service.SetMusicChannelAsync(ctx.Guild.Id, ctx.Channel.Id);

             await ReplyConfirmLocalizedAsync("set_music_channel");
         }
         
         [NadekoCommand, Usage, Description, Aliases]
         [RequireContext(ContextType.Guild)]
         [UserPerm(GuildPerm.ManageMessages)]
         public async Task UnsetMusicChannel()
         {
             await _service.SetMusicChannelAsync(ctx.Guild.Id, null);

             await ReplyConfirmLocalizedAsync("unset_music_channel");
         }

         [NadekoCommand, Usage, Description, Aliases]
         [RequireContext(ContextType.Guild)]
         public async Task AutoDisconnect()
         {
             var newState = await _service.ToggleAutoDisconnectAsync(ctx.Guild.Id);

             if(newState)
                await ReplyConfirmLocalizedAsync("autodc_enable");
             else
                await ReplyConfirmLocalizedAsync("autodc_disable");
         }

         [NadekoCommand, Usage, Description, Aliases]
         [RequireContext(ContextType.Guild)]
         [RequireUserPermission(GuildPermission.Administrator)]
         public async Task MusicQuality()
         {
             var quality = await _service.GetMusicQualityAsync(ctx.Guild.Id);
             await ReplyConfirmLocalizedAsync("current_music_quality", Format.Bold(quality.ToString()));
         }
         
         [NadekoCommand, Usage, Description, Aliases]
         [RequireContext(ContextType.Guild)]
         [RequireUserPermission(GuildPermission.Administrator)]
         public async Task MusicQuality(QualityPreset preset)
         {
             await _service.SetMusicQualityAsync(ctx.Guild.Id, preset);
             await ReplyConfirmLocalizedAsync("music_quality_set", Format.Bold(preset.ToString()));
         }
    }
}