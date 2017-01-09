using WizBot.Services.Database.Models;
using System.Collections.Generic;

namespace WizBot.Services.Database.Repositories
{
    public interface ICurrencyRepository : IRepository<Currency>
    {
        Currency GetOrCreate(ulong userId);
        long GetUserCurrency(ulong userId);
        bool TryUpdateState(ulong userId, long change);
        IEnumerable<Currency> GetTopRichest(int count);
    }
}
