#nullable disable
using Microsoft.Extensions.Caching.Memory;
using NadekoBot.Common.ModuleBehaviors;
using NadekoBot.Modules.Games.Common;
using NadekoBot.Modules.Games.Common.Acrophobia;
using NadekoBot.Modules.Games.Common.Nunchi;
using NadekoBot.Modules.Games.Common.Trivia;
using Newtonsoft.Json;

namespace NadekoBot.Modules.Games.Services;

public class GamesService : INService, IReadyExecutor
{
    private const string TYPING_ARTICLES_PATH = "data/typing_articles3.json";

    public ConcurrentDictionary<ulong, GirlRating> GirlRatings { get; } = new();

    public IReadOnlyList<string> EightBallResponses
        => _gamesConfig.Data.EightBallResponses;

    public List<TypingArticle> TypingArticles { get; } = new();

    //channelId, game
    public ConcurrentDictionary<ulong, AcrophobiaGame> AcrophobiaGames { get; } = new();
    public ConcurrentDictionary<ulong, TriviaGame> RunningTrivias { get; } = new();
    public Dictionary<ulong, TicTacToe> TicTacToeGames { get; } = new();
    public ConcurrentDictionary<ulong, TypingGame> RunningContests { get; } = new();
    public ConcurrentDictionary<ulong, NunchiGame> NunchiGames { get; } = new();

    public AsyncLazy<RatingTexts> Ratings { get; }
    private readonly GamesConfigService _gamesConfig;

    private readonly IHttpClientFactory _httpFactory;
    private readonly IMemoryCache _8BallCache;
    private readonly Random _rng;

    public GamesService(GamesConfigService gamesConfig, IHttpClientFactory httpFactory)
    {
        _gamesConfig = gamesConfig;
        _httpFactory = httpFactory;
        _8BallCache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = 500_000
        });

        Ratings = new(GetRatingTexts);
        _rng = new NadekoRandom();

        try
        {
            TypingArticles = JsonConvert.DeserializeObject<List<TypingArticle>>(File.ReadAllText(TYPING_ARTICLES_PATH));
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error while loading typing articles: {ErrorMessage}", ex.Message);
            TypingArticles = new();
        }
    }

    public async Task OnReadyAsync()
    {
        // reset rating once a day
        using var timer = new PeriodicTimer(TimeSpan.FromDays(1));
        while (await timer.WaitForNextTickAsync())
            GirlRatings.Clear();
    }

    private async Task<RatingTexts> GetRatingTexts()
    {
        using var http = _httpFactory.CreateClient();
        var text = await http.GetStringAsync(
            "https://nadeko-pictures.nyc3.digitaloceanspaces.com/other/rategirl/rates.json");
        return JsonConvert.DeserializeObject<RatingTexts>(text);
    }

    public void AddTypingArticle(IUser user, string text)
    {
        TypingArticles.Add(new()
        {
            Source = user.ToString(),
            Extra = $"Text added on {DateTime.UtcNow} by {user}.",
            Text = text.SanitizeMentions(true)
        });

        File.WriteAllText(TYPING_ARTICLES_PATH, JsonConvert.SerializeObject(TypingArticles));
    }

    public string GetEightballResponse(ulong userId, string question)
        => _8BallCache.GetOrCreate($"8ball:{userId}:{question}",
            e =>
            {
                e.Size = question.Length;
                e.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12);
                return EightBallResponses[_rng.Next(0, EightBallResponses.Count)];
            });

    public TypingArticle RemoveTypingArticle(int index)
    {
        var articles = TypingArticles;
        if (index < 0 || index >= articles.Count)
            return null;

        var removed = articles[index];
        TypingArticles.RemoveAt(index);

        File.WriteAllText(TYPING_ARTICLES_PATH, JsonConvert.SerializeObject(articles));
        return removed;
    }

    public class RatingTexts
    {
        public string Nog { get; set; }
        public string Tra { get; set; }
        public string Fun { get; set; }
        public string Uni { get; set; }
        public string Wif { get; set; }
        public string Dat { get; set; }
        public string Dan { get; set; }
    }
}