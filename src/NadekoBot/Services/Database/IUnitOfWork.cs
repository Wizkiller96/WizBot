using NadekoBot.Core.Services.Database.Repositories;
using System;
using System.Threading.Tasks;

namespace NadekoBot.Core.Services.Database
{
    public interface IUnitOfWork : IDisposable
    {
        NadekoContext _context { get; } 
        IWaifuRepository Waifus { get; }
        IDiscordUserRepository DiscordUsers { get; }

        int SaveChanges();
        Task<int> SaveChangesAsync();
    }
}
