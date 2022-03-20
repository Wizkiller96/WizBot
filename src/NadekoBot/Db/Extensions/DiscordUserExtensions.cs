#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Db.Models;
using NadekoBot.Services.Database;

namespace NadekoBot.Db;

public static class DiscordUserExtensions
{
    public static void EnsureUserCreated(
        this NadekoContext ctx,
        ulong userId,
        string username,
        string discrim,
        string avatarId)
        => ctx.DiscordUser.ToLinqToDBTable()
              .InsertOrUpdate(
                  () => new()
                  {
                      UserId = userId,
                      Username = username,
                      Discriminator = discrim,
                      AvatarId = avatarId,
                      TotalXp = 0,
                      CurrencyAmount = 0
                  },
                  old => new()
                  {
                      Username = username,
                      Discriminator = discrim,
                      AvatarId = avatarId
                  },
                  () => new()
                  {
                      UserId = userId
                  });

    //temp is only used in updatecurrencystate, so that i don't overwrite real usernames/discrims with Unknown
    public static DiscordUser GetOrCreateUser(
        this NadekoContext ctx,
        ulong userId,
        string username,
        string discrim,
        string avatarId)
    {
        ctx.EnsureUserCreated(userId, username, discrim, avatarId);
        return ctx.DiscordUser.Include(x => x.Club).First(u => u.UserId == userId);
    }

    public static DiscordUser GetOrCreateUser(this NadekoContext ctx, IUser original)
        => ctx.GetOrCreateUser(original.Id, original.Username, original.Discriminator, original.AvatarId);

    public static int GetUserGlobalRank(this DbSet<DiscordUser> users, ulong id)
        => users.AsQueryable()
                .Where(x => x.TotalXp
                            > users.AsQueryable().Where(y => y.UserId == id).Select(y => y.TotalXp).FirstOrDefault())
                .Count()
           + 1;

    public static DiscordUser[] GetUsersXpLeaderboardFor(this DbSet<DiscordUser> users, int page)
        => users.AsQueryable().OrderByDescending(x => x.TotalXp).Skip(page * 9).Take(9).AsEnumerable().ToArray();

    public static List<DiscordUser> GetTopRichest(
        this DbSet<DiscordUser> users,
        ulong botId,
        int count,
        int page = 0)
        => users.AsQueryable()
                .Where(c => c.CurrencyAmount > 0 && botId != c.UserId)
                .OrderByDescending(c => c.CurrencyAmount)
                .Skip(page * 9)
                .Take(count)
                .ToList();

    public static long GetUserCurrency(this DbSet<DiscordUser> users, ulong userId)
        => users.AsNoTracking().FirstOrDefault(x => x.UserId == userId)?.CurrencyAmount ?? 0;

    public static void RemoveFromMany(this DbSet<DiscordUser> users, IEnumerable<ulong> ids)
    {
        var items = users.AsQueryable().Where(x => ids.Contains(x.UserId));
        foreach (var item in items)
            item.CurrencyAmount = 0;
    }

    public static decimal GetTotalCurrency(this DbSet<DiscordUser> users)
        => users.Sum((Func<DiscordUser, decimal>)(x => x.CurrencyAmount));

    public static decimal GetTopOnePercentCurrency(this DbSet<DiscordUser> users, ulong botId)
        => users.AsQueryable()
                .Where(x => x.UserId != botId)
                .OrderByDescending(x => x.CurrencyAmount)
                .Take(users.Count() / 100 == 0 ? 1 : users.Count() / 100)
                .Sum(x => x.CurrencyAmount);
}