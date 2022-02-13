#nullable disable
using NadekoBot.Modules.Music.Services;
using NadekoBot.Services.Database.Models;

namespace NadekoBot.Modules.Music;

[NoPublicBot]
public sealed partial class Music : NadekoModule<IMusicService>
{
    public enum All { All = -1 }

    public enum InputRepeatType
    {
        N = 0, No = 0, None = 0,
        T = 1, Track = 1, S = 1, Song = 1,
        Q = 2, Queue = 2, Playlist = 2, Pl = 2
    }

    public const string MUSIC_ICON_URL = "http://i.imgur.com/nhKS3PT.png";

    private const int LQ_ITEMS_PER_PAGE = 9;

    private static readonly SemaphoreSlim _voiceChannelLock = new(1, 1);
    private readonly ILogCommandService _logService;

    public Music(ILogCommandService logService)
        => _logService = logService;

    private async Task<bool> ValidateAsync()
    {
        var user = (IGuildUser)ctx.User;
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

    private async Task EnsureBotInVoiceChannelAsync(ulong voiceChannelId, IGuildUser botUser = null)
    {
        botUser ??= await ctx.Guild.GetCurrentUserAsync();
        await _voiceChannelLock.WaitAsync();
        try
        {
            if (botUser.VoiceChannel?.Id is null || !_service.TryGetMusicPlayer(ctx.Guild.Id, out _))
                await _service.JoinVoiceChannelAsync(ctx.Guild.Id, voiceChannelId);
        }
        finally
        {
            _voiceChannelLock.Release();
        }
    }

    private async Task<bool> QueuePreconditionInternalAsync()
    {
        var user = (IGuildUser)ctx.User;
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

        var mp = await _service.GetOrCreateMusicPlayerAsync((ITextChannel)ctx.Channel);
        if (mp is null)
        {
            await ReplyErrorLocalizedAsync(strs.no_player);
            return;
        }

        var (trackInfo, index) = await mp.TryEnqueueTrackAsync(query, ctx.User.ToString(), asNext, forcePlatform);
        if (trackInfo is null)
        {
            await ReplyErrorLocalizedAsync(strs.track_not_found);
            return;
        }

        try
        {
            var embed = _eb.Create()
                           .WithOkColor()
                           .WithAuthor(GetText(strs.queued_track) + " #" + (index + 1), MUSIC_ICON_URL)
                           .WithDescription($"{trackInfo.PrettyName()}\n{GetText(strs.queue)} ")
                           .WithFooter(trackInfo.Platform.ToString());

            if (!string.IsNullOrWhiteSpace(trackInfo.Thumbnail))
                embed.WithThumbnailUrl(trackInfo.Thumbnail);

            var queuedMessage = await _service.SendToOutputAsync(ctx.Guild.Id, embed);
            queuedMessage?.DeleteAfter(10, _logService);
            if (mp.IsStopped)
            {
                var msg = await ReplyPendingLocalizedAsync(strs.queue_stopped(Format.Code(prefix + "play")));
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

        var mp = await _service.GetOrCreateMusicPlayerAsync((ITextChannel)ctx.Channel);
        if (mp is null)
        {
            await ReplyErrorLocalizedAsync(strs.no_player);
            return;
        }

        mp.MoveTo(index);
    }

    // join vc
    [Cmd]
    [RequireContext(ContextType.Guild)]
    public async partial Task Join()
    {
        var user = (IGuildUser)ctx.User;

        var voiceChannelId = user.VoiceChannel?.Id;

        if (voiceChannelId is null)
        {
            await ReplyErrorLocalizedAsync(strs.must_be_in_voice);
            return;
        }

        await _service.JoinVoiceChannelAsync(user.GuildId, voiceChannelId.Value);
    }

    // leave vc (destroy)
    [Cmd]
    [RequireContext(ContextType.Guild)]
    public async partial Task Destroy()
    {
        var valid = await ValidateAsync();
        if (!valid)
            return;

        await _service.LeaveVoiceChannelAsync(ctx.Guild.Id);
    }

    // play - no args = next
    [Cmd]
    [RequireContext(ContextType.Guild)]
    [Priority(2)]
    public partial Task Play()
        => Next();

    // play - index = skip to that index
    [Cmd]
    [RequireContext(ContextType.Guild)]
    [Priority(1)]
    public partial Task Play(int index)
        => MoveToIndex(index);

    // play - query = q(query)
    [Cmd]
    [RequireContext(ContextType.Guild)]
    [Priority(0)]
    public partial Task Play([Leftover] string query)
        => QueueByQuery(query);

    [Cmd]
    [RequireContext(ContextType.Guild)]
    public partial Task Queue([Leftover] string query)
        => QueueByQuery(query);

    [Cmd]
    [RequireContext(ContextType.Guild)]
    public partial Task QueueNext([Leftover] string query)
        => QueueByQuery(query, true);

    [Cmd]
    [RequireContext(ContextType.Guild)]
    public async partial Task Volume(int vol)
    {
        if (vol is < 0 or > 100)
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

    [Cmd]
    [RequireContext(ContextType.Guild)]
    public async partial Task Next()
    {
        var valid = await ValidateAsync();
        if (!valid)
            return;

        var success = await _service.PlayAsync(ctx.Guild.Id, ((IGuildUser)ctx.User).VoiceChannel.Id);
        if (!success)
            await ReplyErrorLocalizedAsync(strs.no_player);
    }

    // list queue, relevant page
    [Cmd]
    [RequireContext(ContextType.Guild)]
    public async partial Task ListQueue()
    {
        // show page with the current track
        if (!_service.TryGetMusicPlayer(ctx.Guild.Id, out var mp))
        {
            await ReplyErrorLocalizedAsync(strs.no_player);
            return;
        }

        await ListQueue((mp.CurrentIndex / LQ_ITEMS_PER_PAGE) + 1);
    }

    // list queue, specify page
    [Cmd]
    [RequireContext(ContextType.Guild)]
    public async partial Task ListQueue(int page)
    {
        if (--page < 0)
            return;

        IReadOnlyCollection<IQueuedTrackInfo> tracks;
        if (!_service.TryGetMusicPlayer(ctx.Guild.Id, out var mp) || (tracks = mp.GetQueuedTracks()).Count == 0)
        {
            await ReplyErrorLocalizedAsync(strs.no_player);
            return;
        }

        IEmbedBuilder PrintAction(int curPage)
        {
            var desc = string.Empty;
            var current = mp.GetCurrentTrack(out var currentIndex);
            if (current is not null)
                desc = $"`🔊` {current.PrettyFullName()}\n\n" + desc;

            var repeatType = mp.Repeat;
            var add = string.Empty;
            if (mp.IsStopped)
                add += Format.Bold(GetText(strs.queue_stopped(Format.Code(prefix + "play")))) + "\n";
            // var mps = mp.MaxPlaytimeSeconds;
            // if (mps > 0)
            //     add += Format.Bold(GetText(strs.song_skips_after(TimeSpan.FromSeconds(mps).ToString("HH\\:mm\\:ss")))) + "\n";
            if (repeatType == PlayerRepeatType.Track)
                add += "🔂 " + GetText(strs.repeating_track) + "\n";
            else
            {
                if (mp.AutoPlay)
                    add += "↪ " + GetText(strs.autoplaying) + "\n";
                // if (mp.FairPlay && !mp.Autoplay)
                //     add += " " + GetText(strs.fairplay) + "\n";
                if (repeatType == PlayerRepeatType.Queue)
                    add += "🔁 " + GetText(strs.repeating_queue) + "\n";
            }


            desc += tracks.Skip(LQ_ITEMS_PER_PAGE * curPage)
                          .Take(LQ_ITEMS_PER_PAGE)
                          .Select((v, index) =>
                          {
                              index += LQ_ITEMS_PER_PAGE * curPage;
                              if (index == currentIndex)
                                  return $"**⇒**`{index + 1}.` {v.PrettyFullName()}";

                              return $"`{index + 1}.` {v.PrettyFullName()}";
                          })
                          .Join('\n');

            if (!string.IsNullOrWhiteSpace(add))
                desc = add + "\n" + desc;

            var embed = _eb.Create()
                           .WithAuthor(GetText(strs.player_queue(curPage + 1, (tracks.Count / LQ_ITEMS_PER_PAGE) + 1)),
                               MUSIC_ICON_URL)
                           .WithDescription(desc)
                           .WithFooter($"  {mp.PrettyVolume()}  |  🎶 {tracks.Count}  |  ⌛ {mp.PrettyTotalTime()}  ")
                           .WithOkColor();

            return embed;
        }

        await ctx.SendPaginatedConfirmAsync(page, PrintAction, tracks.Count, LQ_ITEMS_PER_PAGE, false);
    }

    // search
    [Cmd]
    [RequireContext(ContextType.Guild)]
    public async partial Task QueueSearch([Leftover] string query)
    {
        _ = ctx.Channel.TriggerTypingAsync();

        var videos = await _service.SearchVideosAsync(query);

        if (videos.Count == 0)
        {
            await ReplyErrorLocalizedAsync(strs.track_not_found);
            return;
        }

        var resultsString = videos.Select((x, i) => $"`{i + 1}.`\n\t{Format.Bold(x.Title)}\n\t{x.Url}").Join('\n');

        var msg = await SendConfirmAsync(resultsString);

        try
        {
            var input = await GetUserInputAsync(ctx.User.Id, ctx.Channel.Id);
            if (input is null || !int.TryParse(input, out var index) || (index -= 1) < 0 || index >= videos.Count)
            {
                _logService.AddDeleteIgnore(msg.Id);
                try
                {
                    await msg.DeleteAsync();
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
                await msg.DeleteAsync();
            }
            catch
            {
            }
        }
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    [Priority(1)]
    public async partial Task TrackRemove(int index)
    {
        if (index < 1)
        {
            await ReplyErrorLocalizedAsync(strs.removed_track_error);
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

        if (!mp.TryRemoveTrackAt(index - 1, out var track))
        {
            await ReplyErrorLocalizedAsync(strs.removed_track_error);
            return;
        }

        var embed = _eb.Create()
                       .WithAuthor(GetText(strs.removed_track) + " #" + index, MUSIC_ICON_URL)
                       .WithDescription(track.PrettyName())
                       .WithFooter(track.PrettyInfo())
                       .WithErrorColor();

        await _service.SendToOutputAsync(ctx.Guild.Id, embed);
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    [Priority(0)]
    public async partial Task TrackRemove(All _ = All.All)
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
        await ReplyConfirmLocalizedAsync(strs.queue_cleared);
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    public async partial Task Stop()
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

    private PlayerRepeatType InputToDbType(InputRepeatType type)
        => type switch
        {
            InputRepeatType.None => PlayerRepeatType.None,
            InputRepeatType.Queue => PlayerRepeatType.Queue,
            InputRepeatType.Track => PlayerRepeatType.Track,
            _ => PlayerRepeatType.Queue
        };

    [Cmd]
    [RequireContext(ContextType.Guild)]
    public async partial Task QueueRepeat(InputRepeatType type = InputRepeatType.Queue)
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

    [Cmd]
    [RequireContext(ContextType.Guild)]
    public async partial Task Pause()
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

    [Cmd]
    [RequireContext(ContextType.Guild)]
    public partial Task Radio(string radioLink)
        => QueueByQuery(radioLink, false, MusicPlatform.Radio);

    [Cmd]
    [RequireContext(ContextType.Guild)]
    [OwnerOnly]
    public partial Task Local([Leftover] string path)
        => QueueByQuery(path, false, MusicPlatform.Local);

    [Cmd]
    [RequireContext(ContextType.Guild)]
    [OwnerOnly]
    public async partial Task LocalPlaylist([Leftover] string dirPath)
    {
        if (string.IsNullOrWhiteSpace(dirPath))
            return;

        var user = (IGuildUser)ctx.User;
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

        var mp = await _service.GetOrCreateMusicPlayerAsync((ITextChannel)ctx.Channel);
        if (mp is null)
        {
            await ReplyErrorLocalizedAsync(strs.no_player);
            return;
        }

        await _service.EnqueueDirectoryAsync(mp, dirPath, ctx.User.ToString());

        await ReplyConfirmLocalizedAsync(strs.dir_queue_complete);
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    public async partial Task TrackMove(int from, int to)
    {
        if (--from < 0 || --to < 0 || from == to)
        {
            await ReplyErrorLocalizedAsync(strs.invalid_input);
            return;
        }

        var valid = await ValidateAsync();
        if (!valid)
            return;

        var mp = await _service.GetOrCreateMusicPlayerAsync((ITextChannel)ctx.Channel);
        if (mp is null)
        {
            await ReplyErrorLocalizedAsync(strs.no_player);
            return;
        }

        var track = mp.MoveTrack(from, to);
        if (track is null)
        {
            await ReplyErrorLocalizedAsync(strs.invalid_input);
            return;
        }

        var embed = _eb.Create()
                       .WithTitle(track.Title.TrimTo(65))
                       .WithAuthor(GetText(strs.track_moved), MUSIC_ICON_URL)
                       .AddField(GetText(strs.from_position), $"#{from + 1}", true)
                       .AddField(GetText(strs.to_position), $"#{to + 1}", true)
                       .WithOkColor();

        if (Uri.IsWellFormedUriString(track.Url, UriKind.Absolute))
            embed.WithUrl(track.Url);

        await ctx.Channel.EmbedAsync(embed);
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    public partial Task SoundCloudQueue([Leftover] string query)
        => QueueByQuery(query, false, MusicPlatform.SoundCloud);

    [Cmd]
    [RequireContext(ContextType.Guild)]
    public async partial Task SoundCloudPl([Leftover] string playlist)
    {
        if (string.IsNullOrWhiteSpace(playlist))
            return;

        var succ = await QueuePreconditionInternalAsync();
        if (!succ)
            return;

        var mp = await _service.GetOrCreateMusicPlayerAsync((ITextChannel)ctx.Channel);
        if (mp is null)
        {
            await ReplyErrorLocalizedAsync(strs.no_player);
            return;
        }

        _ = ctx.Channel.TriggerTypingAsync();

        await _service.EnqueueSoundcloudPlaylistAsync(mp, playlist, ctx.User.ToString());

        await ctx.OkAsync();
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    public async partial Task Playlist([Leftover] string playlistQuery)
    {
        if (string.IsNullOrWhiteSpace(playlistQuery))
            return;

        var succ = await QueuePreconditionInternalAsync();
        if (!succ)
            return;

        var mp = await _service.GetOrCreateMusicPlayerAsync((ITextChannel)ctx.Channel);
        if (mp is null)
        {
            await ReplyErrorLocalizedAsync(strs.no_player);
            return;
        }

        _ = ctx.Channel.TriggerTypingAsync();


        var queuedCount = await _service.EnqueueYoutubePlaylistAsync(mp, playlistQuery, ctx.User.ToString());
        if (queuedCount == 0)
        {
            await ReplyErrorLocalizedAsync(strs.no_search_results);
            return;
        }

        await ctx.OkAsync();
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    public async partial Task NowPlaying()
    {
        var mp = await _service.GetOrCreateMusicPlayerAsync((ITextChannel)ctx.Channel);
        if (mp is null)
        {
            await ReplyErrorLocalizedAsync(strs.no_player);
            return;
        }

        var currentTrack = mp.GetCurrentTrack(out _);
        if (currentTrack is null)
            return;

        var embed = _eb.Create()
                       .WithOkColor()
                       .WithAuthor(GetText(strs.now_playing), MUSIC_ICON_URL)
                       .WithDescription(currentTrack.PrettyName())
                       .WithThumbnailUrl(currentTrack.Thumbnail)
                       .WithFooter(
                           $"{mp.PrettyVolume()} | {mp.PrettyTotalTime()} | {currentTrack.Platform} | {currentTrack.Queuer}");

        await ctx.Channel.EmbedAsync(embed);
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    public async partial Task PlaylistShuffle()
    {
        var valid = await ValidateAsync();
        if (!valid)
            return;

        var mp = await _service.GetOrCreateMusicPlayerAsync((ITextChannel)ctx.Channel);
        if (mp is null)
        {
            await ReplyErrorLocalizedAsync(strs.no_player);
            return;
        }

        mp.ShuffleQueue();
        await ReplyConfirmLocalizedAsync(strs.queue_shuffled);
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    [UserPerm(GuildPerm.ManageMessages)]
    public async partial Task SetMusicChannel()
    {
        await _service.SetMusicChannelAsync(ctx.Guild.Id, ctx.Channel.Id);

        await ReplyConfirmLocalizedAsync(strs.set_music_channel);
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    [UserPerm(GuildPerm.ManageMessages)]
    public async partial Task UnsetMusicChannel()
    {
        await _service.SetMusicChannelAsync(ctx.Guild.Id, null);

        await ReplyConfirmLocalizedAsync(strs.unset_music_channel);
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    public async partial Task AutoDisconnect()
    {
        var newState = await _service.ToggleAutoDisconnectAsync(ctx.Guild.Id);

        if (newState)
            await ReplyConfirmLocalizedAsync(strs.autodc_enable);
        else
            await ReplyConfirmLocalizedAsync(strs.autodc_disable);
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    [UserPerm(GuildPerm.Administrator)]
    public async partial Task MusicQuality()
    {
        var quality = await _service.GetMusicQualityAsync(ctx.Guild.Id);
        await ReplyConfirmLocalizedAsync(strs.current_music_quality(Format.Bold(quality.ToString())));
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    [UserPerm(GuildPerm.Administrator)]
    public async partial Task MusicQuality(QualityPreset preset)
    {
        await _service.SetMusicQualityAsync(ctx.Guild.Id, preset);
        await ReplyConfirmLocalizedAsync(strs.music_quality_set(Format.Bold(preset.ToString())));
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    public async partial Task QueueAutoPlay()
    {
        var newValue = await _service.ToggleQueueAutoPlayAsync(ctx.Guild.Id);
        if (newValue)
            await ReplyConfirmLocalizedAsync(strs.music_autoplay_on);
        else
            await ReplyConfirmLocalizedAsync(strs.music_autoplay_off);
    }
}