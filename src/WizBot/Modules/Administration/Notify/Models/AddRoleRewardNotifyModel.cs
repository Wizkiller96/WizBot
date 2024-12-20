using WizBot.Db.Models;
using WizBot.Modules.Administration;

namespace WizBot.Modules.Xp.Services;

public record struct AddRoleRewardNotifyModel(ulong GuildId, ulong RoleId, ulong UserId, long Level) : INotifyModel
{
    public static string KeyName
        => "notify.reward.addrole";

    public static NotifyType NotifyType
        => NotifyType.AddRoleReward;

    public IReadOnlyDictionary<string, Func<SocketGuild, string>> GetReplacements()
    {
        var model = this;
        return new Dictionary<string, Func<SocketGuild, string>>()
        {
            { "%event.user%", g => g.GetUser(model.UserId)?.ToString() ?? model.UserId.ToString() },
            { "%event.role%", g => g.GetRole(model.RoleId)?.ToString() ?? model.RoleId.ToString() },
            { "%event.level%", g => model.Level.ToString() }
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