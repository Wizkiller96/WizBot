using NadekoBot.Services.Database.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace NadekoBot.Db
{
    public static class WarningExtensions
    {
        public static Warning[] ForId(this DbSet<Warning> warnings, ulong guildId, ulong userId)
        {
            var query = warnings.AsQueryable()
                .Where(x => x.GuildId == guildId && x.UserId == userId)
                .OrderByDescending(x => x.DateAdded);

            return query.ToArray();
        }

        public static bool Forgive(this DbSet<Warning> warnings, ulong guildId, ulong userId, string mod, int index)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));

            var warn = warnings.AsQueryable().Where(x => x.GuildId == guildId && x.UserId == userId)
                .OrderByDescending(x => x.DateAdded)
                .Skip(index)
                .FirstOrDefault();

            if (warn is null || warn.Forgiven)
                return false;

            warn.Forgiven = true;
            warn.ForgivenBy = mod;
            return true;
        }

        public static async Task ForgiveAll(this DbSet<Warning> warnings, ulong guildId, ulong userId, string mod)
        {
            await warnings.AsQueryable().Where(x => x.GuildId == guildId && x.UserId == userId)
                .ForEachAsync(x =>
                {
                    if (x.Forgiven != true)
                    {
                        x.Forgiven = true;
                        x.ForgivenBy = mod;
                    }
                });
        }

        public static Warning[] GetForGuild(this DbSet<Warning> warnings, ulong id)
        {
            return warnings.AsQueryable().Where(x => x.GuildId == id).ToArray();
        }
    }
}
