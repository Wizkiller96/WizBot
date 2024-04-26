#nullable disable
namespace Nadeko.Common;

public interface IDiscordPermOverrideService
{
    bool TryGetOverrides(ulong guildId, string commandName, out Nadeko.Bot.Db.GuildPerm? perm);
}