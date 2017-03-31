using Microsoft.EntityFrameworkCore;
using WizBot.Services.Database.Models;
using System;
using System.Linq;

namespace WizBot.Services.Database.Repositories
{
    public interface IBotConfigRepository : IRepository<BotConfig>
    {
        BotConfig GetOrCreate(Func<DbSet<BotConfig>, IQueryable<BotConfig>> includes = null);
    }
}