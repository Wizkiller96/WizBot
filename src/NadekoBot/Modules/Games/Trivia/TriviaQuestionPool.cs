#nullable disable
namespace NadekoBot.Modules.Games.Common.Trivia;

public class TriviaQuestionPool
{
    private TriviaQuestion[] Pool
        => _cache.LocalData.TriviaQuestions;

    private IReadOnlyDictionary<int, string> Map
        => _cache.LocalData.PokemonMap;

    private readonly IDataCache _cache;
    private readonly int _maxPokemonId;

    private readonly NadekoRandom _rng = new();

    public TriviaQuestionPool(IDataCache cache)
    {
        _cache = cache;
        _maxPokemonId = 721; //xd
    }

    public TriviaQuestion GetRandomQuestion(HashSet<TriviaQuestion> exclude, bool isPokemon)
    {
        if (Pool.Length == 0)
            return null;

        if (isPokemon)
        {
            var num = _rng.Next(1, _maxPokemonId + 1);
            return new("Who's That Pokémon?",
                Map[num].ToTitleCase(),
                "Pokemon",
                $@"https://nadeko.bot/images/pokemon/shadows/{num}.png",
                $@"https://nadeko.bot/images/pokemon/real/{num}.png");
        }

        TriviaQuestion randomQuestion;
        while (exclude.Contains(randomQuestion = Pool[_rng.Next(0, Pool.Length)]))
        {
            // if too many questions are excluded, clear the exclusion list and start over
            if (exclude.Count > Pool.Length / 10 * 9)
            {
                exclude.Clear();
                break;
            }
        }

        return randomQuestion;
    }
}