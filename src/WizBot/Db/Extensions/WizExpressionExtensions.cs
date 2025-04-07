#nullable disable
using LinqToDB;
using Microsoft.EntityFrameworkCore;
using WizBot.Db.Models;

namespace WizBot.Db;

public static class WizExpressionExtensions
{
    public static int ClearFromGuild(this DbSet<WizExpression> exprs, ulong guildId)
        => exprs.Delete(x => x.GuildId == guildId);

    public static IEnumerable<WizExpression> ForId(this DbSet<WizExpression> exprs, ulong id)
        => exprs.AsNoTracking().AsQueryable().Where(x => x.GuildId == id).ToList();
}