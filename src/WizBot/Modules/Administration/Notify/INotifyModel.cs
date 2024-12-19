using WizBot.Db.Models;
using System.Collections;

namespace WizBot.Modules.Administration;

public interface INotifyModel
{
    static abstract string KeyName { get; }
    static abstract NotifyType NotifyType { get; }
    IReadOnlyDictionary<string, Func<SocketGuild, string>> GetReplacements();

    public virtual bool TryGetGuildId(out ulong guildId)
    {
        guildId = 0;
        return false;
    }
    
    public virtual bool TryGetUserId(out ulong userId)
    {
        userId = 0;
        return false;
    }
}