using Discord;
using WizBot.Common;
using WizBot.Core.Services;
using WizBot.Core.Services.Impl;
using WizBot.Extensions;
using WizBot.Modules.Games.Common;
using WizBot.Modules.Games.Common.Acrophobia;
using WizBot.Modules.Games.Common.Hangman;
using WizBot.Modules.Games.Common.Nunchi;
using WizBot.Modules.Games.Common.Trivia;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WizBot.Modules.Games.Services
{
    public class GamesService : INService, IUnloadableService
    {
        private readonly IBotConfigProvider _bc;

        public ConcurrentDictionary<ulong, GirlRating> GirlRatings { get; } = new ConcurrentDictionary<ulong, GirlRating>();

        public ImmutableArray<string> EightBallResponses { get; }

        private readonly Timer _t;
        private readonly CommandHandler _cmd;
        private readonly WizBotStrings _strings;
        private readonly IImageCache _images;
        private readonly Logger _log;
        private readonly WizBotRandom _rng;
        private readonly ICurrencyService _cs;
        private readonly FontProvider _fonts;

        public string TypingArticlesPath { get; } = "data/typing_articles3.json";
        private readonly CommandHandler _cmdHandler;

        public List<TypingArticle> TypingArticles { get; } = new List<TypingArticle>();

        //channelId, game
        public ConcurrentDictionary<ulong, AcrophobiaGame> AcrophobiaGames { get; } = new ConcurrentDictionary<ulong, AcrophobiaGame>();

        public ConcurrentDictionary<ulong, Hangman> HangmanGames { get; } = new ConcurrentDictionary<ulong, Hangman>();
        public TermPool TermPool { get; } = new TermPool();

        public ConcurrentDictionary<ulong, TriviaGame> RunningTrivias { get; } = new ConcurrentDictionary<ulong, TriviaGame>();
        public Dictionary<ulong, TicTacToe> TicTacToeGames { get; } = new Dictionary<ulong, TicTacToe>();
        public ConcurrentDictionary<ulong, TypingGame> RunningContests { get; } = new ConcurrentDictionary<ulong, TypingGame>();
        public ConcurrentDictionary<ulong, NunchiGame> NunchiGames { get; } = new ConcurrentDictionary<ulong, Common.Nunchi.NunchiGame>();

        public GamesService(CommandHandler cmd, IBotConfigProvider bc, WizBot bot,
            WizBotStrings strings, IDataCache data, CommandHandler cmdHandler,
            ICurrencyService cs, FontProvider fonts)
        {
            _bc = bc;
            _cmd = cmd;
            _strings = strings;
            _images = data.LocalImages;
            _cmdHandler = cmdHandler;
            _log = LogManager.GetCurrentClassLogger();
            _rng = new WizBotRandom();
            _cs = cs;
            _fonts = fonts;

            //8ball
            EightBallResponses = _bc.BotConfig.EightBallResponses.Select(ebr => ebr.Text).ToImmutableArray();

            //girl ratings
            _t = new Timer((_) =>
            {
                GirlRatings.Clear();

            }, null, TimeSpan.FromDays(1), TimeSpan.FromDays(1));

            try
            {
                TypingArticles = JsonConvert.DeserializeObject<List<TypingArticle>>(File.ReadAllText(TypingArticlesPath));
            }
            catch (Exception ex)
            {
                _log.Warn("Error while loading typing articles {0}", ex.ToString());
                TypingArticles = new List<TypingArticle>();
            }
        }

        public async Task Unload()
        {
            _t.Change(Timeout.Infinite, Timeout.Infinite);

            AcrophobiaGames.ForEach(x => x.Value.Dispose());
            AcrophobiaGames.Clear();
            HangmanGames.ForEach(x => x.Value.Dispose());
            HangmanGames.Clear();
            await Task.WhenAll(RunningTrivias.Select(x => x.Value.StopGame())).ConfigureAwait(false);
            RunningTrivias.Clear();

            TicTacToeGames.Clear();

            await Task.WhenAll(RunningContests.Select(x => x.Value.Stop()))
                .ConfigureAwait(false);
            RunningContests.Clear();
            NunchiGames.ForEach(x => x.Value.Dispose());
            NunchiGames.Clear();
        }

        private void DisposeElems(IEnumerable<IDisposable> xs)
        {
            xs.ForEach(x => x.Dispose());
        }

        public void AddTypingArticle(IUser user, string text)
        {
            TypingArticles.Add(new TypingArticle
            {
                Source = user.ToString(),
                Extra = $"Text added on {DateTime.UtcNow} by {user}.",
                Text = text.SanitizeMentions(),
            });

            File.WriteAllText(TypingArticlesPath, JsonConvert.SerializeObject(TypingArticles));
        }
        private ConcurrentDictionary<ulong, object> _locks { get; } = new ConcurrentDictionary<ulong, object>();

        private string GetText(ITextChannel ch, string key, params object[] rep)
            => _strings.GetText(key, ch.GuildId, "Games".ToLowerInvariant(), rep);
    }
}
