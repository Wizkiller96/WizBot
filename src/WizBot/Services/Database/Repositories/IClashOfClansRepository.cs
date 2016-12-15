using WizBot.Services.Database.Models;
using System.Collections.Generic;

namespace WizBot.Services.Database.Repositories
{
    public interface IClashOfClansRepository : IRepository<ClashWar>
    {
        IEnumerable<ClashWar> GetAllWars();
    }
}
