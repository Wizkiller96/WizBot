#nullable disable
using Microsoft.EntityFrameworkCore;
using NadekoBot.Common.ModuleBehaviors;
using NadekoBot.Db;
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
        var toPublish = uow.Blacklist.AsNoTracking().ToArray();
        blacklist = toPublish;
        if (publish)
            _pubSub.Pub(_blPubKey, toPublish);
    }

    public void Blacklist(BlacklistType type, ulong id)
    {
        if (_creds.OwnerIds.Contains(id))
            return;

        using var uow = _db.GetDbContext();
        var item = new BlacklistEntry
        {
            ItemId = id,
            Type = type
        };
        uow.Blacklist.Add(item);
        uow.SaveChanges();

        Reload();
    }

    public void UnBlacklist(BlacklistType type, ulong id)
    {
        using var uow = _db.GetDbContext();
        var toRemove = uow.Blacklist.FirstOrDefault(bi => bi.ItemId == id && bi.Type == type);

        if (toRemove is not null)
            uow.Blacklist.Remove(toRemove);

        uow.SaveChanges();

        Reload();
    }

    public void BlacklistUsers(IReadOnlyCollection<ulong> toBlacklist)
    {
        using (var uow = _db.GetDbContext())
        {
            var bc = uow.Blacklist;
            //blacklist the users
            bc.AddRange(toBlacklist.Select(x => new BlacklistEntry
            {
                ItemId = x,
                Type = BlacklistType.User
            }));

            //clear their currencies
            uow.DiscordUser.RemoveFromMany(toBlacklist);
            uow.SaveChanges();
        }

        Reload();
    }
}