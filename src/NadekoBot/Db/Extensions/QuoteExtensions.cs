using NadekoBot.Services.Database.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Common;

namespace NadekoBot.Db
{
    public static class QuoteExtensions
    {
        public static IEnumerable<Quote> GetGroup(this DbSet<Quote> quotes, ulong guildId, int page, OrderType order)
        {
            var q = quotes.AsQueryable().Where(x => x.GuildId == guildId);
            if (order == OrderType.Keyword)
                q = q.OrderBy(x => x.Keyword);
            else
                q = q.OrderBy(x => x.Id);

            return q.Skip(15 * page).Take(15).ToArray();
        }

        public static async Task<Quote> GetRandomQuoteByKeywordAsync(this DbSet<Quote> quotes, ulong guildId, string keyword)
        {
            var rng = new NadekoRandom();
            return (await quotes.AsQueryable()
                .Where(q => q.GuildId == guildId && q.Keyword == keyword)
                .ToListAsync())
                .OrderBy(q => rng.Next())
                .FirstOrDefault();
        }

        public static async Task<Quote> SearchQuoteKeywordTextAsync(this DbSet<Quote> quotes, ulong guildId, string keyword, string text)
        {
            var rngk = new NadekoRandom();
            return (await quotes.AsQueryable()
                .Where(q => q.GuildId == guildId
                            && q.Keyword == keyword
                            && EF.Functions.Like(q.Text.ToUpper(), $"%{text.ToUpper()}%")
                            // && q.Text.Contains(text, StringComparison.OrdinalIgnoreCase)
                            )
                .ToListAsync())
                .OrderBy(q => rngk.Next())
                .FirstOrDefault();
        }

        public static void RemoveAllByKeyword(this DbSet<Quote> quotes, ulong guildId, string keyword)
        {
            quotes.RemoveRange(quotes.AsQueryable().Where(x => x.GuildId == guildId && x.Keyword.ToUpper() == keyword));
        }

    }
}
