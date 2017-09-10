using WizBot.Services.Database.Models;

namespace WizBot.Services.Database.Repositories
{
    public interface IXpRepository : IRepository<UserXpStats>
    {
        UserXpStats GetOrCreateUser(ulong guildId, ulong userId);
        int GetTotalUserXp(ulong userId);
        UserXpStats[] GetUsersFor(ulong guildId, int page);
        (ulong UserId, int TotalXp)[] GetUsersFor(int page);
        int GetUserGlobalRanking(ulong userId);
        int GetUserGuildRanking(ulong userId, ulong guildId);
    }
}