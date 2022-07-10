namespace NadekoBot.Modules.Games.Common.Trivia;

public sealed class DefaultQuestionPool : IQuestionPool
{
    private readonly ILocalDataCache _cache;
    private readonly NadekoRandom _rng;

    public DefaultQuestionPool(ILocalDataCache cache)
    {
        _cache = cache;
        _rng = new NadekoRandom();
    }
    public async Task<TriviaQuestion?> GetRandomQuestionAsync(ISet<TriviaQuestion> exclude)
    {
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