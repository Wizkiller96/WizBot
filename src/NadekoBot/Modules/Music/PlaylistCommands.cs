#nullable disable
using NadekoBot.Db;
using NadekoBot.Modules.Music.Services;
using NadekoBot.Services.Database.Models;

namespace NadekoBot.Modules.Music;

public sealed partial class Music
{
    [Group]
    public sealed partial class PlaylistCommands : NadekoModule<IMusicService>
    {
        private static readonly SemaphoreSlim _playlistLock = new(1, 1);
        private readonly DbService _db;
        private readonly IBotCredentials _creds;

        public PlaylistCommands(DbService db, IBotCredentials creds)
        {
            _db = db;
            _creds = creds;
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

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async partial Task Playlists([Leftover] int num = 1)
        {
            if (num <= 0)
                return;

            List<MusicPlaylist> playlists;

            await using (var uow = _db.GetDbContext())
            {
                playlists = uow.MusicPlaylists.GetPlaylistsOnPage(num);
            }

            var embed = _eb.Create(ctx)
                           .WithAuthor(GetText(strs.playlists_page(num)), MUSIC_ICON_URL)
                           .WithDescription(string.Join("\n",
                               playlists.Select(r => GetText(strs.playlists(r.Id, r.Name, r.Author, r.Songs.Count)))))
                           .WithOkColor();

            await ctx.Channel.EmbedAsync(embed);
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async partial Task DeletePlaylist([Leftover] int id)
        {
            var success = false;
            try
            {
                await using var uow = _db.GetDbContext();
                var pl = uow.MusicPlaylists.FirstOrDefault(x => x.Id == id);

                if (pl is not null)
                {
                    if (_creds.IsOwner(ctx.User) || pl.AuthorId == ctx.User.Id)
                    {
                        uow.MusicPlaylists.Remove(pl);
                        await uow.SaveChangesAsync();
                        success = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error deleting playlist");
            }

            if (!success)
                await ReplyErrorLocalizedAsync(strs.playlist_delete_fail);
            else
                await ReplyConfirmLocalizedAsync(strs.playlist_deleted);
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async partial Task PlaylistShow(int id, int page = 1)
        {
            if (page-- < 1)
                return;

            MusicPlaylist mpl;
            await using (var uow = _db.GetDbContext())
            {
                mpl = uow.MusicPlaylists.GetWithSongs(id);
            }

            await ctx.SendPaginatedConfirmAsync(page,
                cur =>
                {
                    var i = 0;
                    var str = string.Join("\n",
                        mpl.Songs.Skip(cur * 20)
                           .Take(20)
                           .Select(x => $"`{++i}.` [{x.Title.TrimTo(45)}]({x.Query}) `{x.Provider}`"));
                    return _eb.Create().WithTitle($"\"{mpl.Name}\" by {mpl.Author}").WithOkColor().WithDescription(str);
                },
                mpl.Songs.Count,
                20);
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async partial Task Save([Leftover] string name)
        {
            if (!_service.TryGetMusicPlayer(ctx.Guild.Id, out var mp))
            {
                await ReplyErrorLocalizedAsync(strs.no_player);
                return;
            }

            var songs = mp.GetQueuedTracks()
                          .Select(s => new PlaylistSong
                          {
                              Provider = s.Platform.ToString(),
                              ProviderType = (MusicType)s.Platform,
                              Title = s.Title,
                              Query = s.Platform == MusicPlatform.Local ? s.GetStreamUrl().Result!.Trim('"') : s.Url
                          })
                          .ToList();

            MusicPlaylist playlist;
            await using (var uow = _db.GetDbContext())
            {
                playlist = new()
                {
                    Name = name,
                    Author = ctx.User.Username,
                    AuthorId = ctx.User.Id,
                    Songs = songs.ToList()
                };
                uow.MusicPlaylists.Add(playlist);
                await uow.SaveChangesAsync();
            }

            await ctx.Channel.EmbedAsync(_eb.Create()
                                            .WithOkColor()
                                            .WithTitle(GetText(strs.playlist_saved))
                                            .AddField(GetText(strs.name), name)
                                            .AddField(GetText(strs.id), playlist.Id.ToString()));
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async partial Task Load([Leftover] int id)
        {
            // expensive action, 1 at a time
            await _playlistLock.WaitAsync();
            try
            {
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

                MusicPlaylist mpl;
                await using (var uow = _db.GetDbContext())
                {
                    mpl = uow.MusicPlaylists.GetWithSongs(id);
                }

                if (mpl is null)
                {
                    await ReplyErrorLocalizedAsync(strs.playlist_id_not_found);
                    return;
                }

                IUserMessage msg = null;
                try
                {
                    msg = await ctx.Channel.SendMessageAsync(
                        GetText(strs.attempting_to_queue(Format.Bold(mpl.Songs.Count.ToString()))));
                }
                catch (Exception)
                {
                }

                await mp.EnqueueManyAsync(mpl.Songs.Select(x => (x.Query, (MusicPlatform)x.ProviderType)),
                    ctx.User.ToString());

                if (msg is not null)
                    await msg.ModifyAsync(m => m.Content = GetText(strs.playlist_queue_complete));
            }
            finally
            {
                _playlistLock.Release();
            }
        }
    }
}