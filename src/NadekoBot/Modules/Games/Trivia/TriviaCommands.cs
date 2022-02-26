#nullable disable
using NadekoBot.Modules.Games.Common.Trivia;
using NadekoBot.Modules.Games.Services;

namespace NadekoBot.Modules.Games;

public partial class Games
{
    [Group]
    public partial class TriviaCommands : NadekoModule<GamesService>
    {
        private readonly IDataCache _cache;
        private readonly ICurrencyService _cs;
        private readonly GamesConfigService _gamesConfig;
        private readonly DiscordSocketClient _client;

        public TriviaCommands(
            DiscordSocketClient client,
            IDataCache cache,
            ICurrencyService cs,
            GamesConfigService gamesConfig)
        {
            _cache = cache;
            _cs = cs;
            _gamesConfig = gamesConfig;
            _client = client;
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [Priority(0)]
        [NadekoOptionsAttribute(typeof(TriviaOptions))]
        public partial Task Trivia(params string[] args)
            => InternalTrivia(args);

        private async Task InternalTrivia(params string[] args)
        {
            var channel = (ITextChannel)ctx.Channel;

            var (opts, _) = OptionsParser.ParseFrom(new TriviaOptions(), args);

            var config = _gamesConfig.Data;
            if (config.Trivia.MinimumWinReq > 0 && config.Trivia.MinimumWinReq > opts.WinRequirement)
                return;

            var trivia = new TriviaGame(Strings,
                _client,
                config,
                _cache,
                _cs,
                channel.Guild,
                channel,
                opts,
                prefix + "tq",
                _eb);
            if (_service.RunningTrivias.TryAdd(channel.Guild.Id, trivia))
            {
                try
                {
                    await trivia.StartGame();
                }
                finally
                {
                    _service.RunningTrivias.TryRemove(channel.Guild.Id, out trivia);
                    await trivia.EnsureStopped();
                }

                return;
            }

            await SendErrorAsync(GetText(strs.trivia_already_running) + "\n" + trivia.CurrentQuestion);
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async partial Task Tl()
        {
            if (_service.RunningTrivias.TryGetValue(ctx.Guild.Id, out var trivia))
            {
                await SendConfirmAsync(GetText(strs.leaderboard), trivia.GetLeaderboard());
                return;
            }

            await ReplyErrorLocalizedAsync(strs.trivia_none);
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async partial Task Tq()
        {
            var channel = (ITextChannel)ctx.Channel;

            if (_service.RunningTrivias.TryGetValue(channel.Guild.Id, out var trivia))
            {
                await trivia.StopGame();
                return;
            }

            await ReplyErrorLocalizedAsync(strs.trivia_none);
        }
    }
}