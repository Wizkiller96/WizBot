#nullable disable
using NadekoBot.Modules.Games.Services;
using NadekoBot.Services.Database.Models;
using System.Text;

namespace NadekoBot.Modules.Games;

public partial class Games
{
    [Group]
    public partial class PollCommands : NadekoModule<PollService>
    {
        private readonly DiscordSocketClient _client;

        public PollCommands(DiscordSocketClient client)
            => _client = client;

        [Cmd]
        [UserPerm(GuildPerm.ManageMessages)]
        [RequireContext(ContextType.Guild)]
        public async partial Task Poll([Leftover] string arg)
        {
            if (string.IsNullOrWhiteSpace(arg))
                return;

            var poll = _service.CreatePoll(ctx.Guild.Id, ctx.Channel.Id, arg);
            if (poll is null)
            {
                await ReplyErrorLocalizedAsync(strs.poll_invalid_input);
                return;
            }

            if (_service.StartPoll(poll))
            {
                await ctx.Channel.EmbedAsync(_eb.Create()
                                                .WithOkColor()
                                                .WithTitle(GetText(strs.poll_created(ctx.User.ToString())))
                                                .WithDescription(Format.Bold(poll.Question)
                                                                 + "\n\n"
                                                                 + string.Join("\n",
                                                                     poll.Answers.Select(x
                                                                         => $"`{x.Index + 1}.` {Format.Bold(x.Text)}"))));
            }
            else
                await ReplyErrorLocalizedAsync(strs.poll_already_running);
        }

        [Cmd]
        [UserPerm(GuildPerm.ManageMessages)]
        [RequireContext(ContextType.Guild)]
        public async partial Task PollStats()
        {
            if (!_service.ActivePolls.TryGetValue(ctx.Guild.Id, out var pr))
                return;

            await ctx.Channel.EmbedAsync(GetStats(pr.Poll, GetText(strs.current_poll_results)));
        }

        [Cmd]
        [UserPerm(GuildPerm.ManageMessages)]
        [RequireContext(ContextType.Guild)]
        public async partial Task Pollend()
        {
            Poll p;
            if ((p = _service.StopPoll(ctx.Guild.Id)) is null)
                return;

            var embed = GetStats(p, GetText(strs.poll_closed));
            await ctx.Channel.EmbedAsync(embed);
        }

        public IEmbedBuilder GetStats(Poll poll, string title)
        {
            var results = poll.Votes.GroupBy(kvp => kvp.VoteIndex).ToDictionary(x => x.Key, x => x.Sum(_ => 1));

            var totalVotesCast = results.Sum(x => x.Value);

            var eb = _eb.Create().WithTitle(title);

            var sb = new StringBuilder().AppendLine(Format.Bold(poll.Question)).AppendLine();

            var stats = poll.Answers.Select(x =>
                            {
                                results.TryGetValue(x.Index, out var votes);

                                return (x.Index, votes, x.Text);
                            })
                            .OrderByDescending(x => x.votes)
                            .ToArray();

            for (var i = 0; i < stats.Length; i++)
            {
                var (index, votes, text) = stats[i];
                sb.AppendLine(GetText(strs.poll_result(index + 1, Format.Bold(text), Format.Bold(votes.ToString()))));
            }

            return eb.WithDescription(sb.ToString())
                     .WithFooter(GetText(strs.x_votes_cast(totalVotesCast)))
                     .WithOkColor();
        }
    }
}