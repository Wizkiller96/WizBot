using NadekoBot.Core.Services.Database.Repositories;
using NadekoBot.Core.Services.Database.Repositories.Impl;
using System;
using System.Threading.Tasks;

namespace NadekoBot.Core.Services.Database
{
    public sealed class UnitOfWork : IUnitOfWork
    {
        public NadekoContext _context { get; }

        private IQuoteRepository _quotes;
        public IQuoteRepository Quotes => _quotes ?? (_quotes = new QuoteRepository(_context));

        private IGuildConfigRepository _guildConfigs;
        public IGuildConfigRepository GuildConfigs => _guildConfigs ?? (_guildConfigs = new GuildConfigRepository(_context));

        private IReminderRepository _reminders;
        public IReminderRepository Reminders => _reminders ?? (_reminders = new ReminderRepository(_context));

        private IMusicPlaylistRepository _musicPlaylists;
        public IMusicPlaylistRepository MusicPlaylists => _musicPlaylists ?? (_musicPlaylists = new MusicPlaylistRepository(_context));

        private ICustomReactionRepository _customReactions;
        public ICustomReactionRepository CustomReactions => _customReactions ?? (_customReactions = new CustomReactionsRepository(_context));

        private IWaifuRepository _waifus;
        public IWaifuRepository Waifus => _waifus ?? (_waifus = new WaifuRepository(_context));

        private IDiscordUserRepository _discordUsers;
        public IDiscordUserRepository DiscordUsers => _discordUsers ?? (_discordUsers = new DiscordUserRepository(_context));

        private IWarningsRepository _warnings;
        public IWarningsRepository Warnings => _warnings ?? (_warnings = new WarningsRepository(_context));

        private IXpRepository _xp;
        public IXpRepository Xp => _xp ?? (_xp = new XpRepository(_context));

        private IPlantedCurrencyRepository _planted;
        public IPlantedCurrencyRepository PlantedCurrency => _planted ?? (_planted = new PlantedCurrencyRepository(_context));

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
