using WizBot.Services.Database.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WizBot.Services.Database.Repositories
{
    public interface IQuoteRepository : IRepository<Quote>
    {
        IEnumerable<Quote> GetAllQuotesByKeyword(ulong guildId, string keyword);
        Task<Quote> GetRandomQuoteByKeywordAsync(ulong guildId, string keyword);
        IEnumerable<Quote> GetGroup(ulong guildId, int skip, int take);
    }
}
