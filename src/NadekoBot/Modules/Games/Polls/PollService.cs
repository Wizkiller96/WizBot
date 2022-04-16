#nullable disable
using NadekoBot.Common.Collections;
using NadekoBot.Common.ModuleBehaviors;
using NadekoBot.Db;
using NadekoBot.Modules.Games.Common;
using NadekoBot.Services.Database.Models;

namespace NadekoBot.Modules.Games.Services;

public class PollService : IExecOnMessage
{
    public ConcurrentDictionary<ulong, PollRunner> ActivePolls { get; } = new();

    public int Priority
        => 5;

    private readonly DbService _db;
    private readonly IBotStrings _strs;
    private readonly IEmbedBuilderService _eb;

    public PollService(DbService db, IBotStrings strs, IEmbedBuilderService eb)
    {
        _db = db;
        _strs = strs;
        _eb = eb;

        using var uow = db.GetDbContext();
        ActivePolls = uow.Poll.GetAllPolls()
                         .ToDictionary(x => x.GuildId,
                             x =>
                             {
                                 var pr = new PollRunner(db, x);
                                 pr.OnVoted += Pr_OnVoted;
                                 return pr;
                             })
                         .ToConcurrent();
    }

    public Poll CreatePoll(ulong guildId, ulong channelId, string input)
    {
        if (string.IsNullOrWhiteSpace(input) || !input.Contains(";"))
            return null;
        var data = input.Split(';');
        if (data.Length < 3)
            return null;

        var col = new IndexedCollection<PollAnswer>(data.Skip(1)
                                                        .Select(x => new PollAnswer
                                                        {
                                                            Text = x
                                                        }));

        return new()
        {
            Answers = col,
            Question = data[0],
            ChannelId = channelId,
            GuildId = guildId,
            Votes = new()
        };
    }

    public bool StartPoll(Poll p)
    {
        var pr = new PollRunner(_db, p);
        if (ActivePolls.TryAdd(p.GuildId, pr))
        {
            using (var uow = _db.GetDbContext())
            {
                uow.Poll.Add(p);
                uow.SaveChanges();
            }

            pr.OnVoted += Pr_OnVoted;
            return true;
        }

        return false;
    }

    public Poll StopPoll(ulong guildId)
    {
        if (ActivePolls.TryRemove(guildId, out var pr))
        {
            pr.OnVoted -= Pr_OnVoted;

            using var uow = _db.GetDbContext();
            uow.RemovePoll(pr.Poll.Id);
            uow.SaveChanges();

            return pr.Poll;
        }

        return null;
    }

    private async Task Pr_OnVoted(IUserMessage msg, IGuildUser usr)
    {
        var toDelete = await msg.Channel.SendConfirmAsync(_eb,
            _strs.GetText(strs.poll_voted(Format.Bold(usr.ToString())), usr.GuildId));
        toDelete.DeleteAfter(5);
        try { await msg.DeleteAsync(); }
        catch { }
    }

    public async Task<bool> ExecOnMessageAsync(IGuild guild, IUserMessage msg)
    {
        if (guild is null)
            return false;

        if (!ActivePolls.TryGetValue(guild.Id, out var poll))
            return false;

        try
        {
            var voted = await poll.TryVote(msg);

            if (voted)
            {
                Log.Information("User {UserName} [{UserId}] voted in a poll on {GuildName} [{GuildId}] server",
                    msg.Author.ToString(),
                    msg.Author.Id,
                    guild.Name,
                    guild.Id);
            }

            return voted;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error voting");
        }

        return false;
    }
}