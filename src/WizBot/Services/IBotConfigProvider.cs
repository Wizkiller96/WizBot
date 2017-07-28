using WizBot.Common;
using WizBot.Services.Database.Models;

namespace WizBot.Services
{
    public interface IBotConfigProvider
    {
        BotConfig BotConfig { get; }
        void Reload();
        bool Edit(BotConfigEditType type, string newValue);
    }
}