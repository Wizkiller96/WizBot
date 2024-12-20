using WizBot.Db.Models;

namespace WizBot.Modules.Administration;

public record struct LevelUpNotifyModel(
    ulong GuildId,
    ulong ChannelId,
    ulong UserId,
    long Level) : INotifyModel
{
    public static string KeyName
        => "notify.levelup";

    public static NotifyType NotifyType
        => NotifyType.LevelUp;

    public IReadOnlyDictionary<string, Func<SocketGuild, string>> GetReplacements()
    {
        var data = this;
        return new Dictionary<string, Func<SocketGuild, string>>()
        {
            { "%event.level%", g => data.Level.ToString() },
            { "%event.user%", g => g.GetUser(data.UserId)?.ToString() ?? data.UserId.ToString() },
        };
    }

    public bool TryGetGuildId(out ulong guildId)
    {
        guildId = GuildId;
        return true;
    }

    public bool TryGetUserId(out ulong userId)
    {
        userId = UserId;
        return true;
    }
}