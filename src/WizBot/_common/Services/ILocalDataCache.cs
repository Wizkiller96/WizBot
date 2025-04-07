#nullable disable
using WizBot.Common.Pokemon;
using WizBot.Modules.Games.Common.Trivia;

namespace WizBot.Services;

public interface ILocalDataCache
{
    Task<IReadOnlyDictionary<string, SearchPokemon>> GetPokemonsAsync();
    Task<IReadOnlyDictionary<string, SearchPokemonAbility>> GetPokemonAbilitiesAsync();
    Task<TriviaQuestionModel[]> GetTriviaQuestionsAsync();
    Task<IReadOnlyDictionary<int, string>> GetPokemonMapAsync();
}