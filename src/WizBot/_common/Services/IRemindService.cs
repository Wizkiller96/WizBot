#nullable disable
using WizBot.Db.Models;

namespace WizBot.Modules.Utility.Services;

public interface IRemindService
{
    Task AddReminderAsync(ulong userId,
        ulong targetId,
        ulong? guildId,
        bool isPrivate,
        DateTime time,
        string message,
        ReminderType reminderType);
}