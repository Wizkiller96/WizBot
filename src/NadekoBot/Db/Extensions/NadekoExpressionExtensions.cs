#nullable disable
using LinqToDB;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Services.Database.Models;

namespace NadekoBot.Db;

public static class NadekoExpressionExtensions
{
    public static int ClearFromGuild(this DbSet<CustomReaction> exprs, ulong guildId)
        => exprs.Delete(x => x.GuildId == guildId);

    public static IEnumerable<CustomReaction> ForId(this DbSet<CustomReaction> exprs, ulong id)
        => exprs.AsNoTracking().AsQueryable().Where(x => x.GuildId == id).ToList();

    public static CustomReaction GetByGuildIdAndInput(this DbSet<CustomReaction> exprs, ulong? guildId, string input)
        => exprs.FirstOrDefault(x => x.GuildId == guildId && x.Trigger.ToUpper() == input);
}