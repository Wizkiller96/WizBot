#nullable disable
using WizBot.Modules.Gambling.Common.Blackjack;

namespace WizBot.Modules.Gambling.Services;

public class BlackJackService : INService
{
    public ConcurrentDictionary<ulong, Blackjack> Games { get; } = new();
}