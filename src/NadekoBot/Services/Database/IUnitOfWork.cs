using NadekoBot.Core.Services.Database.Repositories;
using System;
using System.Threading.Tasks;

namespace NadekoBot.Core.Services.Database
{
    public interface IUnitOfWork : IDisposable
    {
        NadekoContext _context { get; }

        IQuoteRepository Quotes { get; }
        IGuildConfigRepository GuildConfigs { get; }
        IWaifuRepository Waifus { get; }
        IDiscordUserRepository DiscordUsers { get; }
        IXpRepository Xp { get; }

        int SaveChanges();
        Task<int> SaveChangesAsync();
    }
}
