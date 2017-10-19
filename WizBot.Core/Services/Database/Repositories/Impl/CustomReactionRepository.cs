using WizBot.Core.Services.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace WizBot.Core.Services.Database.Repositories.Impl
{
    public class CustomReactionsRepository : Repository<CustomReaction>, ICustomReactionRepository
    {
        public CustomReactionsRepository(DbContext context) : base(context)
        {
        }
    }
}
