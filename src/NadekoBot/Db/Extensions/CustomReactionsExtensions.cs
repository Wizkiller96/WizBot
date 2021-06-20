using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using LinqToDB;
using NadekoBot.Services.Database.Models;

namespace NadekoBot.Db
{
    public static class CustomReactionsExtensions
    {
        public static int ClearFromGuild(this DbSet<CustomReaction> crs, ulong guildId)
        {
            return crs.Delete(x => x.GuildId == guildId);
        }

        public static IEnumerable<CustomReaction> ForId(this DbSet<CustomReaction> crs, ulong id)
        {
            return crs
                .AsNoTracking()
                .AsQueryable()
                .Where(x => x.GuildId == id)
                .ToArray();
        }

        public static CustomReaction GetByGuildIdAndInput(this DbSet<CustomReaction> crs, ulong? guildId, string input)
        {
            return crs.FirstOrDefault(x => x.GuildId == guildId && x.Trigger.ToUpper() == input);
        }
    }
}
