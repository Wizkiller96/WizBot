using WizBot.Common;
using WizBot.Core.Services.Database.Models;

namespace WizBot.Core.Services
{
    public interface IBotConfigProvider
    {
        BotConfig BotConfig { get; }
        void Reload();
        bool Edit(BotConfigEditType type, string newValue);
        string GetValue(string name);
    }
}
