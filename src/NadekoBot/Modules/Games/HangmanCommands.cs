#nullable enable
using Discord.Commands;
using System.Threading.Tasks;
using Discord;
using NadekoBot.Common.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Modules.Games.Hangman;
using NadekoBot.Services;

namespace NadekoBot.Modules.Games
{
    public partial class Games
    {
        [Group]
        public class HangmanCommands : NadekoSubmodule<IHangmanService>
        {
            [NadekoCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Hangmanlist()
            {
                await SendConfirmAsync(
                    GetText(strs.hangman_types(Prefix)),
                    _service.GetHangmanTypes().JoinWith('\n'));
            }

            private static string Draw(HangmanGame.State state)
            {
                return $@". ┌─────┐
.┃...............┋
.┃...............┋
.┃{(state.Errors > 0 ? ".............😲" : "")}
.┃{(state.Errors > 1 ? "............./" : "")} {(state.Errors > 2 ? "|" : "")} {(state.Errors > 3 ? "\\" : "")}
.┃{(state.Errors > 4 ? "............../" : "")} {(state.Errors > 5 ? "\\" : "")}
/-\";
            }

            public static IEmbedBuilder GetEmbed(IEmbedBuilderService eb, HangmanGame.State state)
            {
                if (state.Phase == HangmanGame.Phase.Running)
                    return eb.Create()
                        .WithOkColor()
                        .AddField("Hangman", Draw(state))
                        .AddField("Guess", Format.Code(state.Word))
                        .WithFooter(state.missedLetters.JoinWith(' '));
                
                if (state.Phase == HangmanGame.Phase.Ended && state.Failed)
                    return eb.Create()
                        .WithErrorColor()
                        .AddField("Hangman", Draw(state))
                        .AddField("Guess", Format.Code(state.Word))
                        .WithFooter(state.missedLetters.JoinWith(' '));
                else
                {
                    return eb.Create()
                        .WithOkColor()
                        .AddField("Hangman", Draw(state))
                        .AddField("Guess", Format.Code(state.Word))
                        .WithFooter(state.missedLetters.JoinWith(' '));
                }
            }

            [NadekoCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Hangman([Leftover] string? type = null)
            {
                if (!_service.StartHangman(ctx.Channel.Id, type, out var hangman))
                {
                    await ReplyErrorLocalizedAsync(strs.hangman_running);
                    return;
                }

                var eb = GetEmbed(_eb, hangman);
                eb.WithDescription(GetText(strs.hangman_game_started));
                await ctx.Channel.EmbedAsync(eb);
            }

            [NadekoCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task HangmanStop()
            {
                if (await _service.StopHangman(ctx.Channel.Id))
                {
                    await ReplyConfirmLocalizedAsync(strs.hangman_stopped).ConfigureAwait(false);
                }
            }
        }
    }
}
