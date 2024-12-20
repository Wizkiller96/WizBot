#nullable disable
using WizBot.Db.Models;

namespace WizBot.Modules.Administration.Services;

public record struct ProtectionNotifyModel(ulong GuildId, ProtectionType ProtType, ulong UserId) : INotifyModel
{
    public static string KeyName
        => "notify.protection";

    public static NotifyType NotifyType
        => NotifyType.Protection;

    public IReadOnlyDictionary<string, Func<SocketGuild, string>> GetReplacements()
    {
        var data = this;
        return new Dictionary<string, Func<SocketGuild, string>>()
        {
            { "%event.type%", g => data.ProtType.ToString() },
        };
    }

    public bool TryGetUserId(out ulong userId)
    {
        userId = UserId;
        return true;
    }

    public bool TryGetGuildId(out ulong guildId)
    {
        guildId = GuildId;
        return true;
    }
}