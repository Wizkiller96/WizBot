using Discord;
using WizBot.Services.Database.Models;

namespace WizBot.Services.Database.Repositories
{
    public interface IDiscordUserRepository : IRepository<DiscordUser>
    {
        DiscordUser GetOrCreate(IUser original);
    }
}
