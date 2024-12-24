using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using WizBot.Common.ModuleBehaviors;
using WizBot.Modules.Utility.Common;
using WizBot.Modules.Utility.Common.Exceptions;
using WizBot.Db.Models;
using System.Net;

namespace WizBot.Modules.Utility.Services;

public class StreamRoleService : IReadyExecutor, INService
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;
    private readonly ConcurrentHashSet<ulong> _srsEnabledGuilds = new();
    private readonly QueueRunner _queueRunner;

    public StreamRoleService(DiscordSocketClient client, DbService db, IBot bot)
    {
        _db = db;
        _client = client;

        _client.PresenceUpdated += OnPresenceUpdate;

        _queueRunner = new QueueRunner();
    }

    private Task OnPresenceUpdate(SocketUser user, SocketPresence? oldPresence, SocketPresence? newPresence)
    {
        _ = Task.Run(async () =>
        {
            if (oldPresence?.Activities?.Count != newPresence?.Activities?.Count)
            {
                var guildUsers = _client.Guilds
                                        .Select(x => x.GetUser(user.Id))
                                        .Where(x => x is not null);

                foreach (var guildUser in guildUsers)
                {
                    if (_srsEnabledGuilds.TryGetValue(guildUser.Guild.Id, out var s))
                        await RescanUser(guildUser, s);
                }
            }
        });

        return Task.CompletedTask;
    }

    public async Task OnReadyAsync()
    {
        await using (var uow = _db.GetDbContext())
        {
            _srsEnabledGuilds = (await uow.GetTable<StreamRoleSettings>()
                                          .Where(x => x.Enabled)
                                          .ToListAsyncLinqToDB())
                                .ToDictionary(x => x.GuildId, x => x)
                                .ToConcurrent();
        }

        await Task.WhenAll(_client.Guilds.Select(RescanUsers).WhenAll(), _queueRunner.RunAsync());
    }

    public async Task<bool> ApplyListAction(
        StreamRoleListType listType,
        IGuild guild,
        AddRemove action,
        ulong userId,
        string userName)
    {
        ArgumentNullException.ThrowIfNull(userName, nameof(userName));

        var success = false;
        await using (var uow = _db.GetDbContext())
        {
            var srs = uow.GetOrCreateStreamRoleSettings(guild.Id);

            if (listType == StreamRoleListType.Whitelist)
            {
                if (action == AddRemove.Rem)
                {
                    await using var ctx = _db.GetDbContext();
                    var deleted = await ctx.GetTable<StreamRoleWhitelistedUser>()
                                           .Where(x => x.StreamRoleSettingsId == srs.Id && x.UserId == userId)
                                           .DeleteAsync();

                    return deleted > 0;
                }
                else
                {
                    await using var ctx = _db.GetDbContext();
                    await ctx.GetTable<StreamRoleWhitelistedUser>()
                             .InsertAsync(() => new StreamRoleWhitelistedUser
                             {
                                 UserId = userId,
                                 Username = userName,
                                 StreamRoleSettingsId = srs.Id
                             });
                }
            }
            else
            {
                if (action == AddRemove.Rem)
                {
                    await using var ctx = _db.GetDbContext();
                    var deleted = await ctx.GetTable<StreamRoleBlacklistedUser>()
                                           .Where(x => x.StreamRoleSettingsId == srs.Id && x.UserId == userId)
                                           .DeleteAsync();

                    return deleted > 0;
                }
                else
                {
                    await using var ctx = _db.GetDbContext();
                    await ctx.GetTable<StreamRoleBlacklistedUser>()
                             .InsertAsync(() => new StreamRoleBlacklistedUser
                             {
                                 UserId = userId,
                                 Username = userName,
                                 StreamRoleSettingsId = srs.Id
                             });
                }
            }
        }

        if (success)
            await RescanUsers(guild);
        return success;
    }

    /// <summary>
    ///     Sets keyword on a guild and updates the cache.
    /// </summary>
    /// <param name="guild">Guild Id</param>
    /// <param name="keyword">Keyword to set</param>
    /// <returns>The keyword set</returns>
    public async Task<string?> SetKeyword(IGuild guild, string? keyword)
    {
        keyword = keyword?.Trim().ToLowerInvariant();

        await using (var uow = _db.GetDbContext())
        {
            var streamRoleSettings = uow.GetOrCreateStreamRoleSettings(guild.Id);

            streamRoleSettings.Keyword = keyword;
            UpdateCache(guild.Id, streamRoleSettings);
            await uow.SaveChangesAsync();
        }

        await RescanUsers(guild);
        return keyword;
    }

    /// <summary>
    ///     Gets the currently set keyword on a guild.
    /// </summary>
    /// <param name="guildId">Guild Id</param>
    /// <returns>The keyword set</returns>
    public string GetKeyword(ulong guildId)
    {
        if (_srsEnabledGuilds.TryGetValue(guildId, out var outSetting))
            return outSetting.Keyword;

        StreamRoleSettings setting;
        using (var uow = _db.GetDbContext())
        {
            setting = uow.GetOrCreateStreamRoleSettings(guildId);
        }

        UpdateCache(guildId, setting);

        return setting.Keyword;
    }

    /// <summary>
    ///     Sets the role to monitor, and a role to which to add to
    ///     the user who starts streaming in the monitored role.
    /// </summary>
    /// <param name="fromRole">Role to monitor</param>
    /// <param name="addRole">Role to add to the user</param>
    public async Task SetStreamRole(IRole fromRole, IRole addRole)
    {
        ArgumentNullException.ThrowIfNull(fromRole, nameof(fromRole));
        ArgumentNullException.ThrowIfNull(addRole, nameof(addRole));

        StreamRoleSettings setting;
        await using (var uow = _db.GetDbContext())
        {
            var streamRoleSettings = uow.GetOrCreateStreamRoleSettings(fromRole.Guild.Id);

            streamRoleSettings.Enabled = true;
            streamRoleSettings.AddRoleId = addRole.Id;
            streamRoleSettings.FromRoleId = fromRole.Id;

            setting = streamRoleSettings;
            await uow.SaveChangesAsync();
        }

        UpdateCache(fromRole.Guild.Id, setting);

        foreach (var usr in await fromRole.GetMembersAsync())
        {
            await RescanUser(usr, setting, addRole);
        }
    }

    /// <summary>
    ///     Stops the stream role feature on the specified guild.
    /// </summary>
    /// <param name="guild">Guild</param>
    /// <param name="cleanup">Whether to rescan users</param>
    public async Task StopStreamRole(IGuild guild, bool cleanup = false)
    {
        await using (var uow = _db.GetDbContext())
        {
            var streamRoleSettings = uow.GetOrCreateStreamRoleSettings(guild.Id);
            streamRoleSettings.Enabled = false;
            streamRoleSettings.AddRoleId = 0;
            streamRoleSettings.FromRoleId = 0;
            await uow.SaveChangesAsync();
        }

        if (_srsEnabledGuilds.TryRemove(guild.Id, out _) && cleanup)
            await RescanUsers(guild);
    }

    private async ValueTask RescanUser(IGuildUser user, StreamRoleSettings setting, IRole? addRole = null)
        => await _queueRunner.EnqueueAsync(() => RescanUserInternal(user, setting, addRole));

    private async Task RescanUserInternal(IGuildUser user, StreamRoleSettings setting, IRole? addRole = null)
    {
        if (user.IsBot)
            return;

        var g = (StreamingGame?)user.Activities.FirstOrDefault(a
            => a is StreamingGame
               && (string.IsNullOrWhiteSpace(setting.Keyword)
                   || a.Name.ToUpperInvariant().Contains(setting.Keyword.ToUpperInvariant())
                   || setting.Whitelist.Any(x => x.UserId == user.Id)));

        if (g is not null
            && setting.Enabled
            && setting.Blacklist.All(x => x.UserId != user.Id)
            && user.RoleIds.Contains(setting.FromRoleId))
        {
            await _queueRunner.EnqueueAsync(async () =>
            {
                try
                {
                    addRole ??= user.Guild.GetRole(setting.AddRoleId);
                    if (addRole is null)
                    {
                        await StopStreamRole(user.Guild);
                        Log.Warning("Stream role in server {RoleId} no longer exists. Stopping", setting.AddRoleId);
                        return;
                    }

                    //check if he doesn't have addrole already, to avoid errors
                    if (!user.RoleIds.Contains(addRole.Id))
                    {
                        await user.AddRoleAsync(addRole);
                        Log.Information("Added stream role to user {User} in {Server} server",
                            user.ToString(),
                            user.Guild.ToString());
                    }
                }
                catch (HttpException ex) when (ex.HttpCode == HttpStatusCode.Forbidden)
                {
                    await StopStreamRole(user.Guild);
                    Log.Warning(ex, "Error adding stream role(s). Forcibly disabling stream role feature");
                    throw new StreamRolePermissionException();
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Failed adding stream role");
                }
            });
        }
        else
        {
            //check if user is in the addrole
            if (user.RoleIds.Contains(setting.AddRoleId))
            {
                await _queueRunner.EnqueueAsync(async () =>
                {
                    try
                    {
                        addRole ??= user.Guild.GetRole(setting.AddRoleId);
                        if (addRole is null)
                        {
                            await StopStreamRole(user.Guild);
                            Log.Warning(
                                "Addrole doesn't exist in {GuildId} server. Forcibly disabling stream role feature",
                                user.Guild.Id);
                            return;
                        }

                        // need to check again in case queuer is taking too long to execute
                        if (user.RoleIds.Contains(setting.AddRoleId))
                        {
                            await user.RemoveRoleAsync(addRole);
                        }

                        Log.Information("Removed stream role from the user {User} in {Server} server",
                            user.ToString(),
                            user.Guild.ToString());
                    }
                    catch (HttpException ex)
                    {
                        if (ex.HttpCode == HttpStatusCode.Forbidden)
                        {
                            await StopStreamRole(user.Guild);
                            Log.Warning(ex, "Error removing stream role(s). Forcibly disabling stream role feature");
                        }
                    }
                });
            }
        }
    }

    private async Task RescanUsers(IGuild guild)
    {
        if (!_srsEnabledGuilds.Contains(guild.Id))
            return;

        var settings = await GetSettingsAsync(guild.Id);

        var addRole = guild.GetRole(setting.AddRoleId);
        if (addRole is null)
            return;

        if (setting.Enabled)
        {
            var users = await guild.GetUsersAsync(CacheMode.CacheOnly);
            foreach (var usr in users.Where(x
                         => x.RoleIds.Contains(setting.FromRoleId) || x.RoleIds.Contains(addRole.Id)))
            {
                if (usr is { } x)
                    await RescanUser(x, setting, addRole);
            }
        }
    }
}