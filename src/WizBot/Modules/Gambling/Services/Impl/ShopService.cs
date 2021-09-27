﻿using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WizBot.Common.Collections;
using WizBot.Services;
using WizBot.Services.Database;
using WizBot.Services.Database.Models;
using WizBot.Db;
using WizBot.Extensions;
using WizBot.Modules.Administration;

namespace WizBot.Modules.Gambling.Services
{
    public class ShopService : IShopService, INService
    {
        private readonly DbService _db;

        public ShopService(DbService db)
        {
            _db = db;
        }

        private IndexedCollection<ShopEntry> GetEntriesInternal(WizBotContext uow, ulong guildId) =>
            uow.GuildConfigsForId(
                    guildId,
                    set => set.Include(x => x.ShopEntries).ThenInclude(x => x.Items)
                )
                .ShopEntries
                .ToIndexed();

        public async Task<bool> ChangeEntryPriceAsync(ulong guildId, int index, int newPrice)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (newPrice <= 0)
                throw new ArgumentOutOfRangeException(nameof(newPrice));

            using var uow = _db.GetDbContext();
            var entries = GetEntriesInternal(uow, guildId);

            if (index >= entries.Count)
                return false;

            entries[index].Price = newPrice;
            await uow.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ChangeEntryNameAsync(ulong guildId, int index, string newName)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (string.IsNullOrWhiteSpace(newName))
                throw new ArgumentNullException(nameof(newName));

            using var uow = _db.GetDbContext();
            var entries = GetEntriesInternal(uow, guildId);

            if (index >= entries.Count)
                return false;

            entries[index].Name = newName.TrimTo(100);
            await uow.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SwapEntriesAsync(ulong guildId, int index1, int index2)
        {
            if (index1 < 0)
                throw new ArgumentOutOfRangeException(nameof(index1));
            if (index2 < 0)
                throw new ArgumentOutOfRangeException(nameof(index2));

            using var uow = _db.GetDbContext();
            var entries = GetEntriesInternal(uow, guildId);

            if (index1 >= entries.Count || index2 >= entries.Count || index1 == index2)
                return false;

            entries[index1].Index = index2;
            entries[index2].Index = index1;

            await uow.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MoveEntryAsync(ulong guildId, int fromIndex, int toIndex)
        {
            if (fromIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(fromIndex));
            if (toIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(toIndex));

            using var uow = _db.GetDbContext();
            var entries = GetEntriesInternal(uow, guildId);

            if (fromIndex >= entries.Count || toIndex >= entries.Count || fromIndex == toIndex)
                return false;

            var entry = entries[fromIndex];
            entries.RemoveAt(fromIndex);
            entries.Insert(toIndex, entry);

            await uow.SaveChangesAsync();
            return true;
        }
    }
}