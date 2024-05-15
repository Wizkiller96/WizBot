#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Db.Models;

namespace NadekoBot.Db;

public static class UserXpExtensions
{
    public static UserXpStats GetOrCreateUserXpStats(this DbContext ctx, ulong guildId, ulong userId)
    {
        var usr = ctx.Set<UserXpStats>().FirstOrDefault(x => x.UserId == userId && x.GuildId == guildId);

        if (usr is null)
        {
            ctx.Add(usr = new()
            {
                Xp = 0,
                UserId = userId,
                NotifyOnLevelUp = XpNotificationLocation.None,
                GuildId = guildId
            });
        }

        return usr;
    }

    public static async Task<IReadOnlyCollection<UserXpStats>> GetUsersFor(
        this DbSet<UserXpStats> xps,
        ulong guildId,
        int page)
        => await xps.ToLinqToDBTable()
                    .Where(x => x.GuildId == guildId)
                    .OrderByDescending(x => x.Xp + x.AwardedXp)
                    .Skip(page * 9)
                    .Take(9)
                    .ToArrayAsyncLinqToDB();

    public static async Task<List<UserXpStats>> GetTopUserXps(this DbSet<UserXpStats> xps, ulong guildId, int count)
        => await xps.ToLinqToDBTable()
                    .Where(x => x.GuildId == guildId)
                    .OrderByDescending(x => x.Xp + x.AwardedXp)
                    .Take(count)
                    .ToListAsyncLinqToDB();

    public static async Task<int> GetUserGuildRanking(this DbSet<UserXpStats> xps, ulong userId, ulong guildId)
        => await xps.ToLinqToDBTable()
                    .Where(x => x.GuildId == guildId
                                && x.Xp + x.AwardedXp
                                > xps.AsQueryable()
                                     .Where(y => y.UserId == userId && y.GuildId == guildId)
                                     .Select(y => y.Xp + y.AwardedXp)
                                     .FirstOrDefault())
                    .CountAsyncLinqToDB()
           + 1;

    public static void ResetGuildUserXp(this DbSet<UserXpStats> xps, ulong userId, ulong guildId)
        => xps.Delete(x => x.UserId == userId && x.GuildId == guildId);

    public static void ResetGuildXp(this DbSet<UserXpStats> xps, ulong guildId)
        => xps.Delete(x => x.GuildId == guildId);

    public static async Task<LevelStats> GetLevelDataFor(this ITable<UserXpStats> userXp, ulong guildId, ulong userId)
        => await userXp
                 .Where(x => x.GuildId == guildId && x.UserId == userId)
                 .FirstOrDefaultAsyncLinqToDB() is UserXpStats uxs
            ? new(uxs.Xp + uxs.AwardedXp)
            : new(0);
}