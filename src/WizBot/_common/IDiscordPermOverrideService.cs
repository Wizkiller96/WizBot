#nullable disable
namespace Wiz.Common;

public interface IDiscordPermOverrideService
{
    bool TryGetOverrides(ulong guildId, string commandName, out WizBot.Db.GuildPerm? perm);
}