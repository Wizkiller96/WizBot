#nullable disable
using LinqToDB;
using WizBot.Services.Database.Models;

namespace WizBot.Modules.Xp.Extensions;

public static class Extensions
{
    public static async Task<LevelStats> GetLevelDataFor(this ITable<UserXpStats> userXp, ulong guildId, ulong userId)
        => await userXp
                 .Where(x => x.GuildId == guildId && x.UserId == userId)
                 .FirstOrDefaultAsync() is UserXpStats uxs
            ? new(uxs.Xp + uxs.AwardedXp)
            : new(0);
}