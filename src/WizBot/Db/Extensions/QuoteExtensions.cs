#nullable disable
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WizBot.Db.Models;

namespace WizBot.Db;

public static class QuoteExtensions
{
    public static IEnumerable<Quote> GetForGuild(this DbSet<Quote> quotes, ulong guildId)
        => quotes.AsQueryable().Where(x => x.GuildId == guildId);
    
    public static async Task<Quote> GetRandomQuoteByKeywordAsync(
        this DbSet<Quote> quotes,
        ulong guildId,
        string keyword)
    {
        return (await quotes.AsQueryable().Where(q => q.GuildId == guildId && q.Keyword == keyword).ToArrayAsync())
            .RandomOrDefault();
    }

    public static async Task<Quote> SearchQuoteKeywordTextAsync(
        this DbSet<Quote> quotes,
        ulong guildId,
        string keyword,
        string text)
    {
        return (await quotes.AsQueryable()
                            .Where(q => q.GuildId == guildId
                                        && (keyword == null || q.Keyword == keyword)
                                        && (EF.Functions.Like(q.Text.ToUpper(), $"%{text.ToUpper()}%")
                                            || EF.Functions.Like(q.AuthorName, text)))
                            .ToArrayAsync())
            .RandomOrDefault();
    }

    public static void RemoveAllByKeyword(this DbSet<Quote> quotes, ulong guildId, string keyword)
        => quotes.RemoveRange(quotes.AsQueryable().Where(x => x.GuildId == guildId && x.Keyword.ToUpper() == keyword));
}