﻿using WizBot.Services.Database.Models;
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

        public IEnumerable<Quote> GetGroup(int skip, int take) => 
            _set.OrderBy(q => q.Keyword).Skip(skip).Take(take).ToList();

        public Task<Quote> GetRandomQuoteByKeywordAsync(ulong guildId, string keyword)
        {
            var rng = new WizBotRandom();
            return _set.Where(q => q.GuildId == guildId && q.Keyword == keyword).OrderBy(q => rng.Next()).FirstOrDefaultAsync();
        }
    }
}
