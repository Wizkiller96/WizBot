using WizBot.Db.Models;

namespace WizBot.Modules.Xp.Services;

public interface IUserService
{
    Task<DiscordUser?> GetUserAsync(ulong userId);
}