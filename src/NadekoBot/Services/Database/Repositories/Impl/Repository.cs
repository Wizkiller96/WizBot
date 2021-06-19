using Microsoft.EntityFrameworkCore;
using NadekoBot.Services.Database.Models;
using System.Collections.Generic;
using System.Linq;
using NadekoBot.Core.Services.Database.Models;

namespace NadekoBot.Services.Database.Repositories.Impl
{
    public abstract class Repository<T> : IRepository<T> where T : DbEntity
    {
        protected DbContext _context { get; set; }
        protected DbSet<T> _set { get; set; }

        public Repository(DbContext context)
        {
            _context = context;
            _set = context.Set<T>();
        }

        public void Add(T obj) =>
            _set.Add(obj);
    }
}
