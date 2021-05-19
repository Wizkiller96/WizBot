﻿using WizBot.Core.Services.Database.Repositories;
using System;
using System.Threading.Tasks;

namespace WizBot.Core.Services.Database
{
    public interface IUnitOfWork : IDisposable
    {
        WizBotContext _context { get; }

        IQuoteRepository Quotes { get; }
        IGuildConfigRepository GuildConfigs { get; }
        IReminderRepository Reminders { get; }
        ISelfAssignedRolesRepository SelfAssignedRoles { get; }
        ICustomReactionRepository CustomReactions { get; }
        IMusicPlaylistRepository MusicPlaylists { get; }
        IWaifuRepository Waifus { get; }
        IDiscordUserRepository DiscordUsers { get; }
        IWarningsRepository Warnings { get; }
        IXpRepository Xp { get; }
        IClubRepository Clubs { get; }
        IPollsRepository Polls { get; }
        IPlantedCurrencyRepository PlantedCurrency { get; }

        int SaveChanges();
        Task<int> SaveChangesAsync();
    }
}
