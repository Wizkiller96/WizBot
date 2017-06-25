using WizBot.Services.Database.Models;
using System.Collections;
using System.Collections.Generic;

namespace WizBot.Services.Database.Repositories
{
    public interface IReminderRepository : IRepository<Reminder>
    {
        IEnumerable<Reminder> GetIncludedReminders(List<long> guildIds);
    }
}
