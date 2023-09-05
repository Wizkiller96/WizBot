#nullable disable
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.EntityFrameworkCore;
using NadekoBot.Common.ModuleBehaviors;
using NadekoBot.Db;
using NadekoBot.Db.Models;
using NadekoBot.Services.Database.Models;

namespace NadekoBot.Modules.Permissions.Services;

public sealed class BlacklistService : IExecOnMessage
{
    public int Priority
        => int.MaxValue;

    private readonly DbService _db;
    private readonly IPubSub _pubSub;
    private readonly IBotCredentials _creds;
    private IReadOnlyList<BlacklistEntry> blacklist;

    private readonly TypedKey<BlacklistEntry[]> _blPubKey = new("blacklist.reload");

    public BlacklistService(DbService db, IPubSub pubSub, IBotCredentials creds)
    {
        _db = db;
        _pubSub = pubSub;
        _creds = creds;

        Reload(false);
        _pubSub.Sub(_blPubKey, OnReload);
    }

    private ValueTask OnReload(BlacklistEntry[] newBlacklist)
    {
        blacklist = newBlacklist;
        return default;
    }

    public Task<bool> ExecOnMessageAsync(IGuild guild, IUserMessage usrMsg)
    {
        foreach (var bl in blacklist)
        {
            if (guild is not null && bl.Type == BlacklistType.Server && bl.ItemId == guild.Id)
            {
                Log.Information("Blocked input from blacklisted guild: {GuildName} [{GuildId}]", guild.Name, guild.Id);

                return Task.FromResult(true);
            }

            if (bl.Type == BlacklistType.Channel && bl.ItemId == usrMsg.Channel.Id)
            {
                Log.Information("Blocked input from blacklisted channel: {ChannelName} [{ChannelId}]",
                    usrMsg.Channel.Name,
                    usrMsg.Channel.Id);

                return Task.FromResult(true);
            }

            if (bl.Type == BlacklistType.User && bl.ItemId == usrMsg.Author.Id)
            {
                Log.Information("Blocked input from blacklisted user: {UserName} [{UserId}]",
                    usrMsg.Author.ToString(),
                    usrMsg.Author.Id);

                return Task.FromResult(true);
            }
        }

        return Task.FromResult(false);
    }

    public IReadOnlyList<BlacklistEntry> GetBlacklist()
        => blacklist;

    public void Reload(bool publish = true)
    {
        using var uow = _db.GetDbContext();
        var toPublish = uow.GetTable<BlacklistEntry>().ToArray();
        blacklist = toPublish;
        if (publish)
            _pubSub.Pub(_blPubKey, toPublish);
    }

    public async Task Blacklist(BlacklistType type, ulong id)
    {
        if (_creds.OwnerIds.Contains(id))
            return;

        await using var uow = _db.GetDbContext();

        await uow
            .GetTable<BlacklistEntry>()
            .InsertAsync(() => new()
            {
                ItemId = id,
                Type = type,
            });

        if (type == BlacklistType.User)
        {
            await uow.GetTable<DiscordUser>()
                .Where(x => x.UserId == id)
                .UpdateAsync(_ => new()
                {
                    CurrencyAmount = 0
                });
        }

        Reload();
    }

    public async Task UnBlacklist(BlacklistType type, ulong id)
    {
        await using var uow = _db.GetDbContext();
        await uow.GetTable<BlacklistEntry>()
            .Where(bi => bi.ItemId == id && bi.Type == type)
            .DeleteAsync();

        Reload();
    }

    public async Task BlacklistUsers(IReadOnlyCollection<ulong> toBlacklist)
    {
        await using var uow = _db.GetDbContext();
        var bc = uow.GetTable<BlacklistEntry>();
        await bc.BulkCopyAsync(toBlacklist.Select(uid => new BlacklistEntry
        {
            ItemId = uid,
            Type = BlacklistType.User
        }));

        var blList = toBlacklist.ToList();
        await uow.GetTable<DiscordUser>()
            .Where(x => blList.Contains(x.UserId))
            .UpdateAsync(_ => new()
            {
                CurrencyAmount = 0
            });

        Reload();
    }
}