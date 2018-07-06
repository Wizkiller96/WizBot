using WizBot.Core.Services.Database.Models;
using System.Collections.Generic;

namespace WizBot.Core.Services.Database.Repositories
{
    public interface IReminderRepository : IRepository<Reminder>
    {
        IEnumerable<Reminder> GetIncludedReminders(IEnumerable<ulong> guildIds);
        IEnumerable<Reminder> RemindersFor(ulong userId, int page);
    }
}
