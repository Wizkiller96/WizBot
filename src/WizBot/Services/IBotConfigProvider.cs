using WizBot.Common;
using WizBot.Services.Database.Models;

namespace WizBot.Services
{
    public interface IBotConfigProvider : INService
    {
        BotConfig BotConfig { get; }
        void Reload();
        bool Edit(BotConfigEditType type, string newValue);
    }
}