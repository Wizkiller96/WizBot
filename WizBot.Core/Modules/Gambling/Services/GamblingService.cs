using WizBot.Core.Modules.Gambling.Common;
using WizBot.Core.Services;
using System.Collections.Concurrent;

namespace WizBot.Modules.Gambling.Services
{
    public class GamblingService : INService
    {
        public ConcurrentDictionary<(ulong, ulong), RollDuelGame> Duels { get; } = new ConcurrentDictionary<(ulong, ulong), RollDuelGame>();
    }
}