#nullable disable
using LinqToDB;
using Microsoft.EntityFrameworkCore;
using WizBot.Services.Database.Models;

namespace WizBot.Db;

public static class WizBotExpressionExtensions
{
    public static int ClearFromGuild(this DbSet<WizBotExpression> exprs, ulong guildId)
        => exprs.Delete(x => x.GuildId == guildId);

    public static IEnumerable<WizBotExpression> ForId(this DbSet<WizBotExpression> exprs, ulong id)
        => exprs.AsNoTracking().AsQueryable().Where(x => x.GuildId == id).ToList();
}