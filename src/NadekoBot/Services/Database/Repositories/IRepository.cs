using System.Collections.Generic;
using NadekoBot.Core.Services.Database.Models;
using NadekoBot.Services.Database.Models;

namespace NadekoBot.Services.Database.Repositories
{
    public interface IRepository<T> where T : DbEntity
    {
        void Add(T obj);
    }
}
