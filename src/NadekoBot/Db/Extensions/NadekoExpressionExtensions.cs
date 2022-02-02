#nullable disable
using LinqToDB;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Services.Database.Models;

namespace NadekoBot.Db;

public static class NadekoExpressionExtensions
{
    public static int ClearFromGuild(this DbSet<NadekoExpression> exprs, ulong guildId)
        => exprs.Delete(x => x.GuildId == guildId);

    public static IEnumerable<NadekoExpression> ForId(this DbSet<NadekoExpression> exprs, ulong id)
        => exprs.AsNoTracking().AsQueryable().Where(x => x.GuildId == id).ToList();

    public static NadekoExpression GetByGuildIdAndInput(
        this DbSet<NadekoExpression> exprs,
        ulong? guildId,
        string input)
        => exprs.FirstOrDefault(x => x.GuildId == guildId && x.Trigger.ToUpper() == input);
}