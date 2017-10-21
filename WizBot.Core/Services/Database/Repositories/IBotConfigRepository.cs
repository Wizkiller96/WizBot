using Microsoft.EntityFrameworkCore;
using WizBot.Core.Services.Database.Models;
using System;
using System.Linq;

namespace WizBot.Core.Services.Database.Repositories
{
    public interface IBotConfigRepository : IRepository<BotConfig>
    {
        BotConfig GetOrCreate(Func<DbSet<BotConfig>, IQueryable<BotConfig>> includes = null);
    }
}
