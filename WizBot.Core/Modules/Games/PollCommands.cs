using Discord;
using Discord.Commands;
using Discord.WebSocket;
using WizBot.Extensions;
using System.Threading.Tasks;
using WizBot.Common.Attributes;
using WizBot.Modules.Games.Services;
using WizBot.Core.Services.Database.Models;
using System.Text;
using System.Linq;

namespace WizBot.Modules.Games
{
    public partial class Games
    {
        [Group]
        public class PollCommands : WizBotSubmodule<PollService>
        {
            private readonly DiscordSocketClient _client;

            public PollCommands(DiscordSocketClient client)
            {
                _client = client;
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            [RequireContext(ContextType.Guild)]
            public async Task Poll([Remainder] string arg)
            {
                if (string.IsNullOrWhiteSpace(arg))
                    return;

                var poll = _service.CreatePoll(Context.Guild.Id,
                    Context.Channel.Id, arg);
                if (_service.StartPoll(poll))
                    await Context.Channel
                        .EmbedAsync(new EmbedBuilder()
                            .WithTitle(GetText("poll_created", Context.User.ToString()))
                            .WithDescription(
                                Format.Bold(poll.Question) + "\n\n" +
                            string.Join("\n", poll.Answers
                                .Select(x => $"`{x.Index + 1}.` {Format.Bold(x.Text)}"))))
                        .ConfigureAwait(false);
                else
                    await ReplyErrorLocalized("poll_already_running").ConfigureAwait(false);
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            [RequireContext(ContextType.Guild)]
            public async Task PollStats()
            {
                if (!_service.ActivePolls.TryGetValue(Context.Guild.Id, out var pr))
                    return;

                await Context.Channel.EmbedAsync(GetStats(pr.Poll, GetText("current_poll_results")));
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            [RequireContext(ContextType.Guild)]
            public async Task Pollend()
            {
                var channel = (ITextChannel)Context.Channel;

                Poll p;
                if ((p = _service.StopPoll(Context.Guild.Id)) == null)
                    return;

                var embed = GetStats(p, GetText("poll_closed"));
                await Context.Channel.EmbedAsync(embed)
                    .ConfigureAwait(false);
            }

            public EmbedBuilder GetStats(Poll poll, string title)
            {
                var results = poll.Votes.GroupBy(kvp => kvp.VoteIndex)
                                    .ToDictionary(x => x.Key, x => x.Sum(kvp => 1))
                                    .OrderByDescending(kvp => kvp.Value)
                                    .ToArray();

                var eb = new EmbedBuilder().WithTitle(title);

                var sb = new StringBuilder()
                    .AppendLine(Format.Bold(poll.Question))
                    .AppendLine();

                var totalVotesCast = 0;
                if (results.Length == 0)
                {
                    sb.AppendLine(GetText("no_votes_cast"));
                }
                else
                {
                    for (int i = 0; i < results.Length; i++)
                    {
                        var result = results[i];
                        sb.AppendLine(GetText("poll_result",
                            result.Key + 1,
                            Format.Bold(poll.Answers[result.Key].Text),
                            Format.Bold(result.Value.ToString())));
                        totalVotesCast += result.Value;
                    }
                }

                return eb.WithDescription(sb.ToString())
                    .WithFooter(efb => efb.WithText(GetText("x_votes_cast", totalVotesCast)))
                    .WithOkColor();
            }
        }
    }
}