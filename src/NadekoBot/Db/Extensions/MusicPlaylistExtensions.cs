using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Services.Database.Models;

namespace NadekoBot.Db
{
    public static class MusicPlaylistExtensions
    {
        public static List<MusicPlaylist> GetPlaylistsOnPage(this DbSet<MusicPlaylist> playlists, int num)
        {
            if (num < 1)
                throw new IndexOutOfRangeException();

            return playlists
                .AsQueryable()
                .Skip((num - 1) * 20)
                .Take(20)
                .Include(pl => pl.Songs)
                .ToList();
        }

        public static MusicPlaylist GetWithSongs(this DbSet<MusicPlaylist> playlists, int id) => 
            playlists
                .Include(mpl => mpl.Songs)
                .FirstOrDefault(mpl => mpl.Id == id);
    }
}
