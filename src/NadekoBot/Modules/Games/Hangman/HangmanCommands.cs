using NadekoBot.Modules.Games.Hangman;

namespace NadekoBot.Modules.Games;

public partial class Games
{
    [Group]
    public partial class HangmanCommands : NadekoModule<IHangmanService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async partial Task Hangmanlist()
            => await SendConfirmAsync(GetText(strs.hangman_types(prefix)), _service.GetHangmanTypes().Join('\n'));

        private static string Draw(HangmanGame.State state)
            => $@". ┌─────┐
.┃...............┋
.┃...............┋
.┃{(state.Errors > 0 ? ".............😲" : "")}
.┃{(state.Errors > 1 ? "............./" : "")} {(state.Errors > 2 ? "|" : "")} {(state.Errors > 3 ? "\\" : "")}
.┃{(state.Errors > 4 ? "............../" : "")} {(state.Errors > 5 ? "\\" : "")}
/-\";

        public static IEmbedBuilder GetEmbed(IEmbedBuilderService eb, HangmanGame.State state)
        {
            if (state.Phase == HangmanGame.Phase.Running)
            {
                return eb.Create()
                         .WithOkColor()
                         .AddField("Hangman", Draw(state))
                         .AddField("Guess", Format.Code(state.Word))
                         .WithFooter(state.MissedLetters.Join(' '));
            }

            if (state.Phase == HangmanGame.Phase.Ended && state.Failed)
            {
                return eb.Create()
                         .WithErrorColor()
                         .AddField("Hangman", Draw(state))
                         .AddField("Guess", Format.Code(state.Word))
                         .WithFooter(state.MissedLetters.Join(' '));
            }

            return eb.Create()
                     .WithOkColor()
                     .AddField("Hangman", Draw(state))
                     .AddField("Guess", Format.Code(state.Word))
                     .WithFooter(state.MissedLetters.Join(' '));
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async partial Task Hangman([Leftover] string? type = null)
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

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async partial Task HangmanStop()
        {
            if (await _service.StopHangman(ctx.Channel.Id))
                await ReplyConfirmLocalizedAsync(strs.hangman_stopped);
        }
    }
}