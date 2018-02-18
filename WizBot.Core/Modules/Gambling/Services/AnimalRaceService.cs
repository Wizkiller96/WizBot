using System.Threading.Tasks;
using WizBot.Core.Services;
using System.Collections.Concurrent;
using WizBot.Modules.Gambling.Common.AnimalRacing;

namespace WizBot.Modules.Gambling.Services
{
    public class AnimalRaceService : INService, IUnloadableService
    {
        public ConcurrentDictionary<ulong, AnimalRace> AnimalRaces { get; } = new ConcurrentDictionary<ulong, AnimalRace>();

        public Task Unload()
        {
            foreach (var kvp in AnimalRaces)
            {
                try { kvp.Value.Dispose(); } catch { }
            }
            return Task.CompletedTask;
        }
    }
}
