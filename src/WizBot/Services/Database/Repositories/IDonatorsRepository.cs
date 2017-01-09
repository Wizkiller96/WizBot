using WizBot.Services.Database.Models;
using System.Collections.Generic;

namespace WizBot.Services.Database.Repositories
{
    public interface IDonatorsRepository : IRepository<Donator>
    {
        IEnumerable<Donator> GetDonatorsOrdered();
        Donator AddOrUpdateDonator(ulong userId, string name, int amount);
    }
}
