using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Services.Database.Models;

namespace NadekoBot.Db
{
    public static class CurrencyTransactionExtensions
    {
        public static List<CurrencyTransaction> GetPageFor(this DbSet<CurrencyTransaction> set, ulong userId, int page)
        {
            return set.AsQueryable()
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.DateAdded)
                .Skip(15 * page)
                .Take(15)
                .ToList();
        }
    }
}