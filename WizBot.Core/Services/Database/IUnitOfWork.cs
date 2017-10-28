using WizBot.Core.Services.Database.Repositories;
using System;
using System.Threading.Tasks;

namespace WizBot.Core.Services.Database
{
    public interface IUnitOfWork : IDisposable
    {
        WizBotContext _context { get; }

        IQuoteRepository Quotes { get; }
        IGuildConfigRepository GuildConfigs { get; }
        IDonatorsRepository Donators { get; }
        IReminderRepository Reminders { get; }
        ISelfAssignedRolesRepository SelfAssignedRoles { get; }
        IBotConfigRepository BotConfig { get; }
        ICustomReactionRepository CustomReactions { get; }
        ICurrencyRepository Currency { get; }
        ICurrencyTransactionsRepository CurrencyTransactions { get; }
        IMusicPlaylistRepository MusicPlaylists { get; }
        IPokeGameRepository PokeGame { get; }
        IWaifuRepository Waifus { get; }
        IDiscordUserRepository DiscordUsers { get; }
        IWarningsRepository Warnings { get; }
        IXpRepository Xp { get; }
        IClubRepository Clubs { get; }
        IPollsRepository Polls { get; }

        int Complete();
        Task<int> CompleteAsync();
    }
}
