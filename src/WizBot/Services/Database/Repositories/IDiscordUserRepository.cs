using Discord;
using WizBot.Services.Database.Models;

namespace WizBot.Services.Database.Repositories
{
    public interface IDiscordUserRepository : IRepository<DiscordUser>
    {
        DiscordUser GetOrCreate(IUser original);
        int GetUserGlobalRanking(ulong id);
        (ulong UserId, int TotalXp)[] GetUsersXpLeaderboardFor(int page);
    }
}
