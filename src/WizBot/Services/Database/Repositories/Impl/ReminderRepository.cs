using WizBot.Services.Database.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WizBot.Services.Database.Repositories.Impl
{
    public class ReminderRepository : Repository<Reminder>, IReminderRepository
    {
        public ReminderRepository(DbContext context) : base(context)
        {
        }

        public IEnumerable<Reminder> GetIncludedReminders(IEnumerable<long> guildIds)
        {
            return _set.Where(x => guildIds.Contains((long)x.ServerId)).ToList();
        }
    }
}
