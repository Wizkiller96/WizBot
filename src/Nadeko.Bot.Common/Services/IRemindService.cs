#nullable disable
using NadekoBot.Services.Database.Models;

namespace NadekoBot.Modules.Utility.Services;

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