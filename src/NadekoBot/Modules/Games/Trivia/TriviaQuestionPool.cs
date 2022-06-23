namespace NadekoBot.Modules.Games.Common.Trivia;

public class TriviaQuestionPool
{
    private readonly ILocalDataCache _cache;
    private readonly int _maxPokemonId;

    private readonly NadekoRandom _rng = new();

    public TriviaQuestionPool(ILocalDataCache cache)
    {
        _cache = cache;
        _maxPokemonId = 721; //xd
    }

    public async Task<TriviaQuestion?> GetRandomQuestionAsync(HashSet<TriviaQuestion> exclude, bool isPokemon)
    {
        if (isPokemon)
        {
            var pokes = await _cache.GetPokemonMapAsync();

            if (pokes is null or { Length: 0 })
                return default;
            
            var num = _rng.Next(1, _maxPokemonId + 1);
            return new(new()
            {
                Question = "Who's That Pokémon?",
                Answer = pokes[num].Name.ToTitleCase(),
                Category = "Pokemon",
                ImageUrl = $@"https://nadeko.bot/images/pokemon/shadows/{num}.png",
                AnswerImageUrl = $@"https://nadeko.bot/images/pokemon/real/{num}.png"
            });
        }

        TriviaQuestion randomQuestion;
        var pool = await _cache.GetTriviaQuestionsAsync();
        
        if(pool is null)
            return default;
        
        while (exclude.Contains(randomQuestion = new(pool[_rng.Next(0, pool.Length)])))
        {
            // if too many questions are excluded, clear the exclusion list and start over
            if (exclude.Count > pool.Length / 10 * 9)
            {
                exclude.Clear();
                break;
            }
        }

        return randomQuestion;
    }
}