#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Db.Models;
using NadekoBot.Services.Database;
using NadekoBot.Services.Database.Models;

namespace NadekoBot.Db;

public class WaifuInfoStats
{
    public string FullName { get; init; }
    public long Price { get; init; }
    public string ClaimerName { get; init; }
    public string AffinityName { get; init; }
    public int AffinityCount { get; init; }
    public int DivorceCount { get; init; }
    public int ClaimCount { get; init; }
    public List<WaifuItem> Items { get; init; }
    public List<string> Claims { get; init; }
    public List<string> Fans { get; init; }
}

public static class WaifuExtensions
{
    public static WaifuInfo ByWaifuUserId(
        this DbSet<WaifuInfo> waifus,
        ulong userId,
        Func<DbSet<WaifuInfo>, IQueryable<WaifuInfo>> includes = null)
    {
        if (includes is null)
        {
            return waifus.Include(wi => wi.Waifu)
                         .Include(wi => wi.Affinity)
                         .Include(wi => wi.Claimer)
                         .Include(wi => wi.Items)
                         .FirstOrDefault(wi => wi.Waifu.UserId == userId);
        }

        return includes(waifus).AsQueryable().FirstOrDefault(wi => wi.Waifu.UserId == userId);
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
                         Price = x.Price
                     })
                     .ToList();
    }

    public static decimal GetTotalValue(this DbSet<WaifuInfo> waifus)
        => waifus.AsQueryable().Where(x => x.ClaimerId != null).Sum(x => x.Price);

    public static ulong GetWaifuUserId(this DbSet<WaifuInfo> waifus, ulong ownerId, string name)
        => waifus.AsQueryable()
                 .AsNoTracking()
                 .Where(x => x.Claimer.UserId == ownerId && x.Waifu.Username + "#" + x.Waifu.Discriminator == name)
                 .Select(x => x.Waifu.UserId)
                 .FirstOrDefault();

    public static async Task<WaifuInfoStats> GetWaifuInfoAsync(this NadekoContext ctx, ulong userId)
    {
        await ctx.WaifuInfo
                 .ToLinqToDBTable()
                 .InsertOrUpdateAsync(() => new()
                     {
                         AffinityId = null,
                         ClaimerId = null,
                         Price = 1,
                         WaifuId = ctx.DiscordUser.Where(x => x.UserId == userId).Select(x => x.Id).First()
                     },
                     _ => new(),
                     () => new()
                     {
                         WaifuId = ctx.DiscordUser.Where(x => x.UserId == userId).Select(x => x.Id).First()
                     });

        var toReturn = ctx.WaifuInfo.AsQueryable()
                          .Where(w => w.WaifuId
                                      == ctx.Set<DiscordUser>()
                                            .AsQueryable()
                                            .Where(u => u.UserId == userId)
                                            .Select(u => u.Id)
                                            .FirstOrDefault())
                          .Select(w => new WaifuInfoStats
                          {
                              FullName =
                                  ctx.Set<DiscordUser>()
                                     .AsQueryable()
                                     .Where(u => u.UserId == userId)
                                     .Select(u => u.Username + "#" + u.Discriminator)
                                     .FirstOrDefault(),
                              AffinityCount =
                                  ctx.Set<WaifuUpdate>()
                                     .AsQueryable()
                                     .Count(x => x.UserId == w.WaifuId
                                                 && x.UpdateType == WaifuUpdateType.AffinityChanged
                                                 && x.NewId != null),
                              AffinityName =
                                  ctx.Set<DiscordUser>()
                                     .AsQueryable()
                                     .Where(u => u.Id == w.AffinityId)
                                     .Select(u => u.Username + "#" + u.Discriminator)
                                     .FirstOrDefault(),
                              ClaimCount = ctx.WaifuInfo.AsQueryable().Count(x => x.ClaimerId == w.WaifuId),
                              ClaimerName =
                                  ctx.Set<DiscordUser>()
                                     .AsQueryable()
                                     .Where(u => u.Id == w.ClaimerId)
                                     .Select(u => u.Username + "#" + u.Discriminator)
                                     .FirstOrDefault(),
                              DivorceCount =
                                  ctx.Set<WaifuUpdate>()
                                     .AsQueryable()
                                     .Count(x => x.OldId == w.WaifuId
                                                 && x.NewId == null
                                                 && x.UpdateType == WaifuUpdateType.Claimed),
                              Price = w.Price,
                              Claims = ctx.WaifuInfo.AsQueryable()
                                          .Include(x => x.Waifu)
                                          .Where(x => x.ClaimerId == w.WaifuId)
                                          .Select(x => x.Waifu.Username + "#" + x.Waifu.Discriminator)
                                          .ToList(),
                              Fans = ctx.WaifuInfo.AsQueryable()
                                        .Include(x => x.Waifu)
                                        .Where(x => x.AffinityId == w.WaifuId)
                                        .Select(x => x.Waifu.Username + "#" + x.Waifu.Discriminator)
                                        .ToList(),
                              Items = w.Items
                          })
                          .FirstOrDefault();

        if (toReturn is null)
            return null;

        return toReturn;
    }
}