using WizBot.Core.Services.Database.Models;

namespace WizBot.Core.Services.Database.Repositories
{
    public interface IPlantedCurrencyRepository : IRepository<PlantedCurrency>
    {
        (long Sum, ulong[] MessageIds) RemoveSumAndGetMessageIdsFor(ulong cid, string pass);
    }
}