using NadekoBot.Db.Models;

namespace NadekoBot.Modules.Utility;

public interface IGuildColorsService
{
    
}

public sealed class GuildColorsService : IGuildColorsService, INService
{
    private readonly DbService _db;

    public GuildColorsService(DbService db)
    {
        _db = db;
    }

    public async Task<GuildColors?> GetGuildColors(ulong guildId)
    {
        // get from database and cache it with linq2db

        await using var ctx = _db.GetDbContext();

        return null;
        // return await ctx
        //     .GuildColors
        //     .FirstOrDefaultAsync(x => x.GuildId == guildId);

    }
}

public partial class Utility
{
    public class GuildColorsCommands : NadekoModule<IGuildColorsService>
    {
        
    }
}