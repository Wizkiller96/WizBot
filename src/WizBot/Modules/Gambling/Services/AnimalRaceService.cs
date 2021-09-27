using System.Threading.Tasks;
using WizBot.Services;
using System.Collections.Concurrent;
using WizBot.Modules.Gambling.Common.AnimalRacing;

namespace WizBot.Modules.Gambling.Services
{
    public class AnimalRaceService : INService
    {
        public ConcurrentDictionary<ulong, AnimalRace> AnimalRaces { get; } = new ConcurrentDictionary<ulong, AnimalRace>();
    }
}
