using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Core.Services;
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using NadekoBot.Core.Services.Database.Models;

namespace NadekoBot.Core.Modules.Administration.Services
{
    public class DangerousCommandsService : INService
    {
        public const string WaifusDeleteSql = @"DELETE FROM WaifuUpdates;
DELETE FROM WaifuItem;
DELETE FROM WaifuInfo;";
        public const string WaifuDeleteSql = @"DELETE FROM WaifuUpdates WHERE UserId=(SELECT Id FROM DiscordUser WHERE UserId={0});
DELETE FROM WaifuItem WHERE WaifuInfoId=(SELECT Id FROM WaifuInfo WHERE WaifuId=(SELECT Id FROM DiscordUser WHERE UserId={0}));
UPDATE WaifuInfo SET ClaimerId=NULL WHERE ClaimerId=(SELECT Id FROM DiscordUser WHERE UserId={0});
DELETE FROM WaifuInfo WHERE WaifuId=(SELECT Id FROM DiscordUser WHERE UserId={0});";
        public const string CurrencyDeleteSql = "UPDATE DiscordUser SET CurrencyAmount=0; DELETE FROM CurrencyTransactions; DELETE FROM PlantedCurrency;";
        public const string MusicPlaylistDeleteSql = "DELETE FROM MusicPlaylists;";
        public const string XpDeleteSql = @"DELETE FROM UserXpStats;
UPDATE DiscordUser
SET ClubId=NULL,
    IsClubAdmin=0,
    TotalXp=0;
DELETE FROM ClubApplicants;
DELETE FROM ClubBans;
DELETE FROM Clubs;";
//        public const string DeleteUnusedCustomReactionsAndQuotes = @"DELETE FROM CustomReactions 
//WHERE UseCount=0 AND (DateAdded < date('now', '-7 day') OR DateAdded is null);

//DELETE FROM Quotes 
//WHERE UseCount=0 AND (DateAdded < date('now', '-7 day') OR DateAdded is null);";

        private readonly DbService _db;

        public DangerousCommandsService(DbService db)
        {
            _db = db;
        }

        public async Task<int> ExecuteSql(string sql)
        {
            int res;
            using (var uow = _db.GetDbContext())
            {
                res = await uow._context.Database.ExecuteSqlRawAsync(sql);
            }
            return res;
        }

        public class SelectResult
        {
            public List<string> ColumnNames { get; set; }
            public List<string[]> Results { get; set; }
        }

        public SelectResult SelectSql(string sql)
        {
            var result = new SelectResult()
            {
                ColumnNames = new List<string>(),
                Results = new List<string[]>(),
            };

            using (var uow = _db.GetDbContext())
            {
                var conn = uow._context.Database.GetDbConnection();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sql;
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                result.ColumnNames.Add(reader.GetName(i));
                            }
                            while (reader.Read())
                            {
                                var obj = new object[reader.FieldCount];
                                reader.GetValues(obj);
                                result.Results.Add(obj.Select(x => x.ToString()).ToArray());
                            }
                        }
                    }
                }
            }
            return result;
        }

        public async Task PurgeUserAsync(ulong userId)
        {
            using var uow = _db.GetDbContext();
            
            // get waifu info
            var wi = await uow._context.Set<WaifuInfo>()
                .FirstOrDefaultAsyncEF(x => x.Waifu.UserId == userId);

            // if it exists, delete waifu related things
            if (!(wi is null))
            {
                // remove updates which have new or old as this waifu
                await uow._context
                    .WaifuUpdates
                    .DeleteAsync(wu => wu.New.UserId == userId || wu.Old.UserId == userId);

                // delete all items this waifu owns
                await uow._context
                    .Set<WaifuItem>()
                    .DeleteAsync(x => x.WaifuInfoId == wi.Id);

                // all waifus this waifu claims are released
                await uow._context
                    .Set<WaifuInfo>()
                    .AsQueryable()
                    .Where(x => x.Claimer.UserId == userId)
                    .UpdateAsync(x => new WaifuInfo() {ClaimerId = null});
                
                // all affinities set to this waifu are reset
                await uow._context
                    .Set<WaifuInfo>()
                    .AsQueryable()
                    .Where(x => x.Affinity.UserId == userId)
                    .UpdateAsync(x => new WaifuInfo() {AffinityId = null});
            }

            // delete guild xp
            await uow._context
                .UserXpStats
                .DeleteAsync(x => x.UserId == userId);
            
            // delete currency transactions
            await uow._context.Set<CurrencyTransaction>()
                .DeleteAsync(x => x.UserId == userId);
            
            // delete user, currency, and clubs go away with it
            await uow._context.DiscordUser
                .DeleteAsync(u => u.UserId == userId);
        }
    }
}
