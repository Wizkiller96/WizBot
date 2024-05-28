#nullable disable
using Microsoft.EntityFrameworkCore;
using WizBot.Db;
using WizBot.Db.Models;

namespace WizBot.Modules.Gambling.Services;

public class ShopService : IShopService, INService
{
    private readonly DbService _db;

    public ShopService(DbService db)
        => _db = db;

    private IndexedCollection<ShopEntry> GetEntriesInternal(DbContext uow, ulong guildId)
        => uow.GuildConfigsForId(guildId,
                set => set.Include(x => x.ShopEntries)
                    .ThenInclude(x => x.Items))
            .ShopEntries.ToIndexed();

    public async Task<bool> ChangeEntryPriceAsync(ulong guildId, int index, int newPrice)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(newPrice);

        await using var uow = _db.GetDbContext();
        var entries = GetEntriesInternal(uow, guildId);

        if (index >= entries.Count)
            return false;

        entries[index].Price = newPrice;
        await uow.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ChangeEntryNameAsync(ulong guildId, int index, string newName)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentNullException(nameof(newName));

        await using var uow = _db.GetDbContext();
        var entries = GetEntriesInternal(uow, guildId);

        if (index >= entries.Count)
            return false;

        entries[index].Name = newName.TrimTo(100);
        await uow.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SwapEntriesAsync(ulong guildId, int index1, int index2)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index1);
        ArgumentOutOfRangeException.ThrowIfNegative(index2);

        await using var uow = _db.GetDbContext();
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
        ArgumentOutOfRangeException.ThrowIfNegative(fromIndex);
        ArgumentOutOfRangeException.ThrowIfNegative(toIndex);

        await using var uow = _db.GetDbContext();
        var entries = GetEntriesInternal(uow, guildId);

        if (fromIndex >= entries.Count || toIndex >= entries.Count || fromIndex == toIndex)
            return false;

        var entry = entries[fromIndex];
        entries.RemoveAt(fromIndex);
        entries.Insert(toIndex, entry);

        await uow.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SetItemRoleRequirementAsync(ulong guildId, int index, ulong? roleId)
    {
        await using var uow = _db.GetDbContext();
        var entries = GetEntriesInternal(uow, guildId);

        if (index >= entries.Count)
            return false;

        var entry = entries[index];

        entry.RoleRequirement = roleId;

        await uow.SaveChangesAsync();
        return true;
    }

    public async Task<ShopEntry> AddShopCommandAsync(ulong guildId, ulong userId, int price, string command)
    {
        await using var uow = _db.GetDbContext();

        var entries = GetEntriesInternal(uow, guildId);
        var entry = new ShopEntry()
        {
            AuthorId = userId,
            Command = command,
            Type = ShopEntryType.Command,
            Price = price,
        };
        entries.Add(entry);
        uow.GuildConfigsForId(guildId, set => set).ShopEntries = entries;

        await uow.SaveChangesAsync();

        return entry;
    }
}