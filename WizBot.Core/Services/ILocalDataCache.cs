using WizBot.Core.Common.Pokemon;
using WizBot.Modules.Games.Common.Trivia;
using System.Collections.Generic;

namespace WizBot.Core.Services
{
    public interface ILocalDataCache
    {
        IReadOnlyDictionary<string, SearchPokemon> Pokemons { get; }
        IReadOnlyDictionary<string, SearchPokemonAbility> PokemonAbilities { get; }
        TriviaQuestion[] TriviaQuestions { get; }
        IReadOnlyDictionary<int, string> PokemonMap { get; }
    }
}