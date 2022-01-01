#nullable disable
using Microsoft.EntityFrameworkCore;
using NadekoBot.Services.Database;
using NadekoBot.Services.Database.Models;

namespace NadekoBot.Db;

public static class PollExtensions
{
    public static IEnumerable<Poll> GetAllPolls(this DbSet<Poll> polls)
        => polls.Include(x => x.Answers).Include(x => x.Votes).ToArray();

    public static void RemovePoll(this NadekoContext ctx, int id)
    {
        var p = ctx.Poll.Include(x => x.Answers).Include(x => x.Votes).FirstOrDefault(x => x.Id == id);

        if (p is null)
            return;

        if (p.Votes is not null)
        {
            ctx.RemoveRange(p.Votes);
            p.Votes.Clear();
        }

        if (p.Answers is not null)
        {
            ctx.RemoveRange(p.Answers);
            p.Answers.Clear();
        }

        ctx.Poll.Remove(p);
    }
}