using System.Linq;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Services.Database.Models;

namespace NadekoBot.Db
{
    public static class DbExtensions
    {
        public static T GetById<T>(this DbSet<T> set, int id) where T: DbEntity
            => set.FirstOrDefault(x => x.Id == id);
    }
}