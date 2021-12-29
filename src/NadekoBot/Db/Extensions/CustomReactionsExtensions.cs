#nullable disable
using LinqToDB;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Services.Database.Models;

namespace NadekoBot.Db;

public static class CustomReactionsExtensions
{
    public static int ClearFromGuild(this DbSet<CustomReaction> crs, ulong guildId)
        => crs.Delete(x => x.GuildId == guildId);

    public static IEnumerable<CustomReaction> ForId(this DbSet<CustomReaction> crs, ulong id)
        => crs.AsNoTracking().AsQueryable().Where(x => x.GuildId == id).ToList();

    public static CustomReaction GetByGuildIdAndInput(this DbSet<CustomReaction> crs, ulong? guildId, string input)
        => crs.FirstOrDefault(x => x.GuildId == guildId && x.Trigger.ToUpper() == input);
}