using Discord.Commands;
using WizBot.Classes;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WizBot.Modules.Games.Commands
{
    class BetrayGame : DiscordCommand
    {
        public BetrayGame(DiscordModule module) : base(module) { }

        private enum Answers
        {
            Cooperate,
            Betray
        }
        internal override void Init(CommandGroupBuilder cgb)
        {
            cgb.CreateCommand(Module.Prefix + "betray")
                .Description("BETRAY GAME. Betray WizBot next turn." +
                             "If WizBot cooperates - you get extra points, WizBot loses a LOT." +
                             $"If WizBot betrays - you both lose some points. | `{Prefix}betray`")
                .Do(async e =>
                {
                    await ReceiveAnswer(e, Answers.Betray).ConfigureAwait(false);
                });

            cgb.CreateCommand(Module.Prefix + "cooperate")
                .Description("BETRAY GAME. Cooperate with WizBot next turn." +
                             "If WizBot cooperates - you both get bonus points." +
                             $"If WizBot betrays - you lose A LOT, WizBot gets extra. | `{Prefix}cooperater`")
                .Do(async e =>
                {

                    await ReceiveAnswer(e, Answers.Cooperate).ConfigureAwait(false);
                });
        }

        private int userPoints = 0;

        private int UserPoints {
            get { return userPoints; }
            set {
                if (value < 0)
                    userPoints = 0;
                userPoints = value;
            }
        }
        private int WizPoints = 0;
        private int wizPoints {
            get { return wizPoints; }
            set {
                if (value < 0)
                    wizPoints = 0;
                wizPoints = value;
            }
        }

        private int round = 0;
        private Answers NextAnswer = Answers.Cooperate;
        private async Task ReceiveAnswer(CommandEventArgs e, Answers userAnswer)
        {
            var response = userAnswer == Answers.Betray
                ? ":no_entry: `You betrayed WizBot` - you monster."
                : ":ok: `You cooperated with WizBot.` ";
            var currentAnswer = NextAnswer;
            var wizResponse = currentAnswer == Answers.Betray
                ? ":no_entry: `aww WizBot betrayed you` - she is so cute"
                : ":ok: `WizBot cooperated.`";
            NextAnswer = userAnswer;
            if (userAnswer == Answers.Betray && currentAnswer == Answers.Betray)
            {
                WizPoints--;
                UserPoints--;
            }
            else if (userAnswer == Answers.Cooperate && currentAnswer == Answers.Cooperate)
            {
                WizPoints += 2;
                UserPoints += 2;
            }
            else if (userAnswer == Answers.Betray && currentAnswer == Answers.Cooperate)
            {
                WizPoints -= 3;
                UserPoints += 3;
            }
            else if (userAnswer == Answers.Cooperate && currentAnswer == Answers.Betray)
            {
                WizPoints += 3;
                UserPoints -= 3;
            }

            await e.Channel.SendMessage($"**ROUND {++round}**\n" +
                                        $"{response}\n" +
                                        $"{wizResponse}\n" +
                                        $"--------------------------------\n" +
                                        $"WizBot has {WizPoints} points." +
                                        $"You have {UserPoints} points." +
                                        $"--------------------------------\n")
                                            .ConfigureAwait(false);
            if (round < 10) return;
            if (wizPoints == userPoints)
                await e.Channel.SendMessage("Its a draw").ConfigureAwait(false);
            else if (wizPoints > userPoints)
                await e.Channel.SendMessage("WizBot won.").ConfigureAwait(false);
            else
                await e.Channel.SendMessage("You won.").ConfigureAwait(false);
            wizPoints = 0;
            userPoints = 0;
            round = 0;
        }
    }

    public class BetraySetting
    {
        private string Story = $"{0} have robbed a bank and got captured by a police." +
                               $"Investigators gave you a choice:\n" +
                               $"You can either >COOPERATE with your friends and " +
                               $"not tell them who's idea it was, OR you can >BETRAY your" +
                               $"friends. Depending on their answers your penalty will vary.";

        public int DoubleCoop = 1;
        public int DoubleBetray = -1;
        public int BetrayCoop_Betrayer = 3;
        public int BetrayCoop_Cooperater = -3;

        public string GetStory(IEnumerable<string> names) => String.Format(Story, string.Join(", ", names));
    }
}
