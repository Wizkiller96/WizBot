#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Db.Models;
using NadekoBot.Services.Database;
using System.Collections.Immutable;

namespace NadekoBot.Db;

public static class DiscordUserExtensions
{
    /// <summary>
    /// Adds the specified <paramref name="users"/> to the database. If a database user with placeholder name
    /// and discriminator is present in <paramref name="users"/>, their name and discriminator get updated accordingly.
    /// </summary>
    /// <param name="ctx">This database context.</param>
    /// <param name="users">The users to add or update in the database.</param>
    /// <returns>A tuple with the amount of new users added and old users updated.</returns>
    public static async Task<(long UsersAdded, long UsersUpdated)> RefreshUsersAsync(this NadekoContext ctx, List<IUser> users)
    {
        var presentDbUsers = await ctx.DiscordUser
            .Select(x => new { x.UserId, x.Username, x.Discriminator })
            .Where(x => users.Select(y => y.Id).Contains(x.UserId))
            .ToArrayAsyncEF();

        var usersToAdd = users
            .Where(x => !presentDbUsers.Select(x => x.UserId).Contains(x.Id))
            .Select(x => new DiscordUser()
            {
                UserId = x.Id,
                AvatarId = x.AvatarId,
                Username = x.Username,
                Discriminator = x.Discriminator
            });

        var added = (await ctx.BulkCopyAsync(usersToAdd)).RowsCopied;
        var toUpdateUserIds = presentDbUsers
            .Where(x => x.Username == "Unknown" && x.Discriminator == "????")
            .Select(x => x.UserId)
            .ToArray();

        foreach (var user in users.Where(x => toUpdateUserIds.Contains(x.Id)))
        {
            await ctx.DiscordUser
                .Where(x => x.UserId == user.Id)
                .UpdateAsync(x => new DiscordUser()
                {
                    Username = user.Username,
                    Discriminator = user.Discriminator,

                    // .award tends to set AvatarId and DateAdded to NULL, so account for that.
                    AvatarId = user.AvatarId,
                    DateAdded = x.DateAdded ?? DateTime.UtcNow
                });
        }

        return (added, toUpdateUserIds.Length);
    }

    public static Task<DiscordUser> GetByUserIdAsync(
        this IQueryable<DiscordUser> set,
        ulong userId)
        => set.FirstOrDefaultAsyncLinqToDB(x => x.UserId == userId);
    
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

    public static Task EnsureUserCreatedAsync(
        this NadekoContext ctx,
        ulong userId)
        => ctx.DiscordUser
              .ToLinqToDBTable()
              .InsertOrUpdateAsync(
                  () => new()
                  {
                      UserId = userId,
                      Username = "Unknown",
                      Discriminator = "????",
                      AvatarId = string.Empty,
                      TotalXp = 0,
                      CurrencyAmount = 0
                  },
                  old => new()
                  {

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
        string avatarId,
        Func<IQueryable<DiscordUser>, IQueryable<DiscordUser>> includes = null)
    {
        ctx.EnsureUserCreated(userId, username, discrim, avatarId);

        IQueryable<DiscordUser> queryable = ctx.DiscordUser;
        if (includes is not null)
            queryable = includes(queryable);
        return queryable.First(u => u.UserId == userId);
    }

    public static DiscordUser GetOrCreateUser(this NadekoContext ctx, IUser original, Func<IQueryable<DiscordUser>, IQueryable<DiscordUser>> includes = null)
        => ctx.GetOrCreateUser(original.Id, original.Username, original.Discriminator, original.AvatarId, includes);

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

    public static async Task<long> GetUserCurrencyAsync(this DbSet<DiscordUser> users, ulong userId)
        => (await users.FirstOrDefaultAsyncLinqToDB(x => x.UserId == userId))?.CurrencyAmount ?? 0;

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