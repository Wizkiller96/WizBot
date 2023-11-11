using LinqToDB;
using Nadeko.Bot.Db.Models;

namespace NadekoBot.Modules.Music;

public sealed partial class Music
{
    public class CleanupCommands : NadekoModule
    {
        private readonly DbService _db;

        public CleanupCommands(DbService db)
        {
            _db = db;
        }
    
        public async Task DeletePlaylists()
        {
            await using var uow = _db.GetDbContext();
            await uow.Set<MusicPlaylist>().DeleteAsync();
            await uow.SaveChangesAsync();
        }
    }
}