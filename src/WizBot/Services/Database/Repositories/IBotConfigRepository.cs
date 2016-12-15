using WizBot.Services.Database.Models;

namespace WizBot.Services.Database.Repositories
{
    public interface IBotConfigRepository : IRepository<BotConfig>
    {
        BotConfig GetOrCreate();
    }
}
