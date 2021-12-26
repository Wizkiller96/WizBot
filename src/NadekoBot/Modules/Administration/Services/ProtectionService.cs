using System.Threading.Channels;
using NadekoBot.Modules.Administration.Common;
using NadekoBot.Services.Database.Models;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Db;

namespace NadekoBot.Modules.Administration.Services;

public class ProtectionService : INService
{
    private readonly ConcurrentDictionary<ulong, AntiRaidStats> _antiRaidGuilds = new();

    private readonly ConcurrentDictionary<ulong, AntiSpamStats> _antiSpamGuilds = new();

    private readonly ConcurrentDictionary<ulong, AntiAltStats> _antiAltGuilds = new();
        
    public event Func<PunishmentAction, ProtectionType, IGuildUser[], Task> OnAntiProtectionTriggered
        = delegate { return Task.CompletedTask; };

    private readonly DiscordSocketClient _client;
    private readonly MuteService _mute;
    private readonly DbService _db;
    private readonly UserPunishService _punishService;
        
    private readonly Channel<PunishQueueItem> PunishUserQueue =
        System.Threading.Channels.Channel.CreateUnbounded<PunishQueueItem>(new()
        {
            SingleReader = true,
            SingleWriter = false
        });

    public ProtectionService(DiscordSocketClient client, Bot bot,
        MuteService mute, DbService db, UserPunishService punishService)
    { 
        _client = client;
        _mute = mute;
        _db = db;
        _punishService = punishService;

        var ids = client.GetGuildIds();
        using (var uow = db.GetDbContext())
        {
            var configs = uow.Set<GuildConfig>()
                .AsQueryable()
                .Include(x => x.AntiRaidSetting)
                .Include(x => x.AntiSpamSetting)
                .ThenInclude(x => x.IgnoredChannels)
                .Include(x => x.AntiAltSetting)
                .Where(x => ids.Contains(x.GuildId))
                .ToList();

            foreach (var gc in configs)
            {
                Initialize(gc);
            }
        }

        _client.MessageReceived += HandleAntiSpam;
        _client.UserJoined += HandleUserJoined;

        bot.JoinedGuild += _bot_JoinedGuild;
        _client.LeftGuild += _client_LeftGuild;
            
        _ = Task.Run(RunQueue);
    }

    private async Task RunQueue()
    {
        while (true)
        {
            var item = await PunishUserQueue.Reader.ReadAsync();

            var muteTime = item.MuteTime;
            var gu = item.User;
            try
            {
                await _punishService.ApplyPunishment(gu.Guild, gu, _client.CurrentUser,
                    item.Action, muteTime, item.RoleId, $"{item.Type} Protection");
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error in punish queue: {Message}", ex.Message);
            }
            finally
            {
                await Task.Delay(1000);
            }
        }
    }

    private Task _client_LeftGuild(SocketGuild guild)
    {
        var _ = Task.Run(async () =>
        {
            TryStopAntiRaid(guild.Id);
            TryStopAntiSpam(guild.Id);
            await TryStopAntiAlt(guild.Id);
        });
        return Task.CompletedTask;
    }

    private Task _bot_JoinedGuild(GuildConfig gc)
    {
        using var uow = _db.GetDbContext();
        var gcWithData = uow.GuildConfigsForId(gc.GuildId,
            set => set
                .Include(x => x.AntiRaidSetting)
                .Include(x => x.AntiAltSetting)
                .Include(x => x.AntiSpamSetting)
                .ThenInclude(x => x.IgnoredChannels));

        Initialize(gcWithData);
        return Task.CompletedTask;
    }

    private void Initialize(GuildConfig gc)
    {
        var raid = gc.AntiRaidSetting;
        var spam = gc.AntiSpamSetting;

        if (raid != null)
        {
            var raidStats = new AntiRaidStats() { AntiRaidSettings = raid };
            _antiRaidGuilds[gc.GuildId] = raidStats;
        }

        if (spam != null)
            _antiSpamGuilds[gc.GuildId] = new() { AntiSpamSettings = spam };

        var alt = gc.AntiAltSetting;
        if (alt is not null)
            _antiAltGuilds[gc.GuildId] = new(alt);
    }

    private Task HandleUserJoined(SocketGuildUser user)
    {
        if (user.IsBot)
            return Task.CompletedTask;
            
        _antiRaidGuilds.TryGetValue(user.Guild.Id, out var maybeStats);
        _antiAltGuilds.TryGetValue(user.Guild.Id, out var maybeAlts);
            
        if (maybeStats is null && maybeAlts is null)
            return Task.CompletedTask;

        _ = Task.Run(async () =>
        {
            if (maybeAlts is { } alts)
            {
                if (user.CreatedAt != default)
                {
                    var diff = DateTime.UtcNow - user.CreatedAt.UtcDateTime;
                    if (diff < alts.MinAge)
                    {
                        alts.Increment();
                            
                        await PunishUsers(
                            alts.Action,
                            ProtectionType.Alting,
                            alts.ActionDurationMinutes, 
                            alts.RoleId,
                            user);
                            
                        return;
                    }
                }
            }
                
            try
            {
                if (maybeStats is not { } stats || !stats.RaidUsers.Add(user))
                    return;
                    
                ++stats.UsersCount;

                if (stats.UsersCount >= stats.AntiRaidSettings.UserThreshold)
                {
                    var users = stats.RaidUsers.ToArray();
                    stats.RaidUsers.Clear();
                    var settings = stats.AntiRaidSettings;

                    await PunishUsers(settings.Action, ProtectionType.Raiding,
                        settings.PunishDuration, null,  users).ConfigureAwait(false);
                }
                await Task.Delay(1000 * stats.AntiRaidSettings.Seconds).ConfigureAwait(false);

                stats.RaidUsers.TryRemove(user);
                --stats.UsersCount;

            }
            catch
            {
                // ignored
            }
        });
        return Task.CompletedTask;
    }

    private Task HandleAntiSpam(SocketMessage arg)
    {
        if (arg is not SocketUserMessage msg || msg.Author.IsBot)
            return Task.CompletedTask;

        if (msg.Channel is not ITextChannel channel)
            return Task.CompletedTask;
        var _ = Task.Run(async () =>
        {
            try
            {
                if (!_antiSpamGuilds.TryGetValue(channel.Guild.Id, out var spamSettings) ||
                    spamSettings.AntiSpamSettings.IgnoredChannels.Contains(new()
                    {
                        ChannelId = channel.Id
                    }))
                    return;

                var stats = spamSettings.UserStats.AddOrUpdate(msg.Author.Id, id => new(msg),
                    (id, old) =>
                    {
                        old.ApplyNextMessage(msg); return old;
                    });

                if (stats.Count >= spamSettings.AntiSpamSettings.MessageThreshold)
                {
                    if (spamSettings.UserStats.TryRemove(msg.Author.Id, out stats))
                    {
                        stats.Dispose();
                        var settings = spamSettings.AntiSpamSettings;
                        await PunishUsers(settings.Action, ProtectionType.Spamming, settings.MuteTime,
                                settings.RoleId, (IGuildUser)msg.Author)
                            .ConfigureAwait(false);
                    }
                }
            }
            catch
            {
                // ignored
            }
        });
        return Task.CompletedTask;
    }

    private async Task PunishUsers(PunishmentAction action, ProtectionType pt, int muteTime, ulong? roleId,
        params IGuildUser[] gus)
    {
        Log.Information(
            "[{PunishType}] - Punishing [{Count}] users with [{PunishAction}] in {GuildName} guild",
            pt,
            gus.Length,
            action,
            gus[0].Guild.Name);
            
        foreach (var gu in gus)
        {
            await PunishUserQueue.Writer.WriteAsync(new()
            {
                Action = action,
                Type = pt,
                User = gu,
                MuteTime = muteTime,
                RoleId = roleId
            });
        }

        _ = OnAntiProtectionTriggered(action, pt, gus);
    }

    public async Task<AntiRaidStats> StartAntiRaidAsync(ulong guildId, int userThreshold, int seconds,
        PunishmentAction action, int minutesDuration)
    {
        var g = _client.GetGuild(guildId);
        await _mute.GetMuteRole(g).ConfigureAwait(false);

        if (action == PunishmentAction.AddRole)
            return null;
            
        if (!IsDurationAllowed(action))
            minutesDuration = 0;

        var stats = new AntiRaidStats()
        {
            AntiRaidSettings = new()
            {
                Action = action,
                Seconds = seconds,
                UserThreshold = userThreshold,
                PunishDuration = minutesDuration
            }
        };

        _antiRaidGuilds.AddOrUpdate(guildId, stats, (key, old) => stats);

        await using var uow = _db.GetDbContext();
        var gc = uow.GuildConfigsForId(guildId, set => set.Include(x => x.AntiRaidSetting));

        gc.AntiRaidSetting = stats.AntiRaidSettings;
        await uow.SaveChangesAsync();

        return stats;
    }

    public bool TryStopAntiRaid(ulong guildId)
    {
        if (_antiRaidGuilds.TryRemove(guildId, out _))
        {
            using var uow = _db.GetDbContext();
            var gc = uow.GuildConfigsForId(guildId, set => set.Include(x => x.AntiRaidSetting));

            gc.AntiRaidSetting = null;
            uow.SaveChanges();
            return true;
        }
        return false;
    }

    public bool TryStopAntiSpam(ulong guildId)
    {
        if (_antiSpamGuilds.TryRemove(guildId, out var removed))
        {
            foreach (var (_, val) in removed.UserStats)
            {
                val.Dispose();
            }
            
            using var uow = _db.GetDbContext();
            var gc = uow.GuildConfigsForId(guildId, set => set.Include(x => x.AntiSpamSetting)
                .ThenInclude(x => x.IgnoredChannels));

            gc.AntiSpamSetting = null;
            uow.SaveChanges();
            return true;
        }
        return false;
    }

    public async Task<AntiSpamStats> StartAntiSpamAsync(ulong guildId, int messageCount, PunishmentAction action,
        int punishDurationMinutes, ulong? roleId)
    {
        var g = _client.GetGuild(guildId);
        await _mute.GetMuteRole(g).ConfigureAwait(false);

        if (!IsDurationAllowed(action))
            punishDurationMinutes = 0;

        var stats = new AntiSpamStats
        {
            AntiSpamSettings = new()
            {
                Action = action,
                MessageThreshold = messageCount,
                MuteTime = punishDurationMinutes,
                RoleId = roleId,
            }
        };

        stats = _antiSpamGuilds.AddOrUpdate(guildId, stats, (key, old) =>
        {
            stats.AntiSpamSettings.IgnoredChannels = old.AntiSpamSettings.IgnoredChannels;
            return stats;
        });

        await using var uow = _db.GetDbContext();
        var gc = uow.GuildConfigsForId(guildId, set => set.Include(x => x.AntiSpamSetting));

        if (gc.AntiSpamSetting != null)
        {
            gc.AntiSpamSetting.Action = stats.AntiSpamSettings.Action;
            gc.AntiSpamSetting.MessageThreshold = stats.AntiSpamSettings.MessageThreshold;
            gc.AntiSpamSetting.MuteTime = stats.AntiSpamSettings.MuteTime;
            gc.AntiSpamSetting.RoleId = stats.AntiSpamSettings.RoleId;
        }
        else
        {
            gc.AntiSpamSetting = stats.AntiSpamSettings;
        }
        await uow.SaveChangesAsync();
        return stats;
    }

    public async Task<bool?> AntiSpamIgnoreAsync(ulong guildId, ulong channelId)
    {
        var obj = new AntiSpamIgnore()
        {
            ChannelId = channelId
        };
        bool added;
        await using var uow = _db.GetDbContext();
        var gc = uow.GuildConfigsForId(guildId, set => set.Include(x => x.AntiSpamSetting).ThenInclude(x => x.IgnoredChannels));
        var spam = gc.AntiSpamSetting;
        if (spam is null)
        {
            return null;
        }

        if (spam.IgnoredChannels.Add(obj)) // if adding to db is successful
        {
            if (_antiSpamGuilds.TryGetValue(guildId, out var temp))
                temp.AntiSpamSettings.IgnoredChannels.Add(obj); // add to local cache
            added = true;
        }
        else
        {
            var toRemove = spam.IgnoredChannels.First(x => x.ChannelId == channelId);
            uow.Set<AntiSpamIgnore>().Remove(toRemove); // remove from db
            if (_antiSpamGuilds.TryGetValue(guildId, out var temp))
            {
                temp.AntiSpamSettings.IgnoredChannels.Remove(toRemove); // remove from local cache
            }
            added = false;
        }

        await uow.SaveChangesAsync();
        return added;
    }

    public (AntiSpamStats, AntiRaidStats, AntiAltStats) GetAntiStats(ulong guildId)
    {
        _antiRaidGuilds.TryGetValue(guildId, out var antiRaidStats);
        _antiSpamGuilds.TryGetValue(guildId, out var antiSpamStats);
        _antiAltGuilds.TryGetValue(guildId, out var antiAltStats);

        return (antiSpamStats, antiRaidStats, antiAltStats);
    }

    public bool IsDurationAllowed(PunishmentAction action)
    {
        switch (action)
        {
            case PunishmentAction.Ban:
            case PunishmentAction.Mute:
            case PunishmentAction.ChatMute:
            case PunishmentAction.VoiceMute:
            case PunishmentAction.AddRole:
                return true;
            default:
                return false;
        }
    }

    public async Task StartAntiAltAsync(ulong guildId, int minAgeMinutes, PunishmentAction action,
        int actionDurationMinutes = 0, ulong? roleId = null)
    {
        await using var uow = _db.GetDbContext();
        var gc = uow.GuildConfigsForId(guildId, set => set.Include(x => x.AntiAltSetting));
        gc.AntiAltSetting = new()
        {
            Action = action,
            ActionDurationMinutes = actionDurationMinutes,
            MinAge = TimeSpan.FromMinutes(minAgeMinutes),
            RoleId = roleId,
        };

        await uow.SaveChangesAsync();
        _antiAltGuilds[guildId] = new(gc.AntiAltSetting);
    }

    public async Task<bool> TryStopAntiAlt(ulong guildId)
    {
        if (!_antiAltGuilds.TryRemove(guildId, out _))
            return false;

        await using var uow = _db.GetDbContext();
        var gc = uow.GuildConfigsForId(guildId, set => set.Include(x => x.AntiAltSetting));
        gc.AntiAltSetting = null;
        await uow.SaveChangesAsync();
        return true;
    }
}