#nullable disable
using Microsoft.EntityFrameworkCore;
using NadekoBot.Db;
using NadekoBot.Services.Database.Models;

namespace NadekoBot.Modules.Administration.Services;

public class VcRoleService : INService
{
    public ConcurrentDictionary<ulong, ConcurrentDictionary<ulong, IRole>> VcRoles { get; }
    public ConcurrentDictionary<ulong, ConcurrentQueue<(bool, IGuildUser, IRole)>> ToAssign { get; }
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;

    public VcRoleService(DiscordSocketClient client, Bot bot, DbService db)
    {
        _db = db;
        _client = client;

        _client.UserVoiceStateUpdated += ClientOnUserVoiceStateUpdated;
        VcRoles = new();
        ToAssign = new();

        using (var uow = db.GetDbContext())
        {
            var guildIds = client.Guilds.Select(x => x.Id).ToList();
            uow.Set<GuildConfig>()
               .AsQueryable()
               .Include(x => x.VcRoleInfos)
               .Where(x => guildIds.Contains(x.GuildId))
               .AsEnumerable()
               .Select(InitializeVcRole)
               .WhenAll();
        }

        Task.Run(async () =>
        {
            while (true)
            {
                Task Selector(ConcurrentQueue<(bool, IGuildUser, IRole)> queue)
                {
                    return Task.Run(async () =>
                    {
                        while (queue.TryDequeue(out var item))
                        {
                            var (add, user, role) = item;

                            try
                            {
                                if (add)
                                {
                                    if (!user.RoleIds.Contains(role.Id))
                                        await user.AddRoleAsync(role);
                                }
                                else
                                {
                                    if (user.RoleIds.Contains(role.Id))
                                        await user.RemoveRoleAsync(role);
                                }
                            }
                            catch
                            {
                            }

                            await Task.Delay(250);
                        }
                    });
                }

                await ToAssign.Values.Select(Selector).Append(Task.Delay(1000)).WhenAll();
            }
        });

        _client.LeftGuild += _client_LeftGuild;
        bot.JoinedGuild += Bot_JoinedGuild;
    }

    private Task Bot_JoinedGuild(GuildConfig arg)
    {
        // includeall no longer loads vcrole
        // need to load new guildconfig with vc role included 
        using (var uow = _db.GetDbContext())
        {
            var configWithVcRole = uow.GuildConfigsForId(arg.GuildId, set => set.Include(x => x.VcRoleInfos));
            _ = InitializeVcRole(configWithVcRole);
        }

        return Task.CompletedTask;
    }

    private Task _client_LeftGuild(SocketGuild arg)
    {
        VcRoles.TryRemove(arg.Id, out _);
        ToAssign.TryRemove(arg.Id, out _);
        return Task.CompletedTask;
    }

    private async Task InitializeVcRole(GuildConfig gconf)
    {
        var g = _client.GetGuild(gconf.GuildId);
        if (g is null)
            return;

        var infos = new ConcurrentDictionary<ulong, IRole>();
        var missingRoles = new List<VcRoleInfo>();
        VcRoles.AddOrUpdate(gconf.GuildId, infos, delegate { return infos; });
        foreach (var ri in gconf.VcRoleInfos)
        {
            var role = g.GetRole(ri.RoleId);
            if (role is null)
            {
                missingRoles.Add(ri);
                continue;
            }

            infos.TryAdd(ri.VoiceChannelId, role);
        }

        if (missingRoles.Any())
        {
            await using var uow = _db.GetDbContext();
            uow.RemoveRange(missingRoles);
            await uow.SaveChangesAsync();
            
            Log.Warning("Removed {MissingRoleCount} missing roles from {ServiceName}",
                missingRoles.Count,
                nameof(VcRoleService));
        }
    }

    public void AddVcRole(ulong guildId, IRole role, ulong vcId)
    {
        if (role is null)
            throw new ArgumentNullException(nameof(role));

        var guildVcRoles = VcRoles.GetOrAdd(guildId, new ConcurrentDictionary<ulong, IRole>());

        guildVcRoles.AddOrUpdate(vcId, role, (_, _) => role);
        using var uow = _db.GetDbContext();
        var conf = uow.GuildConfigsForId(guildId, set => set.Include(x => x.VcRoleInfos));
        var toDelete = conf.VcRoleInfos.FirstOrDefault(x => x.VoiceChannelId == vcId); // remove old one
        if (toDelete is not null)
            uow.Remove(toDelete);
        conf.VcRoleInfos.Add(new()
        {
            VoiceChannelId = vcId,
            RoleId = role.Id
        }); // add new one
        uow.SaveChanges();
    }

    public bool RemoveVcRole(ulong guildId, ulong vcId)
    {
        if (!VcRoles.TryGetValue(guildId, out var guildVcRoles))
            return false;

        if (!guildVcRoles.TryRemove(vcId, out _))
            return false;

        using var uow = _db.GetDbContext();
        var conf = uow.GuildConfigsForId(guildId, set => set.Include(x => x.VcRoleInfos));
        var toRemove = conf.VcRoleInfos.Where(x => x.VoiceChannelId == vcId).ToList();
        uow.RemoveRange(toRemove);
        uow.SaveChanges();

        return true;
    }

    private Task ClientOnUserVoiceStateUpdated(SocketUser usr, SocketVoiceState oldState, SocketVoiceState newState)
    {
        if (usr is not SocketGuildUser gusr)
            return Task.CompletedTask;

        var oldVc = oldState.VoiceChannel;
        var newVc = newState.VoiceChannel;
        _ = Task.Run(() =>
        {
            try
            {
                if (oldVc != newVc)
                {
                    ulong guildId;
                    guildId = newVc?.Guild.Id ?? oldVc.Guild.Id;

                    if (VcRoles.TryGetValue(guildId, out var guildVcRoles))
                    {
                        //remove old
                        if (oldVc is not null && guildVcRoles.TryGetValue(oldVc.Id, out var role))
                            Assign(false, gusr, role);
                        //add new
                        if (newVc is not null && guildVcRoles.TryGetValue(newVc.Id, out role))
                            Assign(true, gusr, role);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error in VcRoleService VoiceStateUpdate");
            }
        });
        return Task.CompletedTask;
    }

    private void Assign(bool v, SocketGuildUser gusr, IRole role)
    {
        var queue = ToAssign.GetOrAdd(gusr.Guild.Id, new ConcurrentQueue<(bool, IGuildUser, IRole)>());
        queue.Enqueue((v, gusr, role));
    }
}