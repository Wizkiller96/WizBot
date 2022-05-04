#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using NadekoBot.Common.ModuleBehaviors;
using NadekoBot.Modules.Xp.Extensions;
using NadekoBot.Services.Database.Models;

namespace NadekoBot.Modules.Administration.Services;

public sealed class ReactionRolesService : IReadyExecutor, INService, IReactionRoleService
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;
    private readonly IBotCredentials _creds;

    private ConcurrentDictionary<ulong, List<ReactionRoleV2>> _cache;
    private readonly object _cacheLock = new();
    private readonly SemaphoreSlim _assignementLock = new(1, 1);

    public ReactionRolesService(DiscordSocketClient client, DbService db, IBotCredentials creds)
    {
        _db = db;
        _client = client;
        _creds = creds;
        _cache = new();
    }
    
    public async Task OnReadyAsync()
    {
        await using var uow = _db.GetDbContext();
        var reros = await uow.GetTable<ReactionRoleV2>()
                             .Where(x => Linq2DbExpressions.GuildOnShard(x.GuildId, _creds.TotalShards, _client.ShardId))
                             .ToListAsyncLinqToDB();

        foreach (var group in reros.GroupBy(x => x.MessageId))
        {
            _cache[group.Key] = group.ToList();
        }

        _client.ReactionAdded += ClientOnReactionAdded;
        _client.ReactionRemoved += ClientOnReactionRemoved;
    }

    private async Task<(IGuildUser, IRole)> GetUserAndRoleAsync(
        SocketReaction r,
        ReactionRoleV2 rero)
    {
        var guild = _client.GetGuild(rero.GuildId);
        var role = guild?.GetRole(rero.RoleId);

        if (role is null)
            return default;

        var user = guild.GetUser(r.UserId) as IGuildUser
                   ?? await _client.Rest.GetGuildUserAsync(guild.Id, r.UserId);

        if (user is null)
            return default;

        return (user, role);
    }

    private Task ClientOnReactionRemoved(
        Cacheable<IUserMessage, ulong> msg,
        Cacheable<IMessageChannel, ulong> ch,
        SocketReaction r)
    {
        if (!_cache.TryGetValue(msg.Id, out var reros))
            return Task.CompletedTask;

        _ = Task.Run(async () =>
        {
            var rero = reros.FirstOrDefault(x => x.Emote == r.Emote.Name || x.Emote == r.Emote.ToString());
            if (rero is null)
                return;

            var (user, role) = await GetUserAndRoleAsync(r, rero);

            if (user.IsBot)
                return;

            await _assignementLock.WaitAsync();
            try
            {
                if (user.RoleIds.Contains(role.Id))
                {
                    await user.RemoveRoleAsync(role.Id);
                }
            }
            finally
            {
                _assignementLock.Release();
            }
        });

        return Task.CompletedTask;
    }

    private Task ClientOnReactionAdded(
        Cacheable<IUserMessage, ulong> msg,
        Cacheable<IMessageChannel, ulong> ch,
        SocketReaction r)
    {
        if (!_cache.TryGetValue(msg.Id, out var reros))
            return Task.CompletedTask;

        _ = Task.Run(async () =>
        {
            var rero = reros.FirstOrDefault(x => x.Emote == r.Emote.Name || x.Emote == r.Emote.ToString());
            if (rero is null)
                return;

            var (user, role) = await GetUserAndRoleAsync(r, rero);

            if (user.IsBot)
                return;

            await _assignementLock.WaitAsync();
            try
            {
                if (!user.RoleIds.Contains(role.Id))
                {
                    // first check if there is a level requirement
                    // and if there is, make sure user satisfies it
                    if (rero.LevelReq > 0)
                    {
                        await using var ctx = _db.GetDbContext();
                        var levelData = await ctx.GetTable<UserXpStats>()
                                 .GetLevelDataFor(user.GuildId, user.Id);

                        if (levelData.Level < rero.LevelReq)
                            return;
                    }
                    
                    // remove all other roles from the same group from the user
                    // execept in group 0, which is a special, non-exclusive group
                    if (rero.Group != 0)
                    {
                        var exclusive = reros
                                        .Where(x => x.Group == rero.Group && x.RoleId != role.Id)
                                        .Select(x => x.RoleId)
                                        .Distinct();

                        
                        try { await user.RemoveRolesAsync(exclusive); }
                        catch { }

                        // remove user's previous reaction
                        try
                        {
                            var m = await msg.GetOrDownloadAsync();
                            if (m is not null)
                            {
                                var reactToRemove = m.Reactions
                                                     .FirstOrDefault(x => x.Key.ToString() != r.Emote.ToString())
                                                     .Key;

                                if (reactToRemove is not null)
                                {
                                    await m.RemoveReactionAsync(reactToRemove, user);
                                }
                            }
                        }
                        catch
                        {
                        }
                    }

                    await user.AddRoleAsync(role.Id);
                }
            }
            finally
            {
                _assignementLock.Release();
            }
        });

        return Task.CompletedTask;
    }

    /// <summary>
    /// Adds a single reaction role
    /// </summary>
    /// <param name="guildId"></param>
    /// <param name="msg"></param>
    /// <param name="channel"></param>
    /// <param name="emote"></param>
    /// <param name="role"></param>
    /// <param name="group"></param>
    /// <param name="levelReq"></param>
    /// <returns></returns>
    public async Task<bool> AddReactionRole(
        ulong guildId,
        IMessage msg,
        ITextChannel channel,
        string emote,
        IRole role,
        int group = 0,
        int levelReq = 0)
    {
        if (group < 0)
            throw new ArgumentOutOfRangeException(nameof(group));

        if (levelReq < 0)
            throw new ArgumentOutOfRangeException(nameof(group));

        await using var ctx = _db.GetDbContext();
        var activeReactionRoles = await ctx.GetTable<ReactionRoleV2>()
                                           .Where(x => x.GuildId == guildId)
                                           .CountAsync();

        if (activeReactionRoles >= 50)
            return false;

        var changed = await ctx.GetTable<ReactionRoleV2>()
                               .InsertOrUpdateAsync(() => new()
                                   {
                                       GuildId = guildId,
                                       ChannelId = channel.Id,

                                       MessageId = msg.Id,
                                       Emote = emote,

                                       RoleId = role.Id,
                                       Group = group,
                                       LevelReq = levelReq
                                   },
                                   (old) => new()
                                   {
                                       RoleId = role.Id,
                                       Group = group,
                                       LevelReq = levelReq
                                   },
                                   () => new()
                                   {
                                       MessageId = msg.Id,
                                       Emote = emote,
                                   });

        if (changed == 0)
            return false;

        var obj = new ReactionRoleV2()
        {
            GuildId = guildId,
            MessageId = msg.Id,
            Emote = emote,
            RoleId = role.Id,
            Group = group,
            LevelReq = levelReq
        };

        lock (_cacheLock)
        {
            _cache.AddOrUpdate(msg.Id,
                _ => new()
                {
                    obj
                },
                (_, list) =>
                {
                    list.RemoveAll(x => x.Emote == emote);
                    list.Add(obj);
                    return list;
                });
        }

        return true;
    }

    /// <summary>
    /// Get all reaction roles on the specified server
    /// </summary>
    /// <param name="guildId"></param>
    /// <returns></returns>
    public async Task<IReadOnlyCollection<ReactionRoleV2>> GetReactionRolesAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<ReactionRoleV2>()
                        .Where(x => x.GuildId == guildId)
                        .ToListAsync();
    }

    /// <summary>
    /// Remove reaction roles on the specified message
    /// </summary>
    /// <param name="guildId"></param>
    /// <param name="messageId"></param>
    /// <returns></returns>
    public async Task<bool> RemoveReactionRoles(ulong guildId, ulong messageId)
    {
        // guildid is used for quick index lookup
        await using var ctx = _db.GetDbContext();
        var changed = await ctx.GetTable<ReactionRoleV2>()
                               .Where(x => x.GuildId == guildId && x.MessageId == messageId)
                               .DeleteAsync();

        _cache.TryRemove(messageId, out _);

        if (changed == 0)
            return false;

        return true;
    }

    /// <summary>
    /// Remove all reaction roles in the specified server
    /// </summary>
    /// <param name="guildId"></param>
    /// <returns></returns>
    public async Task<int> RemoveAllReactionRoles(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        var output = await ctx.GetTable<ReactionRoleV2>()
                              .Where(x => x.GuildId == guildId)
                              .DeleteWithOutputAsync(x => x.MessageId);

        lock (_cacheLock)
        {
            foreach (var o in output)
            {
                _cache.TryRemove(o, out _);
            }
        }

        return output.Length;
    }

    public async Task<IReadOnlyCollection<IEmote>> TransferReactionRolesAsync(ulong guildId, ulong fromMessageId, ulong toMessageId)
    {
        await using var ctx = _db.GetDbContext();
        var updated = ctx.GetTable<ReactionRoleV2>()
                         .Where(x => x.GuildId == guildId && x.MessageId == fromMessageId)
                         .UpdateWithOutput(old => new()
                             {
                                 MessageId = toMessageId
                             },
                             (old, neu) => neu);
        lock (_cacheLock)
        {
            if (_cache.TryRemove(fromMessageId, out var data))
            {
                if (_cache.TryGetValue(toMessageId, out var newData))
                {
                    newData.AddRange(data);
                }
                else
                {
                    _cache[toMessageId] = data;
                }
            }
        }

        return updated.Select(x => x.Emote.ToIEmote()).ToList();
    }
}