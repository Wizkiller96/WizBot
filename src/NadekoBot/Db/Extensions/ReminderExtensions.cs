using NadekoBot.Services.Database.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace NadekoBot.Db
{
    public static class ReminderExtensions
    {
        public static IEnumerable<Reminder> GetIncludedReminders(this DbSet<Reminder> reminders, IEnumerable<ulong> guildIds) 
            => reminders.AsQueryable()
                .Where(x => guildIds.Contains(x.ServerId) || x.ServerId == 0)
                .ToList();

        public static IEnumerable<Reminder> RemindersFor(this DbSet<Reminder> reminders, ulong userId, int page)
            => reminders.AsQueryable()
                .Where(x => x.UserId == userId)
                .OrderBy(x => x.DateAdded)
                .Skip(page * 10)
                .Take(10);
    }
}
