using Discord;
using Discord.Commands;
using Discord.WebSocket;
using WizBot.Extensions;
using WizBot.Core.Services;
using System.Threading.Tasks;
using WizBot.Common.Attributes;
using WizBot.Modules.Games.Common.Trivia;
using WizBot.Modules.Games.Services;

namespace WizBot.Modules.Games
{
    public partial class Games
    {
        [Group]
        public class TriviaCommands : WizBotSubmodule<GamesService>
        {
            private readonly CurrencyService _cs;
            private readonly DiscordSocketClient _client;
            private readonly IBotConfigProvider _bc;

            public TriviaCommands(DiscordSocketClient client, IBotConfigProvider bc, CurrencyService cs)
            {
                _cs = cs;
                _client = client;
                _bc = bc;
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public Task Trivia([Remainder] string additionalArgs = "")
                => InternalTrivia(10, additionalArgs);

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public Task Trivia(int winReq = 10, [Remainder] string additionalArgs = "")
                => InternalTrivia(winReq, additionalArgs);

            public async Task InternalTrivia(int winReq, string additionalArgs = "")
            {
                var channel = (ITextChannel)Context.Channel;

                additionalArgs = additionalArgs?.Trim()?.ToLowerInvariant();

                var showHints = !additionalArgs.Contains("nohint");
                var isPokemon = additionalArgs.Contains("pokemon");

                var trivia = new TriviaGame(_strings, _client, _bc, _cs, channel.Guild, channel, showHints, winReq, isPokemon);
                if (_service.RunningTrivias.TryAdd(channel.Guild.Id, trivia))
                {
                    try
                    {
                        await trivia.StartGame().ConfigureAwait(false);
                    }
                    finally
                    {
                        _service.RunningTrivias.TryRemove(channel.Guild.Id, out trivia);
                        await trivia.EnsureStopped().ConfigureAwait(false);
                    }
                    return;
                }

                await Context.Channel.SendErrorAsync(GetText("trivia_already_running") + "\n" + trivia.CurrentQuestion)
                    .ConfigureAwait(false);
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Tl()
            {
                var channel = (ITextChannel)Context.Channel;

                if (_service.RunningTrivias.TryGetValue(channel.Guild.Id, out TriviaGame trivia))
                {
                    await channel.SendConfirmAsync(GetText("leaderboard"), trivia.GetLeaderboard()).ConfigureAwait(false);
                    return;
                }

                await ReplyErrorLocalized("trivia_none").ConfigureAwait(false);
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Tq()
            {
                var channel = (ITextChannel)Context.Channel;

                if (_service.RunningTrivias.TryGetValue(channel.Guild.Id, out TriviaGame trivia))
                {
                    await trivia.StopGame().ConfigureAwait(false);
                    return;
                }

                await ReplyErrorLocalized("trivia_none").ConfigureAwait(false);
            }
        }
    }
}
