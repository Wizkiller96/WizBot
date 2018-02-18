using WizBot.Core.Modules.Gambling.Common.Blackjack;
using WizBot.Core.Services;
using System.Collections.Concurrent;

namespace WizBot.Core.Modules.Gambling.Services
{
    public class BlackJackService : INService
    {
        public ConcurrentDictionary<ulong, Blackjack> Games { get; } = new ConcurrentDictionary<ulong, Blackjack>();
    }
}
