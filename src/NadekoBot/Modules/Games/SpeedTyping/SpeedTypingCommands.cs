#nullable disable
using NadekoBot.Modules.Games.Common;
using NadekoBot.Modules.Games.Services;

namespace NadekoBot.Modules.Games;

public partial class Games
{
    [Group]
    public partial class SpeedTypingCommands : NadekoModule<GamesService>
    {
        private readonly GamesService _games;
        private readonly DiscordSocketClient _client;

        public SpeedTypingCommands(DiscordSocketClient client, GamesService games)
        {
            _games = games;
            _client = client;
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [NadekoOptionsAttribute(typeof(TypingGame.Options))]
        public async partial Task TypeStart(params string[] args)
        {
            var (options, _) = OptionsParser.ParseFrom(new TypingGame.Options(), args);
            var channel = (ITextChannel)ctx.Channel;

            var game = _service.RunningContests.GetOrAdd(ctx.Guild.Id,
                _ => new(_games, _client, channel, prefix, options, _eb));

            if (game.IsActive)
                await SendErrorAsync($"Contest already running in {game.Channel.Mention} channel.");
            else
                await game.Start();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async partial Task TypeStop()
        {
            if (_service.RunningContests.TryRemove(ctx.Guild.Id, out var game))
            {
                await game.Stop();
                return;
            }

            await SendErrorAsync("No contest to stop on this channel.");
        }


        [Cmd]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async partial Task Typeadd([Leftover] string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            _games.AddTypingArticle(ctx.User, text);

            await SendConfirmAsync("Added new article for typing game.");
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async partial Task Typelist(int page = 1)
        {
            if (page < 1)
                return;

            var articles = _games.TypingArticles.Skip((page - 1) * 15).Take(15).ToArray();

            if (!articles.Any())
            {
                await SendErrorAsync($"{ctx.User.Mention} `No articles found on that page.`");
                return;
            }

            var i = (page - 1) * 15;
            await SendConfirmAsync("List of articles for Type Race",
                string.Join("\n", articles.Select(a => $"`#{++i}` - {a.Text.TrimTo(50)}")));
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async partial Task Typedel(int index)
        {
            var removed = _service.RemoveTypingArticle(--index);

            if (removed is null)
                return;

            var embed = _eb.Create()
                           .WithTitle($"Removed typing article #{index + 1}")
                           .WithDescription(removed.Text.TrimTo(50))
                           .WithOkColor();

            await ctx.Channel.EmbedAsync(embed);
        }
    }
}