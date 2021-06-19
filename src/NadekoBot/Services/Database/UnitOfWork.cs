using NadekoBot.Services.Database.Repositories;
using NadekoBot.Services.Database.Repositories.Impl;
using System;
using System.Threading.Tasks;
using NadekoBot.Core.Services.Database;
using NadekoBot.Core.Services.Database.Repositories;

namespace NadekoBot.Services.Database
{
    public sealed class UnitOfWork : IUnitOfWork
    {
        public NadekoContext _context { get; }

        private IWaifuRepository _waifus;
        public IWaifuRepository Waifus => _waifus ?? (_waifus = new WaifuRepository(_context));

        private IDiscordUserRepository _discordUsers;
        public IDiscordUserRepository DiscordUsers => _discordUsers ?? (_discordUsers = new DiscordUserRepository(_context));

        public UnitOfWork(NadekoContext context)
        {
            _context = context;
        }

        public int SaveChanges() =>
            _context.SaveChanges();

        public Task<int> SaveChangesAsync() =>
            _context.SaveChangesAsync();

        public void Dispose()
        {
            _context.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
