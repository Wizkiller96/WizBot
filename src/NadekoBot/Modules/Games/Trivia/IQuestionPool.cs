namespace NadekoBot.Modules.Games.Common.Trivia;

public interface IQuestionPool
{
    Task<TriviaQuestion?> GetRandomQuestionAsync(ISet<TriviaQuestion> exclude);
}