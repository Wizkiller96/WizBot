#nullable disable
using WizBot.Common.Pokemon;
using WizBot.Modules.Games.Common.Trivia;

namespace WizBot.Services;

public interface ILocalDataCache
{
    IReadOnlyDictionary<string, SearchPokemon> Pokemons { get; }
    IReadOnlyDictionary<string, SearchPokemonAbility> PokemonAbilities { get; }
    IReadOnlyDictionary<int, string> PokemonMap { get; }
    TriviaQuestion[] TriviaQuestions { get; }
}