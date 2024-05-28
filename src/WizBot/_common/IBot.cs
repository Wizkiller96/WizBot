#nullable disable
using WizBot.Db.Models;

namespace WizBot;

public interface IBot
{
    IReadOnlyList<ulong> GetCurrentGuildIds();
    event Func<GuildConfig, Task> JoinedGuild;
    IReadOnlyCollection<GuildConfig> AllGuildConfigs { get; }
    bool IsReady { get; }
}