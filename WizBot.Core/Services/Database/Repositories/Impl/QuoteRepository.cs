using WizBot.Core.Services.Database.Models;
using WizBot.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WizBot.Common;

namespace WizBot.Core.Services.Database.Repositories.Impl
{
    public class QuoteRepository : Repository<Quote>, IQuoteRepository
    {
        public QuoteRepository(DbContext context) : base(context)
        {
        }

        public IEnumerable<Quote> GetGroup(ulong guildId, int page, OrderType order)
        {
            var q = _set.AsQueryable().Where(x => x.GuildId == guildId);
            if (order == OrderType.Keyword)
                q = q.OrderBy(x => x.Keyword);
            else
                q = q.OrderBy(x => x.Id);

            return q.Skip(15 * page).Take(15).ToArray();
        }

        public Task<Quote> GetRandomQuoteByKeywordAsync(ulong guildId, string keyword)
        {
            var rng = new WizBotRandom();
            return _set.AsQueryable()
                .Where(q => q.GuildId == guildId && q.Keyword == keyword).OrderBy(q => rng.Next())
                .FirstOrDefaultAsync();
        }

        public async Task<Quote> SearchQuoteKeywordTextAsync(ulong guildId, string keyword, string text)
        {
            var rngk = new WizBotRandom();
            return (await _set.AsQueryable()
                .Where(q => q.Text.ContainsNoCase(text, StringComparison.OrdinalIgnoreCase)
                    && q.GuildId == guildId && q.Keyword == keyword)
                .ToListAsync())
                .OrderBy(q => rngk.Next())
                .FirstOrDefault();
        }

        public void RemoveAllByKeyword(ulong guildId, string keyword)
        {
            _set.RemoveRange(_set.AsQueryable().Where(x => x.GuildId == guildId && x.Keyword.ToUpperInvariant() == keyword));
        }

    }
}
