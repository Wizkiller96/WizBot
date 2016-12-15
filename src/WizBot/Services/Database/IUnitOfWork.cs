﻿using WizBot.Services.Database.Repositories;
using System;
using System.Threading.Tasks;

namespace WizBot.Services.Database
{
    public interface IUnitOfWork : IDisposable
    {
        WizBotContext _context { get; }

        IQuoteRepository Quotes { get; }
        IGuildConfigRepository GuildConfigs { get; }
        IDonatorsRepository Donators { get; }
        IClashOfClansRepository ClashOfClans { get; }
        IReminderRepository Reminders { get; }
        ISelfAssignedRolesRepository SelfAssignedRoles { get; }
        IBotConfigRepository BotConfig { get; }
        IRepeaterRepository Repeaters { get; }
        IUnitConverterRepository ConverterUnits { get; }
        ICustomReactionRepository CustomReactions { get; }
        ICurrencyRepository Currency { get; }
        ICurrencyTransactionsRepository CurrencyTransactions { get; }
        IMusicPlaylistRepository MusicPlaylists { get; }
        IPokeGameRepository PokeGame { get; }

        int Complete();
        Task<int> CompleteAsync();
    }
}
