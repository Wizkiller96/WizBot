#nullable disable
using Microsoft.EntityFrameworkCore;
using NadekoBot.Db;
using NadekoBot.Services.Database.Models;

namespace NadekoBot.Modules.Administration.Services;

public enum MuteType
{
    Voice,
    Chat,
    All
}

public class MuteService : INService
{
    public enum TimerType { Mute, Ban, AddRole }

    private static readonly OverwritePermissions _denyOverwrite = new(addReactions: PermValue.Deny,
        sendMessages: PermValue.Deny,
        attachFiles: PermValue.Deny);

    public event Action<IGuildUser, IUser, MuteType, string> UserMuted = delegate { };
    public event Action<IGuildUser, IUser, MuteType, string> UserUnmuted = delegate { };

    public ConcurrentDictionary<ulong, string> GuildMuteRoles { get; }
    public ConcurrentDictionary<ulong, ConcurrentHashSet<ulong>> MutedUsers { get; }

    public ConcurrentDictionary<ulong, ConcurrentDictionary<(ulong, TimerType), Timer>> UnTimers { get; } = new();

    private readonly DiscordSocketClient _client;
    private readonly DbService _db;
    private readonly IEmbedBuilderService _eb;

    public MuteService(DiscordSocketClient client, DbService db, IEmbedBuilderService eb)
    {
        _client = client;
        _db = db;
        _eb = eb;

        using (var uow = db.GetDbContext())
        {
            var guildIds = client.Guilds.Select(x => x.Id).ToList();
            var configs = uow.Set<GuildConfig>()
                             .AsNoTracking()
                             .AsSplitQuery()
                             .Include(x => x.MutedUsers)
                             .Include(x => x.UnbanTimer)
                             .Include(x => x.UnmuteTimers)
                             .Include(x => x.UnroleTimer)
                             .Where(x => guildIds.Contains(x.GuildId))
                             .ToList();

            GuildMuteRoles = configs.Where(c => !string.IsNullOrWhiteSpace(c.MuteRoleName))
                                    .ToDictionary(c => c.GuildId, c => c.MuteRoleName)
                                    .ToConcurrent();

            MutedUsers = new(configs.ToDictionary(k => k.GuildId,
                v => new ConcurrentHashSet<ulong>(v.MutedUsers.Select(m => m.UserId))));

            var max = TimeSpan.FromDays(49);

            foreach (var conf in configs)
            {
                foreach (var x in conf.UnmuteTimers)
                {
                    TimeSpan after;
                    if (x.UnmuteAt - TimeSpan.FromMinutes(2) <= DateTime.UtcNow)
                        after = TimeSpan.FromMinutes(2);
                    else
                    {
                        var unmute = x.UnmuteAt - DateTime.UtcNow;
                        after = unmute > max ? max : unmute;
                    }

                    StartUn_Timer(conf.GuildId, x.UserId, after, TimerType.Mute);
                }

                foreach (var x in conf.UnbanTimer)
                {
                    TimeSpan after;
                    if (x.UnbanAt - TimeSpan.FromMinutes(2) <= DateTime.UtcNow)
                        after = TimeSpan.FromMinutes(2);
                    else
                    {
                        var unban = x.UnbanAt - DateTime.UtcNow;
                        after = unban > max ? max : unban;
                    }

                    StartUn_Timer(conf.GuildId, x.UserId, after, TimerType.Ban);
                }

                foreach (var x in conf.UnroleTimer)
                {
                    TimeSpan after;
                    if (x.UnbanAt - TimeSpan.FromMinutes(2) <= DateTime.UtcNow)
                        after = TimeSpan.FromMinutes(2);
                    else
                    {
                        var unban = x.UnbanAt - DateTime.UtcNow;
                        after = unban > max ? max : unban;
                    }

                    StartUn_Timer(conf.GuildId, x.UserId, after, TimerType.AddRole, x.RoleId);
                }
            }

            _client.UserJoined += Client_UserJoined;
        }

        UserMuted += OnUserMuted;
        UserUnmuted += OnUserUnmuted;
    }

    private void OnUserMuted(
        IGuildUser user,
        IUser mod,
        MuteType type,
        string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return;

        _ = Task.Run(() => user.SendMessageAsync(embed: _eb.Create()
                                                           .WithDescription(
                                                               $"You've been muted in {user.Guild} server")
                                                           .AddField("Mute Type", type.ToString())
                                                           .AddField("Moderator", mod.ToString())
                                                           .AddField("Reason", reason)
                                                           .Build()));
    }

    private void OnUserUnmuted(
        IGuildUser user,
        IUser mod,
        MuteType type,
        string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return;

        _ = Task.Run(() => user.SendMessageAsync(embed: _eb.Create()
                                                           .WithDescription(
                                                               $"You've been unmuted in {user.Guild} server")
                                                           .AddField("Unmute Type", type.ToString())
                                                           .AddField("Moderator", mod.ToString())
                                                           .AddField("Reason", reason)
                                                           .Build()));
    }

    private Task Client_UserJoined(IGuildUser usr)
    {
        try
        {
            MutedUsers.TryGetValue(usr.Guild.Id, out var muted);

            if (muted is null || !muted.Contains(usr.Id))
                return Task.CompletedTask;
            _ = Task.Run(() => MuteUser(usr, _client.CurrentUser, reason: "Sticky mute"));
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error in MuteService UserJoined event");
        }

        return Task.CompletedTask;
    }

    public async Task SetMuteRoleAsync(ulong guildId, string name)
    {
        await using var uow = _db.GetDbContext();
        var config = uow.GuildConfigsForId(guildId, set => set);
        config.MuteRoleName = name;
        GuildMuteRoles.AddOrUpdate(guildId, name, (_, _) => name);
        await uow.SaveChangesAsync();
    }

    public async Task MuteUser(
        IGuildUser usr,
        IUser mod,
        MuteType type = MuteType.All,
        string reason = "")
    {
        if (type == MuteType.All)
        {
            try { await usr.ModifyAsync(x => x.Mute = true); }
            catch { }

            var muteRole = await GetMuteRole(usr.Guild);
            if (!usr.RoleIds.Contains(muteRole.Id))
                await usr.AddRoleAsync(muteRole);
            StopTimer(usr.GuildId, usr.Id, TimerType.Mute);
            await using (var uow = _db.GetDbContext())
            {
                var config = uow.GuildConfigsForId(usr.Guild.Id,
                    set => set.Include(gc => gc.MutedUsers).Include(gc => gc.UnmuteTimers));
                config.MutedUsers.Add(new()
                {
                    UserId = usr.Id
                });
                if (MutedUsers.TryGetValue(usr.Guild.Id, out var muted))
                    muted.Add(usr.Id);

                config.UnmuteTimers.RemoveWhere(x => x.UserId == usr.Id);

                await uow.SaveChangesAsync();
            }

            UserMuted(usr, mod, MuteType.All, reason);
        }
        else if (type == MuteType.Voice)
        {
            try
            {
                await usr.ModifyAsync(x => x.Mute = true);
                UserMuted(usr, mod, MuteType.Voice, reason);
            }
            catch { }
        }
        else if (type == MuteType.Chat)
        {
            await usr.AddRoleAsync(await GetMuteRole(usr.Guild));
            UserMuted(usr, mod, MuteType.Chat, reason);
        }
    }

    public async Task UnmuteUser(
        ulong guildId,
        ulong usrId,
        IUser mod,
        MuteType type = MuteType.All,
        string reason = "")
    {
        var usr = _client.GetGuild(guildId)?.GetUser(usrId);
        if (type == MuteType.All)
        {
            StopTimer(guildId, usrId, TimerType.Mute);
            await using (var uow = _db.GetDbContext())
            {
                var config = uow.GuildConfigsForId(guildId,
                    set => set.Include(gc => gc.MutedUsers).Include(gc => gc.UnmuteTimers));
                var match = new MutedUserId
                {
                    UserId = usrId
                };
                var toRemove = config.MutedUsers.FirstOrDefault(x => x.Equals(match));
                if (toRemove is not null)
                    uow.Remove(toRemove);
                if (MutedUsers.TryGetValue(guildId, out var muted))
                    muted.TryRemove(usrId);

                config.UnmuteTimers.RemoveWhere(x => x.UserId == usrId);

                await uow.SaveChangesAsync();
            }

            if (usr is not null)
            {
                try { await usr.ModifyAsync(x => x.Mute = false); }
                catch { }

                try { await usr.RemoveRoleAsync(await GetMuteRole(usr.Guild)); }
                catch
                {
                    /*ignore*/
                }

                UserUnmuted(usr, mod, MuteType.All, reason);
            }
        }
        else if (type == MuteType.Voice)
        {
            if (usr is null)
                return;
            try
            {
                await usr.ModifyAsync(x => x.Mute = false);
                UserUnmuted(usr, mod, MuteType.Voice, reason);
            }
            catch { }
        }
        else if (type == MuteType.Chat)
        {
            if (usr is null)
                return;
            await usr.RemoveRoleAsync(await GetMuteRole(usr.Guild));
            UserUnmuted(usr, mod, MuteType.Chat, reason);
        }
    }

    public async Task<IRole> GetMuteRole(IGuild guild)
    {
        if (guild is null)
            throw new ArgumentNullException(nameof(guild));

        const string defaultMuteRoleName = "nadeko-mute";

        var muteRoleName = GuildMuteRoles.GetOrAdd(guild.Id, defaultMuteRoleName);

        var muteRole = guild.Roles.FirstOrDefault(r => r.Name == muteRoleName);
        if (muteRole is null)
            //if it doesn't exist, create it
        {
            try { muteRole = await guild.CreateRoleAsync(muteRoleName, isMentionable: false); }
            catch
            {
                //if creations fails,  maybe the name is not correct, find default one, if doesn't work, create default one
                muteRole = guild.Roles.FirstOrDefault(r => r.Name == muteRoleName)
                           ?? await guild.CreateRoleAsync(defaultMuteRoleName, isMentionable: false);
            }
        }

        foreach (var toOverwrite in await guild.GetTextChannelsAsync())
        {
            try
            {
                if (!toOverwrite.PermissionOverwrites.Any(x => x.TargetId == muteRole.Id
                                                               && x.TargetType == PermissionTarget.Role))
                {
                    await toOverwrite.AddPermissionOverwriteAsync(muteRole, _denyOverwrite);

                    await Task.Delay(200);
                }
            }
            catch
            {
                // ignored
            }
        }

        return muteRole;
    }

    public async Task TimedMute(
        IGuildUser user,
        IUser mod,
        TimeSpan after,
        MuteType muteType = MuteType.All,
        string reason = "")
    {
        await MuteUser(user, mod, muteType, reason); // mute the user. This will also remove any previous unmute timers
        await using (var uow = _db.GetDbContext())
        {
            var config = uow.GuildConfigsForId(user.GuildId, set => set.Include(x => x.UnmuteTimers));
            config.UnmuteTimers.Add(new()
            {
                UserId = user.Id,
                UnmuteAt = DateTime.UtcNow + after
            }); // add teh unmute timer to the database
            uow.SaveChanges();
        }

        StartUn_Timer(user.GuildId, user.Id, after, TimerType.Mute); // start the timer
    }

    public async Task TimedBan(
        IGuild guild,
        IUser user,
        TimeSpan after,
        string reason)
    {
        await guild.AddBanAsync(user.Id, 0, reason);
        await using (var uow = _db.GetDbContext())
        {
            var config = uow.GuildConfigsForId(guild.Id, set => set.Include(x => x.UnbanTimer));
            config.UnbanTimer.Add(new()
            {
                UserId = user.Id,
                UnbanAt = DateTime.UtcNow + after
            }); // add teh unmute timer to the database
            uow.SaveChanges();
        }

        StartUn_Timer(guild.Id, user.Id, after, TimerType.Ban); // start the timer
    }

    public async Task TimedRole(
        IGuildUser user,
        TimeSpan after,
        string reason,
        IRole role)
    {
        await user.AddRoleAsync(role);
        await using (var uow = _db.GetDbContext())
        {
            var config = uow.GuildConfigsForId(user.GuildId, set => set.Include(x => x.UnroleTimer));
            config.UnroleTimer.Add(new()
            {
                UserId = user.Id,
                UnbanAt = DateTime.UtcNow + after,
                RoleId = role.Id
            }); // add teh unmute timer to the database
            uow.SaveChanges();
        }

        StartUn_Timer(user.GuildId, user.Id, after, TimerType.AddRole, role.Id); // start the timer
    }

    public void StartUn_Timer(
        ulong guildId,
        ulong userId,
        TimeSpan after,
        TimerType type,
        ulong? roleId = null)
    {
        //load the unmute timers for this guild
        var userUnTimers = UnTimers.GetOrAdd(guildId, new ConcurrentDictionary<(ulong, TimerType), Timer>());

        //unmute timer to be added
        var toAdd = new Timer(async _ =>
            {
                if (type == TimerType.Ban)
                {
                    try
                    {
                        RemoveTimerFromDb(guildId, userId, type);
                        StopTimer(guildId, userId, type);
                        var guild = _client.GetGuild(guildId); // load the guild
                        if (guild is not null)
                            await guild.RemoveBanAsync(userId);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "Couldn't unban user {UserId} in guild {GuildId}", userId, guildId);
                    }
                }
                else if (type == TimerType.AddRole)
                {
                    try
                    {
                        if (roleId is null)
                            return;

                        RemoveTimerFromDb(guildId, userId, type);
                        StopTimer(guildId, userId, type);
                        var guild = _client.GetGuild(guildId);
                        var user = guild?.GetUser(userId);
                        var role = guild?.GetRole(roleId.Value);
                        if (guild is not null && user is not null && user.Roles.Contains(role))
                            await user.RemoveRoleAsync(role);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "Couldn't remove role from user {UserId} in guild {GuildId}", userId, guildId);
                    }
                }
                else
                {
                    try
                    {
                        // unmute the user, this will also remove the timer from the db
                        await UnmuteUser(guildId, userId, _client.CurrentUser, reason: "Timed mute expired");
                    }
                    catch (Exception ex)
                    {
                        RemoveTimerFromDb(guildId, userId, type); // if unmute errored, just remove unmute from db
                        Log.Warning(ex, "Couldn't unmute user {UserId} in guild {GuildId}", userId, guildId);
                    }
                }
            },
            null,
            after,
            Timeout.InfiniteTimeSpan);

        //add it, or stop the old one and add this one
        userUnTimers.AddOrUpdate((userId, type),
            _ => toAdd,
            (_, old) =>
            {
                old.Change(Timeout.Infinite, Timeout.Infinite);
                return toAdd;
            });
    }

    public void StopTimer(ulong guildId, ulong userId, TimerType type)
    {
        if (!UnTimers.TryGetValue(guildId, out var userTimer))
            return;

        if (userTimer.TryRemove((userId, type), out var removed))
            removed.Change(Timeout.Infinite, Timeout.Infinite);
    }

    private void RemoveTimerFromDb(ulong guildId, ulong userId, TimerType type)
    {
        using var uow = _db.GetDbContext();
        object toDelete;
        if (type == TimerType.Mute)
        {
            var config = uow.GuildConfigsForId(guildId, set => set.Include(x => x.UnmuteTimers));
            toDelete = config.UnmuteTimers.FirstOrDefault(x => x.UserId == userId);
        }
        else
        {
            var config = uow.GuildConfigsForId(guildId, set => set.Include(x => x.UnbanTimer));
            toDelete = config.UnbanTimer.FirstOrDefault(x => x.UserId == userId);
        }

        if (toDelete is not null)
            uow.Remove(toDelete);
        uow.SaveChanges();
    }
}