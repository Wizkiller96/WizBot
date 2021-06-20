using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Services.Database;
using NadekoBot.Services.Database.Models;
using NadekoBot.Db.Models;

namespace NadekoBot.Db
{
    public class WaifuInfoStats
    {
        public string FullName { get; set; }
        public int Price { get; set; }
        public string ClaimerName { get; set; }
        public string AffinityName { get; set; }
        public int AffinityCount { get; set; }
        public int DivorceCount { get; set; }
        public int ClaimCount { get; set; }
        public List<WaifuItem> Items { get; set; }
        public List<string> Claims { get; set; }
        public List<string> Fans { get; set; }
    }
    
    public static class WaifuExtensions
    {
        public static WaifuInfo ByWaifuUserId(this DbSet<WaifuInfo> waifus, ulong userId, Func<DbSet<WaifuInfo>, IQueryable<WaifuInfo>> includes = null)
        {
            if (includes is null)
            {
                return waifus.Include(wi => wi.Waifu)
                            .Include(wi => wi.Affinity)
                            .Include(wi => wi.Claimer)
                            .Include(wi => wi.Items)
                            .FirstOrDefault(wi => wi.Waifu.UserId == userId);
            }

            return includes(waifus)
                .AsQueryable()
                .FirstOrDefault(wi => wi.Waifu.UserId == userId);
        }

        public static IEnumerable<WaifuLbResult> GetTop(this DbSet<WaifuInfo> waifus, int count, int skip = 0)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (count == 0)
                return new List<WaifuLbResult>();

            return waifus.Include(wi => wi.Waifu)
                        .Include(wi => wi.Affinity)
                        .Include(wi => wi.Claimer)
                    .OrderByDescending(wi => wi.Price)
                    .Skip(skip)
                    .Take(count)
                    .Select(x => new WaifuLbResult
                    {
                        Affinity = x.Affinity == null ? null : x.Affinity.Username,
                        AffinityDiscrim = x.Affinity == null ? null : x.Affinity.Discriminator,
                        Claimer = x.Claimer == null ? null : x.Claimer.Username,
                        ClaimerDiscrim = x.Claimer == null ? null : x.Claimer.Discriminator,
                        Username = x.Waifu.Username,
                        Discrim = x.Waifu.Discriminator,
                        Price = x.Price,
                    })
                    .ToList();

        }

        public static decimal GetTotalValue(this DbSet<WaifuInfo> waifus)
        {
            return waifus
                .AsQueryable()
                .Where(x => x.ClaimerId != null)
                .Sum(x => x.Price);
        }

        public static ulong GetWaifuUserId(this DbSet<WaifuInfo> waifus, ulong ownerId, string name)
        {
            return waifus
                .AsQueryable()
                .AsNoTracking()
                .Where(x => x.Claimer.UserId == ownerId
                            && x.Waifu.Username + "#" + x.Waifu.Discriminator == name)
                .Select(x => x.Waifu.UserId)
                .FirstOrDefault();
        }
        
        public static WaifuInfoStats GetWaifuInfo(this NadekoContext ctx, ulong userId)
        {
            ctx.Database.ExecuteSqlInterpolated($@"
INSERT OR IGNORE INTO WaifuInfo (AffinityId, ClaimerId, Price, WaifuId)
VALUES ({null}, {null}, {1}, (SELECT Id FROM DiscordUser WHERE UserId={userId}));");

            var toReturn = ctx.WaifuInfo
                .AsQueryable()
                .Where(w => w.WaifuId == ctx.Set<DiscordUser>()
                    .AsQueryable()
                    .Where(u => u.UserId == userId)
                    .Select(u => u.Id).FirstOrDefault())
                .Select(w => new WaifuInfoStats
                {
                    FullName = ctx.Set<DiscordUser>()
                        .AsQueryable()
                        .Where(u => u.UserId == userId)
                        .Select(u => u.Username + "#" + u.Discriminator)
                        .FirstOrDefault(),

                    AffinityCount = ctx.Set<WaifuUpdate>()
                        .AsQueryable()
                        .Count(x => x.UserId == w.WaifuId &&
                            x.UpdateType == WaifuUpdateType.AffinityChanged &&
                            x.NewId != null),

                    AffinityName = ctx.Set<DiscordUser>()
                        .AsQueryable()
                        .Where(u => u.Id == w.AffinityId)
                        .Select(u => u.Username + "#" + u.Discriminator)
                        .FirstOrDefault(),

                    ClaimCount = ctx.WaifuInfo
                        .AsQueryable()
                        .Count(x => x.ClaimerId == w.WaifuId),

                    ClaimerName = ctx.Set<DiscordUser>()
                        .AsQueryable()
                        .Where(u => u.Id == w.ClaimerId)
                        .Select(u => u.Username + "#" + u.Discriminator)
                        .FirstOrDefault(),

                    DivorceCount = ctx
                        .Set<WaifuUpdate>()
                        .AsQueryable()
                        .Count(x => x.OldId == w.WaifuId &&
                                    x.NewId == null &&
                                    x.UpdateType == WaifuUpdateType.Claimed),

                    Price = w.Price,

                    Claims = ctx.WaifuInfo
                        .AsQueryable()
                        .Include(x => x.Waifu)
                        .Where(x => x.ClaimerId == w.WaifuId)
                        .Select(x => x.Waifu.Username + "#" + x.Waifu.Discriminator)
                        .ToList(),

                    Fans = ctx.WaifuInfo
                        .AsQueryable()
                        .Include(x => x.Waifu)
                        .Where(x => x.AffinityId == w.WaifuId)
                        .Select(x => x.Waifu.Username + "#" + x.Waifu.Discriminator)
                        .ToList(),
                    
                    Items = w.Items,
                })
            .FirstOrDefault();

            if (toReturn is null)
                return null;
            
            return toReturn;
        }
    }
}