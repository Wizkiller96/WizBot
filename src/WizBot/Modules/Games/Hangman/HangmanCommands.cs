using WizBot.Modules.Games.Hangman;

namespace WizBot.Modules.Games;

public partial class Games
{
    [Group]
    public partial class HangmanCommands : WizModule<IHangmanService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Hangmanlist()
            => await Response()
                .Confirm(GetText(strs.hangman_types(prefix)), _service.GetHangmanTypes().Join('\n'))
                .SendAsync();

        private static string Draw(HangmanGame.State state)
        {
            var head = state.Errors >= 1 ? "O" : " ";
            var torso = state.Errors >= 2 ? "|" : " ";
            var leftArm = state.Errors >= 3 ? "/" : " ";
            var rightArm = state.Errors >= 4 ? "\\" : " ";
            var leftLeg = state.Errors >= 5 ? "/" : " ";
            var rightLeg = state.Errors >= 6 ? "\\" : " ";

            return $"""
                    ```
                     ┌─────┐
                     │     {head}
                     │     {leftArm}{torso}{rightArm}
                     │      {leftLeg} {rightLeg}
                    ─┴─
                    ```
                    """;
        }

        public static EmbedBuilder GetEmbed(IMessageSenderService sender, HangmanGame.State state)
        {
            var eb = sender.CreateEmbed()
                .WithOkColor()
                .AddField("Hangman", Draw(state))
                .AddField("Guess", Format.Code(state.Word));

            if (state.Phase == HangmanGame.Phase.Running)
            {
                return eb
                    .WithFooter(state.MissedLetters.Join(' '))
                    .WithAuthor(state.Category);
            }

            if (state.Phase == HangmanGame.Phase.Ended && state.Failed)
            {
                return eb
                    .WithFooter(state.MissedLetters.Join(' '));
            }

            return eb.WithFooter(state.MissedLetters.Join(' '));
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Hangman([Leftover] string? type = null)
        {
            if (!_service.StartHangman(ctx.Channel.Id, type, out var hangman))
            {
                await Response().Error(strs.hangman_running).SendAsync();
                return;
            }

            var eb = GetEmbed(_sender, hangman);
            eb.WithDescription(GetText(strs.hangman_game_started));
            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task HangmanStop()
        {
            if (await _service.StopHangman(ctx.Channel.Id))
                await Response().Confirm(strs.hangman_stopped).SendAsync();
        }
    }
}