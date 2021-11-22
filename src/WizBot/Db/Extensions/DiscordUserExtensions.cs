﻿using System;
using WizBot.Db.Models;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Discord;
using System.Collections.Generic;
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using WizBot.Services.Database;

namespace WizBot.Db
{
    public static class DiscordUserExtensions
    {
        public static void EnsureUserCreated(this WizBotContext ctx, ulong userId, string username, string discrim, string avatarId)
        {
            ctx.DiscordUser
                .ToLinqToDBTable()
                .InsertOrUpdate(() => new()
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
                        AvatarId = avatarId,
                    }, () => new()
                    {
                        UserId = userId
                    });
        }

        //temp is only used in updatecurrencystate, so that i don't overwrite real usernames/discrims with Unknown
        public static DiscordUser GetOrCreateUser(this WizBotContext ctx, ulong userId, string username, string discrim, string avatarId)
        {
            ctx.EnsureUserCreated(userId, username, discrim, avatarId);
            return ctx.DiscordUser
                .Include(x => x.Club)
                .First(u => u.UserId == userId);
        }

        public static DiscordUser GetOrCreateUser(this WizBotContext ctx, IUser original)
            => ctx.GetOrCreateUser(original.Id, original.Username, original.Discriminator, original.AvatarId);

        public static int GetUserGlobalRank(this DbSet<DiscordUser> users, ulong id)
        {
            return users.AsQueryable()
                .Where(x => x.TotalXp > (users
                    .AsQueryable()
                    .Where(y => y.UserId == id)
                    .Select(y => y.TotalXp)
                    .FirstOrDefault()))
                .Count() + 1;
        }

        public static DiscordUser[] GetUsersXpLeaderboardFor(this DbSet<DiscordUser> users, int page)
        {
            return users.AsQueryable()
                .OrderByDescending(x => x.TotalXp)
                .Skip(page * 9)
                .Take(9)
                .AsEnumerable()
                .ToArray();
        }

        public static List<DiscordUser> GetTopRichest(this DbSet<DiscordUser> users, ulong botId, int count, int page = 0)
        {
            return users.AsQueryable()
                .Where(c => c.CurrencyAmount > 0 && botId != c.UserId)
                .OrderByDescending(c => c.CurrencyAmount)
                .Skip(page * 9)
                .Take(count)
                .ToList();
        }

        public static List<DiscordUser> GetTopRichest(this DbSet<DiscordUser> users, ulong botId, int count)
        {
            return users.AsQueryable()
                .Where(c => c.CurrencyAmount > 0 && botId != c.UserId)
                .OrderByDescending(c => c.CurrencyAmount)
                .Take(count)
                .ToList();
        }

        public static long GetUserCurrency(this DbSet<DiscordUser> users, ulong userId) =>
            users.AsNoTracking()
                .FirstOrDefault(x => x.UserId == userId)
                ?.CurrencyAmount ?? 0;

        public static void RemoveFromMany(this DbSet<DiscordUser> users, IEnumerable<ulong> ids)
        {
            var items = users.AsQueryable().Where(x => ids.Contains(x.UserId));
            foreach (var item in items)
            {
                item.CurrencyAmount = 0;
            }
        }

        public static bool TryUpdateCurrencyState(this WizBotContext ctx, ulong userId, string name, string discrim, string avatarId, long amount, bool allowNegative = false)
        {
            if (amount == 0)
                return true;

            // if remove - try to remove if he has more or equal than the amount
            // and return number of rows > 0 (was there a change)
            if (amount < 0 && !allowNegative)
            {
                var rows = ctx.Database.ExecuteSqlInterpolated($@"
UPDATE DiscordUser
SET CurrencyAmount=CurrencyAmount+{amount}
WHERE UserId={userId} AND CurrencyAmount>={-amount};");
                return rows > 0;
            }

            // if remove and negative is allowed, just remove without any condition
            if (amount < 0 && allowNegative)
            {
                var rows = ctx.Database.ExecuteSqlInterpolated($@"
UPDATE DiscordUser
SET CurrencyAmount=CurrencyAmount+{amount}
WHERE UserId={userId};");
                return rows > 0;
            }

            // if add - create a new user with default values if it doesn't exist
            // if it exists, sum current amount with the new one, if it doesn't
            // he just has the new amount
            var updatedUserData = !string.IsNullOrWhiteSpace(name);
            name = name ?? "Unknown";
            discrim = discrim ?? "????";
            avatarId = avatarId ?? "";

            // just update the amount, there is no new user data
            if (!updatedUserData)
            {
                var rows = ctx.Database.ExecuteSqlInterpolated($@"
UPDATE OR IGNORE DiscordUser
SET CurrencyAmount=CurrencyAmount+{amount}
WHERE UserId={userId};

INSERT OR IGNORE INTO DiscordUser (UserId, Username, Discriminator, AvatarId, CurrencyAmount, TotalXp)
VALUES ({userId}, {name}, {discrim}, {avatarId}, {amount}, 0);
");
                
            }
            else
            {
                ctx.Database.ExecuteSqlInterpolated($@"
UPDATE OR IGNORE DiscordUser
SET CurrencyAmount=CurrencyAmount+{amount},
    Username={name},
    Discriminator={discrim},
    AvatarId={avatarId}
WHERE UserId={userId};

INSERT OR IGNORE INTO DiscordUser (UserId, Username, Discriminator, AvatarId, CurrencyAmount, TotalXp)
VALUES ({userId}, {name}, {discrim}, {avatarId}, {amount}, 0);
");
            }
            return true;
        }

        public static decimal GetTotalCurrency(this DbSet<DiscordUser> users)
        {
            return users
                .Sum((Func<DiscordUser, decimal>)(x => x.CurrencyAmount));
        }

        public static decimal GetTopOnePercentCurrency(this DbSet<DiscordUser> users, ulong botId)
        {
            return users.AsQueryable()
                .Where(x => x.UserId != botId)
                .OrderByDescending(x => x.CurrencyAmount)
                .Take(users.Count() / 100 == 0 ? 1 : users.Count() / 100)
                .Sum(x => x.CurrencyAmount);
        }
    }
}
