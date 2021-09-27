using WizBot.Modules.Gambling.Common.Blackjack;
using WizBot.Services;
using System.Collections.Concurrent;

namespace WizBot.Modules.Gambling.Services
{
    public class BlackJackService : INService
    {
        public ConcurrentDictionary<ulong, Blackjack> Games { get; } = new ConcurrentDictionary<ulong, Blackjack>();
    }
}
