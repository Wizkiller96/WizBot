using WizBot.Services.Database.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace WizBot.Services.Database.Repositories.Impl
{
    public class QuoteRepository : Repository<Quote>, IQuoteRepository
    {
        public QuoteRepository(DbContext context) : base(context)
        {
        }

        public IEnumerable<Quote> GetAllQuotesByKeyword(ulong guildId, string keyword) => 
            _set.Where(q => q.GuildId == guildId && q.Keyword == keyword);

        public IEnumerable<Quote> GetGroup(ulong guildId, int skip, int take) => 
            _set.Where(q=>q.GuildId == guildId).OrderBy(q => q.Keyword).Skip(skip).Take(take).ToList();

        public Task<Quote> GetRandomQuoteByKeywordAsync(ulong guildId, string keyword)
        {
            var rng = new WizBotRandom();
            return _set.Where(q => q.GuildId == guildId && q.Keyword == keyword).OrderBy(q => rng.Next()).FirstOrDefaultAsync();
        }
        public Task<Quote> SearchQuoteKeywordTextAsync(ulong guildId, string keyword, string text)
        {      			
            var rngk = new WizBotRandom();
            return _set.Where(q => q.Text.Contains(text) && q.GuildId == guildId && q.Keyword == keyword).OrderBy(q => rngk.Next()).FirstOrDefaultAsync();
	    }
    }
}
