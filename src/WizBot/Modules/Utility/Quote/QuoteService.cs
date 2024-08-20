#nullable disable warnings
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using WizBot.Db.Models;

namespace WizBot.Modules.Utility;

public sealed class QuoteService : IQuoteService, INService
{
    private readonly DbService _db;

    public QuoteService(DbService db)
    {
        _db = db;
    }

    /// <summary>
    /// Delete all quotes created by the author in a guild
    /// </summary>
    /// <param name="guildId">ID of the guild</param>
    /// <param name="userId">ID of the user</param>
    /// <returns>Number of deleted qutoes</returns>
    public async Task<int> DeleteAllAuthorQuotesAsync(ulong guildId, ulong userId)
    {
        await using var ctx = _db.GetDbContext();
        var deleted = await ctx.GetTable<Quote>()
                               .Where(x => x.GuildId == guildId && x.AuthorId == userId)
                               .DeleteAsync();

        return deleted;
    }

    /// <summary>
    /// Delete all quotes in a guild
    /// </summary>
    /// <param name="guildId">ID of the guild</param>
    /// <returns>Number of deleted qutoes</returns>
    public async Task<int> DeleteAllQuotesAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        var deleted = await ctx.GetTable<Quote>()
                               .Where(x => x.GuildId == guildId)
                               .DeleteAsync();

        return deleted;
    }

    public async Task<IReadOnlyCollection<Quote>> GetAllQuotesAsync(ulong guildId, int page, OrderType order)
    {
        await using var uow = _db.GetDbContext();
        var q = uow.Set<Quote>()
                   .ToLinqToDBTable()
                   .Where(x => x.GuildId == guildId);

        if (order == OrderType.Keyword)
            q = q.OrderBy(x => x.Keyword);
        else
            q = q.OrderBy(x => x.Id);

        return await q.Skip(15 * page).Take(15).ToArrayAsyncLinqToDB();
    }
}