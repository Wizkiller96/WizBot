using System.Threading.Tasks;
using NadekoBot.Services;
using System.Collections.Concurrent;
using NadekoBot.Modules.Gambling.Common.AnimalRacing;

namespace NadekoBot.Modules.Gambling.Services
{
    public class AnimalRaceService : INService
    {
        public ConcurrentDictionary<ulong, AnimalRace> AnimalRaces { get; } = new ConcurrentDictionary<ulong, AnimalRace>();
    }
}
