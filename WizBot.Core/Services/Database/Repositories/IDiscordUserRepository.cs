using Discord;
using WizBot.Core.Services.Database.Models;

namespace WizBot.Core.Services.Database.Repositories
{
    public interface IDiscordUserRepository : IRepository<DiscordUser>
    {
        DiscordUser GetOrCreate(IUser original);
        int GetUserGlobalRanking(ulong id);
        DiscordUser[] GetUsersXpLeaderboardFor(int page);
    }
}
