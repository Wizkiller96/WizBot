using WizBot.Common.Attributes;
using WizBot.Core.Modules.Gambling.Services;
using System.Threading.Tasks;

namespace WizBot.Modules.Gambling
{
    public partial class Gambling
    {
        public class CurrencyRaffleCommands : WizBotSubmodule<CurrencyRaffleService>
        {
            [WizBotCommand, Usage, Description, Aliases]
            public async Task RaffleCur(int amount)
            {
                if (_service.Games.TryAdd(Context.Channel.Id,
                    ))
                {

                }
            }
        }
    }
}