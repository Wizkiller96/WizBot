#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Db.Models;
using NadekoBot.Services.Database.Models;

namespace NadekoBot.Modules.Administration.Services;

public class DangerousCommandsService : INService
{
    private readonly DbService _db;

    public DangerousCommandsService(DbService db)
        => _db = db;

    public async Task DeleteXp()
    {
        await using var ctx = _db.GetDbContext();
        await ctx.DiscordUser.UpdateAsync(_ => new DiscordUser()
        {
            ClubId = null,
            // IsClubAdmin = false,
            TotalXp = 0
        });
        await ctx.ClubApplicants.DeleteAsync();
        await ctx.ClubBans.DeleteAsync();
        await ctx.Clubs.DeleteAsync();
        await ctx.SaveChangesAsync();
    }
    
    public async Task DeleteWaifus()
    {
        await using var ctx = _db.GetDbContext();
        await ctx.WaifuUpdates.DeleteAsync();
        await ctx.WaifuItem.DeleteAsync();
        await ctx.WaifuInfo.DeleteAsync();
        await ctx.SaveChangesAsync();
    }

    public async Task DeleteWaifu(ulong userId)
    {
        await using var ctx = _db.GetDbContext();
        await ctx.WaifuUpdates
                 .Where(x => x.User.UserId == userId)
                 .DeleteAsync();
        await ctx.WaifuItem
                 .Where(x => x.WaifuInfo.Waifu.UserId == userId)
                 .DeleteAsync();
        await ctx.WaifuInfo
                 .Where(x => x.Claimer.UserId == userId)
                 .UpdateAsync(old => new WaifuInfo()
                 {
                     ClaimerId = null,
                 });
        await ctx.WaifuInfo
                 .Where(x => x.Waifu.UserId == userId)
                 .DeleteAsync();
        await ctx.SaveChangesAsync();
    }
    
    public async Task DeletePlaylists()
    {
        await using var ctx = _db.GetDbContext();
        await ctx.MusicPlaylists.DeleteAsync();
        await ctx.SaveChangesAsync();
    }

    public async Task DeleteCurrency()
    {
        await using var ctx = _db.GetDbContext();
        await ctx.DiscordUser.UpdateAsync(_ => new DiscordUser()
        {
            CurrencyAmount = 0
        });

        await ctx.CurrencyTransactions.DeleteAsync();
        await ctx.PlantedCurrency.DeleteAsync();
        await ctx.SaveChangesAsync();
    }

    public async Task<int> ExecuteSql(string sql)
    {
        int res;
        await using var uow = _db.GetDbContext();
        res = await uow.Database.ExecuteSqlRawAsync(sql);
        return res;
    }

    public SelectResult SelectSql(string sql)
    {
        var result = new SelectResult
        {
            ColumnNames = new(),
            Results = new()
        };

        using var uow = _db.GetDbContext();
        var conn = uow.Database.GetDbConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        using var reader = cmd.ExecuteReader();
        if (reader.HasRows)
        {
            for (var i = 0; i < reader.FieldCount; i++)
                result.ColumnNames.Add(reader.GetName(i));
            while (reader.Read())
            {
                var obj = new object[reader.FieldCount];
                reader.GetValues(obj);
                result.Results.Add(obj.Select(x => x.ToString()).ToArray());
            }
        }

        return result;
    }

    public async Task PurgeUserAsync(ulong userId)
    {
        await using var uow = _db.GetDbContext();

        // get waifu info
        var wi = await uow.Set<WaifuInfo>().FirstOrDefaultAsyncEF(x => x.Waifu.UserId == userId);

        // if it exists, delete waifu related things
        if (wi is not null)
        {
            // remove updates which have new or old as this waifu
            await uow.WaifuUpdates.DeleteAsync(wu => wu.New.UserId == userId || wu.Old.UserId == userId);

            // delete all items this waifu owns
            await uow.Set<WaifuItem>().DeleteAsync(x => x.WaifuInfoId == wi.Id);

            // all waifus this waifu claims are released
            await uow.Set<WaifuInfo>()
                     .AsQueryable()
                     .Where(x => x.Claimer.UserId == userId)
                     .UpdateAsync(x => new()
                     {
                         ClaimerId = null
                     });

            // all affinities set to this waifu are reset
            await uow.Set<WaifuInfo>()
                     .AsQueryable()
                     .Where(x => x.Affinity.UserId == userId)
                     .UpdateAsync(x => new()
                     {
                         AffinityId = null
                     });
        }

        // delete guild xp
        await uow.UserXpStats.DeleteAsync(x => x.UserId == userId);

        // delete currency transactions
        await uow.Set<CurrencyTransaction>().DeleteAsync(x => x.UserId == userId);

        // delete user, currency, and clubs go away with it
        await uow.DiscordUser.DeleteAsync(u => u.UserId == userId);
    }

    public class SelectResult
    {
        public List<string> ColumnNames { get; set; }
        public List<string[]> Results { get; set; }
    }
}