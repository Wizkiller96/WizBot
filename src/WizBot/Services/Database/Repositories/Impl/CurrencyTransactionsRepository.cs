using WizBot.Services.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace WizBot.Services.Database.Repositories.Impl
{
    public class CurrencyTransactionsRepository : Repository<CurrencyTransaction>, ICurrencyTransactionsRepository
    {
        public CurrencyTransactionsRepository(DbContext context) : base(context)
        {
        }
    }
}
