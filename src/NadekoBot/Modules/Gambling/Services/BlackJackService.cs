using NadekoBot.Modules.Gambling.Common.Blackjack;
using NadekoBot.Services;
using System.Collections.Concurrent;

namespace NadekoBot.Modules.Gambling.Services
{
    public class BlackJackService : INService
    {
        public ConcurrentDictionary<ulong, Blackjack> Games { get; } = new ConcurrentDictionary<ulong, Blackjack>();
    }
}
