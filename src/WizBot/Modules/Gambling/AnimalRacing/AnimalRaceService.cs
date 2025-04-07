#nullable disable
using WizBot.Modules.Gambling.Common.AnimalRacing;

namespace WizBot.Modules.Gambling.Services;

public class AnimalRaceService : INService
{
    public ConcurrentDictionary<ulong, AnimalRace> AnimalRaces { get; } = new();
}