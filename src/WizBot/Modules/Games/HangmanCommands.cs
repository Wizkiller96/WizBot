using Discord.Commands;
using WizBot.Extensions;
using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using WizBot.Common.Attributes;
using WizBot.Modules.Games.Common.Hangman;
using WizBot.Modules.Games.Services;
using WizBot.Modules.Games.Common.Hangman.Exceptions;
using WizBot.Services;

namespace WizBot.Modules.Games
{
    public partial class Games
    {
        [Group]
        public class HangmanCommands : WizBotSubmodule<GamesService>
        {
            private readonly DiscordSocketClient _client;
            private readonly ICurrencyService _cs;
            private readonly GamesConfigService _gcs;

            public HangmanCommands(DiscordSocketClient client, ICurrencyService cs, GamesConfigService gcs)
            {
                _client = client;
                _cs = cs;
                _gcs = gcs;
            }

            [WizBotCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Hangmanlist()
            {
                await SendConfirmAsync(Format.Code(GetText(strs.hangman_types(Prefix)) + "\n" + string.Join("\n", _service.TermPool.Data.Keys)));
            }

            [WizBotCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Hangman([Leftover]string type = "random")
            {
                Hangman hm;
                try
                {
                    hm = new Hangman(type, _service.TermPool);
                }
                catch (TermNotFoundException)
                {
                    return;
                }

                if (!_service.HangmanGames.TryAdd(ctx.Channel.Id, hm))
                {
                    hm.Dispose();
                    await ReplyErrorLocalizedAsync(strs.hangman_running).ConfigureAwait(false);
                    return;
                }
                hm.OnGameEnded += Hm_OnGameEnded;
                hm.OnGuessFailed += Hm_OnGuessFailed;
                hm.OnGuessSucceeded += Hm_OnGuessSucceeded;
                hm.OnLetterAlreadyUsed += Hm_OnLetterAlreadyUsed;
                _client.MessageReceived += _client_MessageReceived;

                try
                {
                    await SendConfirmAsync(GetText(strs.hangman_game_started) + $" ({hm.TermType})",
                        hm.ScrambledWord + "\n" + hm.GetHangman())
                        .ConfigureAwait(false);
                }
                catch { }

                await hm.EndedTask.ConfigureAwait(false);

                _client.MessageReceived -= _client_MessageReceived;
                _service.HangmanGames.TryRemove(ctx.Channel.Id, out _);
                hm.Dispose();

                Task _client_MessageReceived(SocketMessage msg)
                {
                    var _ = Task.Run(() =>
                    {
                        if (ctx.Channel.Id == msg.Channel.Id && !msg.Author.IsBot)
                            return hm.Input(msg.Author.Id, msg.Author.ToString(), msg.Content);
                        else
                            return Task.CompletedTask;
                    });
                    return Task.CompletedTask;
                }
            }

            Task Hm_OnGameEnded(Hangman game, string winner, ulong userId)
            {
                if (winner is null)
                {
                    var loseEmbed = _eb.Create().WithTitle($"Hangman Game ({game.TermType}) - Ended")
                                             .WithDescription(Format.Bold("You lose."))
                                             .AddField("It was", game.Term.GetWord())
                                             .WithFooter(string.Join(" ", game.PreviousGuesses))
                                             .WithErrorColor();

                    if (Uri.IsWellFormedUriString(game.Term.ImageUrl, UriKind.Absolute))
                        loseEmbed.WithImageUrl(game.Term.ImageUrl);

                    return ctx.Channel.EmbedAsync(loseEmbed);
                }

                var reward = _gcs.Data.Hangman.CurrencyReward;
                if (reward > 0)
                    _cs.AddAsync(userId, "hangman win", reward, true);
                
                var winEmbed = _eb.Create().WithTitle($"Hangman Game ({game.TermType}) - Ended")
                                             .WithDescription(Format.Bold($"{winner} Won."))
                                             .AddField("It was", game.Term.GetWord())
                                             .WithFooter(string.Join(" ", game.PreviousGuesses))
                                             .WithOkColor();

                if (Uri.IsWellFormedUriString(game.Term.ImageUrl, UriKind.Absolute))
                    winEmbed.WithImageUrl(game.Term.ImageUrl);

                return ctx.Channel.EmbedAsync(winEmbed);
            }

            private Task Hm_OnLetterAlreadyUsed(Hangman game, string user, char guess)
            {
                return SendErrorAsync($"Hangman Game ({game.TermType})", $"{user} Letter `{guess}` has already been used. You can guess again in 3 seconds.\n" + game.ScrambledWord + "\n" + game.GetHangman(),
                                    footer: string.Join(" ", game.PreviousGuesses));
            }

            private Task Hm_OnGuessSucceeded(Hangman game, string user, char guess)
            {
                return SendConfirmAsync($"Hangman Game ({game.TermType})", $"{user} guessed a letter `{guess}`!\n" + game.ScrambledWord + "\n" + game.GetHangman(),
                    footer: string.Join(" ", game.PreviousGuesses));
            }

            private Task Hm_OnGuessFailed(Hangman game, string user, char guess)
            {
                return SendErrorAsync($"Hangman Game ({game.TermType})", $"{user} Letter `{guess}` does not exist. You can guess again in 3 seconds.\n" + game.ScrambledWord + "\n" + game.GetHangman(),
                                    footer: string.Join(" ", game.PreviousGuesses));
            }

            [WizBotCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task HangmanStop()
            {
                if (_service.HangmanGames.TryRemove(ctx.Channel.Id, out var removed))
                {
                    await removed.Stop().ConfigureAwait(false);
                    await ReplyConfirmLocalizedAsync(strs.hangman_stopped).ConfigureAwait(false);
                }
            }
        }
    }
}
