namespace WizBot.Modules.Games.Common.Trivia;

public sealed class PokemonQuestionPool : IQuestionPool
{
    public int QuestionsCount => 905; // xd
    private readonly WizBotRandom _rng;
    private readonly ILocalDataCache _cache;

    public PokemonQuestionPool(ILocalDataCache cache)
    {
        _cache = cache;
        _rng = new WizBotRandom();
    }

    public async Task<TriviaQuestion?> GetQuestionAsync()
    {
        var pokes = await _cache.GetPokemonMapAsync();

        if (pokes is null or { Count: 0 })
            return default;
            
        var num = _rng.Next(1, QuestionsCount + 1);
        return new(new()
        {
            Question = "Who's That Pokémon?",
            Answer = pokes[num].ToTitleCase(),
            Category = "Pokemon",
            ImageUrl = $@"https://wizbot.cc/assets/pokemon/shadows/{num}.png",
            AnswerImageUrl = $@"https://wizbot.cc/assets/pokemon/real/{num}.png"
        });
    }
}