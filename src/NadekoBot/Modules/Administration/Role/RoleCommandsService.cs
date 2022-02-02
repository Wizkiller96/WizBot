#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Common.Collections;
using NadekoBot.Db;
using NadekoBot.Services.Database.Models;

namespace NadekoBot.Modules.Administration.Services;

public class RoleCommandsService : INService
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;
    private readonly ConcurrentDictionary<ulong, IndexedCollection<ReactionRoleMessage>> _models;

    /// <summary>
    ///     Contains the (Message ID, User ID) of reaction roles that are currently being processed.
    /// </summary>
    private readonly ConcurrentHashSet<(ulong, ulong)> _reacting = new();

    public RoleCommandsService(DiscordSocketClient client, DbService db, Bot bot)
    {
        _db = db;
        _client = client;
#if !GLOBAL_NADEKO
        _models = bot.AllGuildConfigs.ToDictionary(x => x.GuildId, x => x.ReactionRoleMessages).ToConcurrent();

        _client.ReactionAdded += _client_ReactionAdded;
        _client.ReactionRemoved += _client_ReactionRemoved;
#endif
    }

    private Task _client_ReactionAdded(
        Cacheable<IUserMessage, ulong> msg,
        Cacheable<IMessageChannel, ulong> chan,
        SocketReaction reaction)
    {
        _ = Task.Run(async () =>
        {
            if (!reaction.User.IsSpecified
                || reaction.User.Value.IsBot
                || reaction.User.Value is not SocketGuildUser gusr
                || chan.Value is not SocketGuildChannel gch
                || !_models.TryGetValue(gch.Guild.Id, out var confs))
                return;

            var conf = confs.FirstOrDefault(x => x.MessageId == msg.Id);

            if (conf is null)
                return;

            // compare emote names for backwards compatibility :facepalm:
            var reactionRole = conf.ReactionRoles.FirstOrDefault(x
                => x.EmoteName == reaction.Emote.Name || x.EmoteName == reaction.Emote.ToString());

            if (reactionRole is not null)
            {
                if (!conf.Exclusive)
                {
                    await AddReactionRoleAsync(gusr, reactionRole);
                    return;
                }

                // If same (message, user) are being processed in an exclusive rero, quit
                if (!_reacting.Add((msg.Id, reaction.UserId)))
                    return;

                try
                {
                    var removeExclusiveTask = RemoveExclusiveReactionRoleAsync(msg,
                        gusr,
                        reaction,
                        conf,
                        reactionRole,
                        CancellationToken.None);
                    var addRoleTask = AddReactionRoleAsync(gusr, reactionRole);

                    await Task.WhenAll(removeExclusiveTask, addRoleTask);
                }
                finally
                {
                    // Free (message/user) for another exclusive rero
                    _reacting.TryRemove((msg.Id, reaction.UserId));
                }
            }
            else
            {
                var dl = await msg.GetOrDownloadAsync();
                await dl.RemoveReactionAsync(reaction.Emote,
                    dl.Author,
                    new()
                    {
                        RetryMode = RetryMode.RetryRatelimit | RetryMode.Retry502
                    });
                Log.Warning("User {Author} is adding unrelated reactions to the reaction roles message", dl.Author);
            }
        });

        return Task.CompletedTask;
    }

    private Task _client_ReactionRemoved(
        Cacheable<IUserMessage, ulong> msg,
        Cacheable<IMessageChannel, ulong> chan,
        SocketReaction reaction)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                if (!reaction.User.IsSpecified
                    || reaction.User.Value.IsBot
                    || reaction.User.Value is not SocketGuildUser gusr)
                    return;

                if (chan.Value is not SocketGuildChannel gch)
                    return;

                if (!_models.TryGetValue(gch.Guild.Id, out var confs))
                    return;

                var conf = confs.FirstOrDefault(x => x.MessageId == msg.Id);

                if (conf is null)
                    return;

                var reactionRole = conf.ReactionRoles.FirstOrDefault(x
                    => x.EmoteName == reaction.Emote.Name || x.EmoteName == reaction.Emote.ToString());

                if (reactionRole is not null)
                {
                    var role = gusr.Guild.GetRole(reactionRole.RoleId);
                    if (role is null)
                        return;
                    await gusr.RemoveRoleAsync(role);
                }
            }
            catch { }
        });

        return Task.CompletedTask;
    }

    public bool Get(ulong id, out IndexedCollection<ReactionRoleMessage> rrs)
        => _models.TryGetValue(id, out rrs);

    public bool Add(ulong id, ReactionRoleMessage rrm)
    {
        using var uow = _db.GetDbContext();
        var table = uow.GetTable<ReactionRoleMessage>();
        table.Delete(x => x.MessageId == rrm.MessageId);

        var gc = uow.GuildConfigsForId(id,
            set => set.Include(x => x.ReactionRoleMessages).ThenInclude(x => x.ReactionRoles));

        if (gc.ReactionRoleMessages.Count >= 10)
            return false;

        gc.ReactionRoleMessages.Add(rrm);
        uow.SaveChanges();

        _models.AddOrUpdate(id, gc.ReactionRoleMessages, delegate { return gc.ReactionRoleMessages; });
        return true;
    }

    public void Remove(ulong id, int index)
    {
        using var uow = _db.GetDbContext();
        var gc = uow.GuildConfigsForId(id,
            set => set.Include(x => x.ReactionRoleMessages).ThenInclude(x => x.ReactionRoles));
        uow.Set<ReactionRole>().RemoveRange(gc.ReactionRoleMessages[index].ReactionRoles);
        gc.ReactionRoleMessages.RemoveAt(index);
        _models.AddOrUpdate(id, gc.ReactionRoleMessages, delegate { return gc.ReactionRoleMessages; });
        uow.SaveChanges();
    }

    /// <summary>
    ///     Adds a reaction role to the specified user.
    /// </summary>
    /// <param name="user">A Discord guild user.</param>
    /// <param name="dbRero">The database settings of this reaction role.</param>
    private Task AddReactionRoleAsync(SocketGuildUser user, ReactionRole dbRero)
    {
        var toAdd = user.Guild.GetRole(dbRero.RoleId);

        return toAdd is not null && !user.Roles.Contains(toAdd) ? user.AddRoleAsync(toAdd) : Task.CompletedTask;
    }

    /// <summary>
    ///     Removes the exclusive reaction roles and reactions from the specified user.
    /// </summary>
    /// <param name="reactionMessage">The Discord message that contains the reaction roles.</param>
    /// <param name="user">A Discord guild user.</param>
    /// <param name="reaction">The Discord reaction of the user.</param>
    /// <param name="dbReroMsg">The database entry of the reaction role message.</param>
    /// <param name="dbRero">The database settings of this reaction role.</param>
    /// <param name="cToken">A cancellation token to cancel the operation.</param>
    /// <exception cref="OperationCanceledException">Occurs when the operation is cancelled before it began.</exception>
    /// <exception cref="TaskCanceledException">Occurs when the operation is cancelled while it's still executing.</exception>
    private Task RemoveExclusiveReactionRoleAsync(
        Cacheable<IUserMessage, ulong> reactionMessage,
        SocketGuildUser user,
        SocketReaction reaction,
        ReactionRoleMessage dbReroMsg,
        ReactionRole dbRero,
        CancellationToken cToken = default)
    {
        cToken.ThrowIfCancellationRequested();

        var roleIds = dbReroMsg.ReactionRoles.Select(x => x.RoleId)
                               .Where(x => x != dbRero.RoleId)
                               .Select(x => user.Guild.GetRole(x))
                               .Where(x => x is not null);

        var removeReactionsTask = RemoveOldReactionsAsync(reactionMessage, user, reaction, cToken);

        var removeRolesTask = user.RemoveRolesAsync(roleIds);

        return Task.WhenAll(removeReactionsTask, removeRolesTask);
    }

    /// <summary>
    ///     Removes old reactions from an exclusive reaction role.
    /// </summary>
    /// <param name="reactionMessage">The Discord message that contains the reaction roles.</param>
    /// <param name="user">A Discord guild user.</param>
    /// <param name="reaction">The Discord reaction of the user.</param>
    /// <param name="cToken">A cancellation token to cancel the operation.</param>
    /// <exception cref="OperationCanceledException">Occurs when the operation is cancelled before it began.</exception>
    /// <exception cref="TaskCanceledException">Occurs when the operation is cancelled while it's still executing.</exception>
    private async Task RemoveOldReactionsAsync(
        Cacheable<IUserMessage, ulong> reactionMessage,
        SocketGuildUser user,
        SocketReaction reaction,
        CancellationToken cToken = default)
    {
        cToken.ThrowIfCancellationRequested();

        //if the role is exclusive,
        // remove all other reactions user added to the message
        var dl = await reactionMessage.GetOrDownloadAsync();
        foreach (var r in dl.Reactions)
        {
            if (r.Key.Name == reaction.Emote.Name)
                continue;
            try { await dl.RemoveReactionAsync(r.Key, user); }
            catch { }

            await Task.Delay(100, cToken);
        }
    }
}