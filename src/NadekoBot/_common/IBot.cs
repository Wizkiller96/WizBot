#nullable disable
using Nadeko.Bot.Db.Models;

namespace NadekoBot;

public interface IBot
{
    IReadOnlyList<ulong> GetCurrentGuildIds();
    event Func<GuildConfig, Task> JoinedGuild;
    IReadOnlyCollection<GuildConfig> AllGuildConfigs { get; }
    bool IsReady { get; }
}