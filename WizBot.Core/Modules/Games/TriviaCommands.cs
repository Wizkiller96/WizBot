using Discord;
using Discord.Commands;
using Discord.WebSocket;
using WizBot.Extensions;
using WizBot.Core.Services;
using System.Threading.Tasks;
using WizBot.Common.Attributes;
using WizBot.Modules.Games.Common.Trivia;
using WizBot.Modules.Games.Services;
using WizBot.Core.Common;
using WizBot.Core.Modules.Games.Common.Trivia;

namespace WizBot.Modules.Games
{
    public partial class Games
    {
        [Group]
        public class TriviaCommands : WizBotSubmodule<GamesService>
        {
            private readonly IDataCache _cache;
            private readonly ICurrencyService _cs;
            private readonly DiscordSocketClient _client;

            public TriviaCommands(DiscordSocketClient client, IDataCache cache, ICurrencyService cs)
            {
                _cache = cache;
                _cs = cs;
                _client = client;
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [Priority(0)]
            [WizBotOptionsAttribute(typeof(TriviaOptions))]
            public Task Trivia(params string[] args)
                => InternalTrivia(args);

            public async Task InternalTrivia(params string[] args)
            {
                var channel = (ITextChannel)ctx.Channel;

                var (opts, _) = OptionsParser.ParseFrom(new TriviaOptions(), args);

                if (Bc.BotConfig.MinimumTriviaWinReq > 0 && Bc.BotConfig.MinimumTriviaWinReq > opts.WinRequirement)
                {
                    return;
                }
                var trivia = new TriviaGame(Strings, _client, Bc, _cache, _cs, channel.Guild, channel, opts, Prefix + "tq");
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

                await ctx.Channel.SendErrorAsync(GetText("trivia_already_running") + "\n" + trivia.CurrentQuestion)
                    .ConfigureAwait(false);
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Tl()
            {
                var channel = (ITextChannel)ctx.Channel;

                if (_service.RunningTrivias.TryGetValue(channel.Guild.Id, out TriviaGame trivia))
                {
                    await channel.SendConfirmAsync(GetText("leaderboard"), trivia.GetLeaderboard()).ConfigureAwait(false);
                    return;
                }

                await ReplyErrorLocalizedAsync("trivia_none").ConfigureAwait(false);
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Tq()
            {
                var channel = (ITextChannel)ctx.Channel;

                if (_service.RunningTrivias.TryGetValue(channel.Guild.Id, out TriviaGame trivia))
                {
                    await trivia.StopGame().ConfigureAwait(false);
                    return;
                }

                await ReplyErrorLocalizedAsync("trivia_none").ConfigureAwait(false);
            }
        }
    }
}