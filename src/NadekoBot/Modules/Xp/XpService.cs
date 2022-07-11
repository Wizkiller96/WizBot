#nullable disable
using LinqToDB;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Common.ModuleBehaviors;
using NadekoBot.Db;
using NadekoBot.Db.Models;
using NadekoBot.Modules.Utility.Patronage;
using NadekoBot.Services.Database.Models;
using Newtonsoft.Json;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Threading.Channels;
using Color = SixLabors.ImageSharp.Color;
using Image = SixLabors.ImageSharp.Image;

namespace NadekoBot.Modules.Xp.Services;

public class XpService : INService, IReadyExecutor, IExecNoCommand
{
    public const int XP_REQUIRED_LVL_1 = 36;

    private readonly DbService _db;
    private readonly IImageCache _images;
    private readonly IBotStrings _strings;
    private readonly FontProvider _fonts;
    private readonly IBotCredentials _creds;
    private readonly ICurrencyService _cs;
    private readonly IHttpClientFactory _httpFactory;
    private readonly XpConfigService _xpConfig;
    private readonly IPubSub _pubSub;
    private readonly IEmbedBuilderService _eb;

    private readonly ConcurrentDictionary<ulong, ConcurrentHashSet<ulong>> _excludedRoles;
    private readonly ConcurrentDictionary<ulong, ConcurrentHashSet<ulong>> _excludedChannels;
    private readonly ConcurrentHashSet<ulong> _excludedServers;

    private XpTemplate template;
    private readonly DiscordSocketClient _client;

    private readonly TypedKey<bool> _xpTemplateReloadKey;
    private readonly IPatronageService _ps;
    private readonly IBotCache _c;
    
    
    private readonly QueueRunner _levelUpQueue = new QueueRunner(0, 50);
    private readonly Channel<UserXpGainData> _xpGainQueue = Channel.CreateUnbounded<UserXpGainData>();

    public XpService(
        DiscordSocketClient client,
        Bot bot,
        DbService db,
        IBotStrings strings,
        IImageCache images,
        IBotCache c,
        FontProvider fonts,
        IBotCredentials creds,
        ICurrencyService cs,
        IHttpClientFactory http,
        XpConfigService xpConfig,
        IPubSub pubSub,
        IEmbedBuilderService eb,
        IPatronageService ps)
    {
        _db = db;
        _images = images;
        _strings = strings;
        _fonts = fonts;
        _creds = creds;
        _cs = cs;
        _httpFactory = http;
        _xpConfig = xpConfig;
        _pubSub = pubSub;
        _eb = eb;
        _excludedServers = new();
        _excludedChannels = new();
        _client = client;
        _xpTemplateReloadKey = new("xp.template.reload");
        _ps = ps;
        _c = c;

        InternalReloadXpTemplate();

        if (client.ShardId == 0)
        {
            _pubSub.Sub(_xpTemplateReloadKey,
                _ =>
                {
                    InternalReloadXpTemplate();
                    return default;
                });
        }

        //load settings
        var allGuildConfigs = bot.AllGuildConfigs.Where(x => x.XpSettings is not null).ToList();

        _excludedChannels = allGuildConfigs.ToDictionary(x => x.GuildId,
                                               x => new ConcurrentHashSet<ulong>(x.XpSettings.ExclusionList
                                                   .Where(ex => ex.ItemType == ExcludedItemType.Channel)
                                                   .Select(ex => ex.ItemId)
                                                   .Distinct()))
                                           .ToConcurrent();

        _excludedRoles = allGuildConfigs.ToDictionary(x => x.GuildId,
                                            x => new ConcurrentHashSet<ulong>(x.XpSettings.ExclusionList
                                                                               .Where(ex => ex.ItemType
                                                                                   == ExcludedItemType.Role)
                                                                               .Select(ex => ex.ItemId)
                                                                               .Distinct()))
                                        .ToConcurrent();

        _excludedServers = new(allGuildConfigs.Where(x => x.XpSettings.ServerExcluded).Select(x => x.GuildId));

#if !GLOBAL_NADEKO
        _client.UserVoiceStateUpdated += Client_OnUserVoiceStateUpdated;

        // Scan guilds on startup.
        _client.GuildAvailable += Client_OnGuildAvailable;
        foreach (var guild in _client.Guilds)
            Client_OnGuildAvailable(guild);
#endif
    }

    public async Task OnReadyAsync()
    {
        _ = Task.Run(() => _levelUpQueue.RunAsync());
        
        using var timer = new PeriodicTimer(5.Seconds());
        while (await timer.WaitForNextTickAsync())
        {
            await UpdateXp();
        }
    }

    public sealed class MiniGuildXpStats
    {
        public long Xp { get; set; }
        public XpNotificationLocation NotifyOnLevelUp { get; set; }
        public ulong GuildId { get; set; }
        public ulong UserId { get; set; }
    }
    private async Task UpdateXp()
    {
        try
        {
            var reader = _xpGainQueue.Reader;
            
            // sum up all gains into a single UserCacheItem
            var globalToAdd = new Dictionary<ulong, UserXpGainData>();
            var guildToAdd = new Dictionary<ulong, Dictionary<ulong, UserXpGainData>>();
            while (reader.TryRead(out var item))
            {
                // add global xp to these users
                if (!globalToAdd.TryGetValue(item.User.Id, out var ci))
                    globalToAdd[item.User.Id] = item.Clone();
                else
                    ci.XpAmount += item.XpAmount;

                
                // ad guild xp in these guilds to these users
                if (!guildToAdd.TryGetValue(item.Guild.Id, out var users))
                    users = guildToAdd[item.Guild.Id] = new();

                if (!users.TryGetValue(item.User.Id, out ci))
                    users[item.User.Id] = item.Clone();
                else
                    ci.XpAmount += item.XpAmount;
            }
            
            await using var ctx = _db.GetDbContext();
            await using var tran = await ctx.Database.BeginTransactionAsync();
            
            // update global user xp in batches
            // group by xp amount and update the same amounts at the same time
            var dus = new List<DiscordUser>(globalToAdd.Count);
            foreach (var group in globalToAdd.GroupBy(x => x.Value.XpAmount, x => x.Key))
            {
                var items = await ctx.DiscordUser
                    .Where(x => group.Contains(x.UserId))
                    .UpdateWithOutputAsync(old => new()
                        {
                            TotalXp = old.TotalXp + group.Key
                        },
                        (_, n) => n);
            
                dus.AddRange(items);
            }
            
            // update guild user xp in batches
            var gxps = new List<UserXpStats>(globalToAdd.Count);
            foreach (var (guildId, toAdd) in guildToAdd)
            {
                foreach (var group in toAdd.GroupBy(x => x.Value.XpAmount, x => x.Key))
                {
                    var items = await ctx
                        .UserXpStats
                        .Where(x => x.GuildId == guildId)
                        .Where(x => group.Contains(x.UserId))
                        .UpdateWithOutputAsync(old => new()
                            {
                                Xp = old.Xp + group.Key
                            },
                            (_, n) => n);

                    gxps.AddRange(items);
                }
            }
            
            await tran.CommitAsync();
            
            foreach (var du in dus)
            {
                var oldLevel = new LevelStats(du.TotalXp - globalToAdd[du.UserId].XpAmount);
                var newLevel = new LevelStats(du.TotalXp);
            
                if (oldLevel.Level != newLevel.Level)
                {
                    var item = globalToAdd[du.UserId];
                    await _levelUpQueue.EnqueueAsync(
                        NotifyUser(item.Guild.Id,
                            item.Channel.Id,
                            du.UserId,
                            false,
                            oldLevel.Level,
                            newLevel.Level,
                            du.NotifyOnLevelUp));
                }
            }

            foreach (var du in gxps)
            {
                if (guildToAdd.TryGetValue(du.GuildId, out var users)
                    && users.TryGetValue(du.UserId, out var xpGainData))
                {
                    var oldLevel = new LevelStats(du.Xp - xpGainData.XpAmount);
                    var newLevel = new LevelStats(du.Xp);

                    if (oldLevel.Level < newLevel.Level)
                    {
                        await _levelUpQueue.EnqueueAsync(
                            NotifyUser(xpGainData.Guild.Id,
                                xpGainData.Channel.Id,
                                du.UserId,
                                true,
                                oldLevel.Level,
                                newLevel.Level,
                                du.NotifyOnLevelUp));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error In the XP update loop");
        }
    }

    private Func<Task> NotifyUser(
        ulong guildId,
        ulong channelId,
        ulong userId,
        bool isServer,
        long oldLevel,
        long newLevel,
        XpNotificationLocation notifyLoc)
        => async () =>
        {
            if (isServer)
            {
                await HandleRewardsInternalAsync(guildId, userId, oldLevel, newLevel);
            }

            await HandleNotifyInternalAsync(guildId, channelId, userId, isServer, newLevel, notifyLoc);
        };

    private async Task HandleRewardsInternalAsync(ulong guildId, ulong userId, long oldLevel, long newLevel)
    {
        await using var ctx = _db.GetDbContext();
            var rrews = ctx.XpSettingsFor(guildId).RoleRewards.ToList();
            var crews = ctx.XpSettingsFor(guildId).CurrencyRewards.ToList();
        
        //loop through levels since last level up, so if a high amount of xp is gained, reward are still applied.
        for (var i = oldLevel + 1; i <= newLevel; i++)
        {
            var rrew = rrews.FirstOrDefault(x => x.Level == i);
            if (rrew is not null)
            {
                var guild = _client.GetGuild(guildId);
                var role = guild?.GetRole(rrew.RoleId);
                var user = guild?.GetUser(userId);

                if (role is not null && user is not null)
                {
                    if (rrew.Remove)
                        _ = user.RemoveRoleAsync(role);
                    else
                        _ = user.AddRoleAsync(role);
                }
            }

            //get currency reward for this level
            var crew = crews.FirstOrDefault(x => x.Level == i);
            if (crew is not null)
            {
                //give the user the reward if it exists
                await _cs.AddAsync(userId, crew.Amount, new("xp", "level-up"));
            }
        }
    }

    private async Task HandleNotifyInternalAsync(ulong guildId, 
        ulong channelId, 
        ulong userId, 
        bool isServer,
        long newLevel,
        XpNotificationLocation notifyLoc)
    {
        if (notifyLoc == XpNotificationLocation.None)
            return;
            
        var guild = _client.GetGuild(guildId);
        var user = guild?.GetUser(userId);
        var ch = guild?.GetTextChannel(channelId);

        if (guild is null || user is null)
            return;

        if (isServer)
        {
            if (notifyLoc == XpNotificationLocation.Dm)
            {
                await user.SendConfirmAsync(_eb,
                    _strings.GetText(strs.level_up_dm(user.Mention,
                            Format.Bold(newLevel.ToString()),
                            Format.Bold(guild.ToString() ?? "-")),
                        guild.Id));
            }
            else // channel
            {
                await ch.SendConfirmAsync(_eb,
                    _strings.GetText(strs.level_up_channel(user.Mention,
                            Format.Bold(newLevel.ToString())),
                        guild.Id));
            }
        }
        else // global level
        {
            var chan = notifyLoc switch
            {
                XpNotificationLocation.Dm => (IMessageChannel)await user.CreateDMChannelAsync(),
                XpNotificationLocation.Channel => ch,
                _ => null
            };

            if (chan is null)
                return;

            await chan.SendConfirmAsync(_eb,
                _strings.GetText(strs.level_up_global(user.Mention,
                        Format.Bold(newLevel.ToString())),
                    guild.Id));
        }
    }

    private const string XP_TEMPLATE_PATH = "./data/xp_template.json";

    private void InternalReloadXpTemplate()
    {
        try
        {
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new RequireObjectPropertiesContractResolver()
            };

            if (!File.Exists(XP_TEMPLATE_PATH))
            {
                var newTemp = new XpTemplate();
                newTemp.Version = 1;
                File.WriteAllText(XP_TEMPLATE_PATH, JsonConvert.SerializeObject(newTemp, Formatting.Indented));
            }

            template = JsonConvert.DeserializeObject<XpTemplate>(
                File.ReadAllText(XP_TEMPLATE_PATH),
                settings);

            if (template!.Version < 1)
            {
                Log.Warning("Loaded default xp_template.json values as the old one was version 0. "
                            + "Old one was renamed to xp_template.json.old");
                File.WriteAllText("./data/xp_template.json.old",
                    JsonConvert.SerializeObject(template, Formatting.Indented));
                template = new();
                template.Version = 1;
                File.WriteAllText(XP_TEMPLATE_PATH, JsonConvert.SerializeObject(template, Formatting.Indented));
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "xp_template.json is invalid. Loaded default values");
            template = new();
            template.Version = 1;
        }
    }

    public void ReloadXpTemplate()
        => _pubSub.Pub(_xpTemplateReloadKey, true);

    public void SetCurrencyReward(ulong guildId, int level, int amount)
    {
        using var uow = _db.GetDbContext();
        var settings = uow.XpSettingsFor(guildId);

        if (amount <= 0)
        {
            var toRemove = settings.CurrencyRewards.FirstOrDefault(x => x.Level == level);
            if (toRemove is not null)
            {
                uow.Remove(toRemove);
                settings.CurrencyRewards.Remove(toRemove);
            }
        }
        else
        {
            var rew = settings.CurrencyRewards.FirstOrDefault(x => x.Level == level);

            if (rew is not null)
                rew.Amount = amount;
            else
            {
                settings.CurrencyRewards.Add(new()
                {
                    Level = level,
                    Amount = amount
                });
            }
        }

        uow.SaveChanges();
    }

    public IEnumerable<XpCurrencyReward> GetCurrencyRewards(ulong id)
    {
        using var uow = _db.GetDbContext();
        return uow.XpSettingsFor(id).CurrencyRewards.ToArray();
    }

    public IEnumerable<XpRoleReward> GetRoleRewards(ulong id)
    {
        using var uow = _db.GetDbContext();
        return uow.XpSettingsFor(id).RoleRewards.ToArray();
    }

    public void ResetRoleReward(ulong guildId, int level)
    {
        using var uow = _db.GetDbContext();
        var settings = uow.XpSettingsFor(guildId);

        var toRemove = settings.RoleRewards.FirstOrDefault(x => x.Level == level);
        if (toRemove is not null)
        {
            uow.Remove(toRemove);
            settings.RoleRewards.Remove(toRemove);
        }

        uow.SaveChanges();
    }

    public void SetRoleReward(
        ulong guildId,
        int level,
        ulong roleId,
        bool remove)
    {
        using var uow = _db.GetDbContext();
        var settings = uow.XpSettingsFor(guildId);


        var rew = settings.RoleRewards.FirstOrDefault(x => x.Level == level);

        if (rew is not null)
        {
            rew.RoleId = roleId;
            rew.Remove = remove;
        }
        else
        {
            settings.RoleRewards.Add(new()
            {
                Level = level,
                RoleId = roleId,
                Remove = remove
            });
        }

        uow.SaveChanges();
    }

    public List<UserXpStats> GetUserXps(ulong guildId, int page)
    {
        using var uow = _db.GetDbContext();
        return uow.UserXpStats.GetUsersFor(guildId, page);
    }

    public List<UserXpStats> GetTopUserXps(ulong guildId, int count)
    {
        using var uow = _db.GetDbContext();
        return uow.UserXpStats.GetTopUserXps(guildId, count);
    }

    public DiscordUser[] GetUserXps(int page)
    {
        using var uow = _db.GetDbContext();
        return uow.DiscordUser.GetUsersXpLeaderboardFor(page);
    }

    public async Task ChangeNotificationType(ulong userId, ulong guildId, XpNotificationLocation type)
    {
        await using var uow = _db.GetDbContext();
        var user = uow.GetOrCreateUserXpStats(guildId, userId);
        user.NotifyOnLevelUp = type;
        await uow.SaveChangesAsync();
    }

    public XpNotificationLocation GetNotificationType(ulong userId, ulong guildId)
    {
        using var uow = _db.GetDbContext();
        var user = uow.GetOrCreateUserXpStats(guildId, userId);
        return user.NotifyOnLevelUp;
    }

    public XpNotificationLocation GetNotificationType(IUser user)
    {
        using var uow = _db.GetDbContext();
        return uow.GetOrCreateUser(user).NotifyOnLevelUp;
    }

    public async Task ChangeNotificationType(IUser user, XpNotificationLocation type)
    {
        await using var uow = _db.GetDbContext();
        var du = uow.GetOrCreateUser(user);
        du.NotifyOnLevelUp = type;
        await uow.SaveChangesAsync();
    }

    private Task Client_OnGuildAvailable(SocketGuild guild)
    {
        Task.Run(async () =>
        {
            foreach (var channel in guild.VoiceChannels)
                await ScanChannelForVoiceXp(channel);
        });

        return Task.CompletedTask;
    }

    private Task Client_OnUserVoiceStateUpdated(SocketUser socketUser, SocketVoiceState before, SocketVoiceState after)
    {
        if (socketUser is not SocketGuildUser user || user.IsBot)
            return Task.CompletedTask;

        _ = Task.Run(async () =>
        {
            if (before.VoiceChannel is not null)
                await ScanChannelForVoiceXp(before.VoiceChannel);

            if (after.VoiceChannel is not null && after.VoiceChannel != before.VoiceChannel)
                await ScanChannelForVoiceXp(after.VoiceChannel);
            else if (after.VoiceChannel is null)
            {
                // In this case, the user left the channel and the previous for loops didn't catch
                // it because it wasn't in any new channel. So we need to get rid of it.
                await UserLeftVoiceChannel(user, before.VoiceChannel);
            }
        });

        return Task.CompletedTask;
    }

    private async Task ScanChannelForVoiceXp(SocketVoiceChannel channel)
    {
        if (ShouldTrackVoiceChannel(channel))
        {
            foreach (var user in channel.Users)
                await ScanUserForVoiceXp(user, channel);
        }
        else
        {
            foreach (var user in channel.Users)
                await UserLeftVoiceChannel(user, channel);
        }
    }

    /// <summary>
    ///     Assumes that the channel itself is valid and adding xp.
    /// </summary>
    /// <param name="user"></param>
    /// <param name="channel"></param>
    private async Task ScanUserForVoiceXp(SocketGuildUser user, SocketVoiceChannel channel)
    {
        if (UserParticipatingInVoiceChannel(user) && ShouldTrackXp(user, channel.Id))
            await UserJoinedVoiceChannel(user);
        else
            await UserLeftVoiceChannel(user, channel);
    }

    private bool ShouldTrackVoiceChannel(SocketVoiceChannel channel)
        => channel.Users.Where(UserParticipatingInVoiceChannel).Take(2).Count() >= 2;

    private bool UserParticipatingInVoiceChannel(SocketGuildUser user)
        => !user.IsDeafened && !user.IsMuted && !user.IsSelfDeafened && !user.IsSelfMuted;

    private TypedKey<long> GetVoiceXpKey(ulong userId)
        => new($"xp:vc_join:{userId}");
    
    private async Task UserJoinedVoiceChannel(SocketGuildUser user)
    {
        var value = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        await _c.AddAsync(GetVoiceXpKey(user.Id),
            value,
            TimeSpan.FromMinutes(_xpConfig.Data.VoiceMaxMinutes),
            overwrite: false);
    }

    private async Task UserLeftVoiceChannel(SocketGuildUser user, SocketVoiceChannel channel)
    {
        var key = GetVoiceXpKey(user.Id);
        var result = await _c.GetAsync(key);
        if (!await _c.RemoveAsync(key))
            return;

        // Allow for if this function gets called multiple times when a user leaves a channel.
        if (!result.TryGetValue(out var unixTime))
            return;

        var dateStart = DateTimeOffset.FromUnixTimeSeconds(unixTime);
        var dateEnd = DateTimeOffset.UtcNow;
        var minutes = (dateEnd - dateStart).TotalMinutes;
        var xp = _xpConfig.Data.VoiceXpPerMinute * minutes;
        var actualXp = (int)Math.Floor(xp);

        if (actualXp > 0)
        {
            await _xpGainQueue.Writer.WriteAsync(new()
            {
                Guild = channel.Guild,
                User = user,
                XpAmount = actualXp
            });
        }
    }

    private bool ShouldTrackXp(SocketGuildUser user, ulong channelId)
    {
        if (_excludedChannels.TryGetValue(user.Guild.Id, out var chans) && chans.Contains(channelId))
            return false;

        if (_excludedServers.Contains(user.Guild.Id))
            return false;

        if (_excludedRoles.TryGetValue(user.Guild.Id, out var roles) && user.Roles.Any(x => roles.Contains(x.Id)))
            return false;

        return true;
    }

    public Task ExecOnNoCommandAsync(IGuild guild, IUserMessage arg)
    {
        if (arg.Author is not SocketGuildUser user || user.IsBot)
            return Task.CompletedTask;

        _ = Task.Run(async () =>
        {
            if (!ShouldTrackXp(user, arg.Channel.Id))
                return;

            var xpConf = _xpConfig.Data;
            var xp = 0;
            if (arg.Attachments.Any(a => a.Height >= 128 && a.Width >= 128))
                xp = xpConf.XpFromImage;

            if (arg.Content.Contains(' ') || arg.Content.Length >= 5)
                xp = Math.Max(xp, xpConf.XpPerMessage);

            if (xp <= 0)
                return;

            if (!await SetUserRewardedAsync(user.Id))
                return;

            await _xpGainQueue.Writer.WriteAsync(new()
            {
                Guild = user.Guild,
                Channel = arg.Channel,
                User = user,
                XpAmount = xp
            });
        });
        return Task.CompletedTask;
    }

    // public void AddXpDirectly(IGuildUser user, IMessageChannel channel, int amount)
    // {
    //     if (amount <= 0)
    //         throw new ArgumentOutOfRangeException(nameof(amount));
    //
    //     _xpGainQueue.Writer.WriteAsync(new()
    //     {
    //         Guild = user.Guild,
    //         Channel = channel,
    //         User = user,
    //         XpAmount = amount
    //     });
    // }

    public void AddXp(ulong userId, ulong guildId, int amount)
    {
        using var uow = _db.GetDbContext();
        var usr = uow.GetOrCreateUserXpStats(guildId, userId);

        usr.AwardedXp += amount;

        uow.SaveChanges();
    }

    public bool IsServerExcluded(ulong id)
        => _excludedServers.Contains(id);

    public IEnumerable<ulong> GetExcludedRoles(ulong id)
    {
        if (_excludedRoles.TryGetValue(id, out var val))
            return val.ToArray();

        return Enumerable.Empty<ulong>();
    }

    public IEnumerable<ulong> GetExcludedChannels(ulong id)
    {
        if (_excludedChannels.TryGetValue(id, out var val))
            return val.ToArray();

        return Enumerable.Empty<ulong>();
    }

    private static TypedKey<bool> GetUserRewKey(ulong userId)
        => new($"xp:user_gain:{userId}");
    
    private async Task<bool> SetUserRewardedAsync(ulong userId)
        => await _c.AddAsync(GetUserRewKey(userId),
            true,
            expiry: TimeSpan.FromMinutes(_xpConfig.Data.MessageXpCooldown),
            overwrite: false);

    public async Task<FullUserStats> GetUserStatsAsync(IGuildUser user)
    {
        await using var uow = _db.GetDbContext();
        var du = uow.GetOrCreateUser(user, set => set.Include(x => x.Club));
        var totalXp = du.TotalXp;
        var globalRank = uow.DiscordUser.GetUserGlobalRank(user.Id);
        var guildRank = uow.UserXpStats.GetUserGuildRanking(user.Id, user.GuildId);
        var stats = uow.GetOrCreateUserXpStats(user.GuildId, user.Id);
        await uow.SaveChangesAsync();

        return new(du,
            stats,
            new(totalXp),
            new(stats.Xp + stats.AwardedXp),
            globalRank,
            guildRank);
    }

    public bool ToggleExcludeServer(ulong id)
    {
        using var uow = _db.GetDbContext();
        var xpSetting = uow.XpSettingsFor(id);
        if (_excludedServers.Add(id))
        {
            xpSetting.ServerExcluded = true;
            uow.SaveChanges();
            return true;
        }

        _excludedServers.TryRemove(id);
        xpSetting.ServerExcluded = false;
        uow.SaveChanges();
        return false;
    }

    public bool ToggleExcludeRole(ulong guildId, ulong rId)
    {
        var roles = _excludedRoles.GetOrAdd(guildId, _ => new());
        using var uow = _db.GetDbContext();
        var xpSetting = uow.XpSettingsFor(guildId);
        var excludeObj = new ExcludedItem
        {
            ItemId = rId,
            ItemType = ExcludedItemType.Role
        };

        if (roles.Add(rId))
        {
            if (xpSetting.ExclusionList.Add(excludeObj))
                uow.SaveChanges();

            return true;
        }

        roles.TryRemove(rId);

        var toDelete = xpSetting.ExclusionList.FirstOrDefault(x => x.Equals(excludeObj));
        if (toDelete is not null)
        {
            uow.Remove(toDelete);
            uow.SaveChanges();
        }

        return false;
    }

    public bool ToggleExcludeChannel(ulong guildId, ulong chId)
    {
        var channels = _excludedChannels.GetOrAdd(guildId, _ => new());
        using var uow = _db.GetDbContext();
        var xpSetting = uow.XpSettingsFor(guildId);
        var excludeObj = new ExcludedItem
        {
            ItemId = chId,
            ItemType = ExcludedItemType.Channel
        };

        if (channels.Add(chId))
        {
            if (xpSetting.ExclusionList.Add(excludeObj))
                uow.SaveChanges();

            return true;
        }

        channels.TryRemove(chId);

        if (xpSetting.ExclusionList.Remove(excludeObj))
            uow.SaveChanges();

        return false;
    }

    public async Task<(Stream Image, IImageFormat Format)> GenerateXpImageAsync(IGuildUser user)
    {
        var stats = await GetUserStatsAsync(user);
        return await GenerateXpImageAsync(stats);
    }


    public Task<(Stream Image, IImageFormat Format)> GenerateXpImageAsync(FullUserStats stats)
        => Task.Run(async () =>
        {
            var usernameTextOptions = new TextGraphicsOptions
            {
                TextOptions = new()
                {
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center
                }
            }.WithFallbackFonts(_fonts.FallBackFonts);

            var clubTextOptions = new TextGraphicsOptions
            {
                TextOptions = new()
                {
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Top
                }
            }.WithFallbackFonts(_fonts.FallBackFonts);

            using var img = Image.Load<Rgba32>(await GetXpBackgroundAsync(stats.User.UserId), out var imageFormat);
            if (template.User.Name.Show)
            {
                var fontSize = (int)(template.User.Name.FontSize * 0.9);
                var username = stats.User.ToString();
                var usernameFont = _fonts.NotoSans.CreateFont(fontSize, FontStyle.Bold);

                var size = TextMeasurer.Measure($"@{username}", new(usernameFont));
                var scale = 400f / size.Width;
                if (scale < 1)
                    usernameFont = _fonts.NotoSans.CreateFont(template.User.Name.FontSize * scale, FontStyle.Bold);

                img.Mutate(x =>
                {
                    x.DrawText(usernameTextOptions,
                        "@" + username,
                        usernameFont,
                        template.User.Name.Color,
                        new(template.User.Name.Pos.X, template.User.Name.Pos.Y + 8));
                });
            }

            //club name

            if (template.Club.Name.Show)
            {
                var clubName = stats.User.Club?.ToString() ?? "-";

                var clubFont = _fonts.NotoSans.CreateFont(template.Club.Name.FontSize, FontStyle.Regular);

                img.Mutate(x => x.DrawText(clubTextOptions,
                    clubName,
                    clubFont,
                    template.Club.Name.Color,
                    new(template.Club.Name.Pos.X + 50, template.Club.Name.Pos.Y - 8)));
            }

            Font GetTruncatedFont(
                FontFamily fontFamily,
                int fontSize,
                FontStyle style,
                string text,
                int maxSize)
            {
                var font = fontFamily.CreateFont(fontSize, style);
                var size = TextMeasurer.Measure(text, new(font));
                var scale = maxSize / size.Width;
                if (scale < 1)
                    font = fontFamily.CreateFont(fontSize * scale, style);

                return font;
            }


            if (template.User.GlobalLevel.Show)
            {
                // up to 83 width

                var globalLevelFont = GetTruncatedFont(
                    _fonts.NotoSans,
                    template.User.GlobalLevel.FontSize,
                    FontStyle.Bold,
                    stats.Global.Level.ToString(),
                    75);

                img.Mutate(x =>
                {
                    x.DrawText(stats.Global.Level.ToString(),
                        globalLevelFont,
                        template.User.GlobalLevel.Color,
                        new(template.User.GlobalLevel.Pos.X, template.User.GlobalLevel.Pos.Y)); //level
                });
            }

            if (template.User.GuildLevel.Show)
            {
                var guildLevelFont = GetTruncatedFont(
                    _fonts.NotoSans,
                    template.User.GuildLevel.FontSize,
                    FontStyle.Bold,
                    stats.Guild.Level.ToString(),
                    75);

                img.Mutate(x =>
                {
                    x.DrawText(stats.Guild.Level.ToString(),
                        guildLevelFont,
                        template.User.GuildLevel.Color,
                        new(template.User.GuildLevel.Pos.X, template.User.GuildLevel.Pos.Y));
                });
            }

            var pen = new Pen(Color.Black, 1.25f);

            var global = stats.Global;
            var guild = stats.Guild;

            //xp bar
            if (template.User.Xp.Bar.Show)
            {
                var xpPercent = global.LevelXp / (float)global.RequiredXp;
                DrawXpBar(xpPercent, template.User.Xp.Bar.Global, img);
                xpPercent = guild.LevelXp / (float)guild.RequiredXp;
                DrawXpBar(xpPercent, template.User.Xp.Bar.Guild, img);
            }

            if (template.User.Xp.Global.Show)
            {
                img.Mutate(x => x.DrawText(
                    new()
                    {
                        TextOptions = new()
                        {
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    },
                    $"{global.LevelXp}/{global.RequiredXp}",
                    _fonts.NotoSans.CreateFont(template.User.Xp.Global.FontSize, FontStyle.Bold),
                    Brushes.Solid(template.User.Xp.Global.Color),
                    pen,
                    new(template.User.Xp.Global.Pos.X, template.User.Xp.Global.Pos.Y)));
            }

            if (template.User.Xp.Guild.Show)
            {
                img.Mutate(x => x.DrawText(
                    new()
                    {
                        TextOptions = new()
                        {
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    },
                    $"{guild.LevelXp}/{guild.RequiredXp}",
                    _fonts.NotoSans.CreateFont(template.User.Xp.Guild.FontSize, FontStyle.Bold),
                    Brushes.Solid(template.User.Xp.Guild.Color),
                    pen,
                    new(template.User.Xp.Guild.Pos.X, template.User.Xp.Guild.Pos.Y)));
            }

            if (stats.FullGuildStats.AwardedXp != 0 && template.User.Xp.Awarded.Show)
            {
                var sign = stats.FullGuildStats.AwardedXp > 0 ? "+ " : "";
                var awX = template.User.Xp.Awarded.Pos.X
                          - (Math.Max(0, stats.FullGuildStats.AwardedXp.ToString().Length - 2) * 5);
                var awY = template.User.Xp.Awarded.Pos.Y;
                img.Mutate(x => x.DrawText($"({sign}{stats.FullGuildStats.AwardedXp})",
                    _fonts.NotoSans.CreateFont(template.User.Xp.Awarded.FontSize, FontStyle.Bold),
                    Brushes.Solid(template.User.Xp.Awarded.Color),
                    pen,
                    new(awX, awY)));
            }

            //ranking
            if (template.User.GlobalRank.Show)
            {
                var globalRankStr = stats.GlobalRanking.ToString();

                var globalRankFont = GetTruncatedFont(
                    _fonts.UniSans,
                    template.User.GlobalRank.FontSize,
                    FontStyle.Bold,
                    globalRankStr,
                    68);

                img.Mutate(x => x.DrawText(globalRankStr,
                    globalRankFont,
                    template.User.GlobalRank.Color,
                    new(template.User.GlobalRank.Pos.X, template.User.GlobalRank.Pos.Y)));
            }

            if (template.User.GuildRank.Show)
            {
                var guildRankStr = stats.GuildRanking.ToString();

                var guildRankFont = GetTruncatedFont(
                    _fonts.UniSans,
                    template.User.GuildRank.FontSize,
                    FontStyle.Bold,
                    guildRankStr,
                    43);

                img.Mutate(x => x.DrawText(guildRankStr,
                    guildRankFont,
                    template.User.GuildRank.Color,
                    new(template.User.GuildRank.Pos.X, template.User.GuildRank.Pos.Y)));
            }

            //avatar
            if (stats.User.AvatarId is not null && template.User.Icon.Show)
            {
                try
                {
                    var avatarUrl = stats.User.RealAvatarUrl();

                    var result = await _c.GetImageDataAsync(avatarUrl);
                    if (!result.TryPickT0(out var data, out _))
                    {
                        using (var http = _httpFactory.CreateClient())
                        {
                            var avatarData = await http.GetByteArrayAsync(avatarUrl);
                            using (var tempDraw = Image.Load(avatarData))
                            {
                                tempDraw.Mutate(x => x
                                                     .Resize(template.User.Icon.Size.X, template.User.Icon.Size.Y)
                                                     .ApplyRoundedCorners(Math.Max(template.User.Icon.Size.X,
                                                                              template.User.Icon.Size.Y)
                                                                          / 2.0f));
                                await using (var stream = tempDraw.ToStream())
                                {
                                    data = stream.ToArray();
                                }
                            }
                        }

                        await _c.SetImageDataAsync(avatarUrl, data);
                    }

                    using var toDraw = Image.Load(data);
                    if (toDraw.Size() != new Size(template.User.Icon.Size.X, template.User.Icon.Size.Y))
                        toDraw.Mutate(x => x.Resize(template.User.Icon.Size.X, template.User.Icon.Size.Y));

                    img.Mutate(x => x.DrawImage(toDraw,
                        new Point(template.User.Icon.Pos.X, template.User.Icon.Pos.Y),
                        1));
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Error drawing avatar image");
                }
            }

            //club image
            if (template.Club.Icon.Show)
                await DrawClubImage(img, stats);

// #if GLOBAL_NADEKO
            await DrawFrame(img, stats.User.UserId);
// #endif

            var outputSize = template.OutputSize;
            if (outputSize.X != img.Width || outputSize.Y != img.Height)
                img.Mutate(x => x.Resize(template.OutputSize.X, template.OutputSize.Y));

            var output = ((Stream)await img.ToStreamAsync(imageFormat), imageFormat);

            return output;
        });

    private async Task<byte[]> GetXpBackgroundAsync(ulong userId)
    {
        var img = await _images.GetXpBackgroundImageAsync();
        return img;
    }

    // #if GLOBAL_NADEKO
    private async Task DrawFrame(Image<Rgba32> img, ulong userId)
    {
        var patron = await _ps.GetPatronAsync(userId);
        Image frame = null;
        if (patron.Tier == PatronTier.V)
            frame = Image.Load<Rgba32>(File.OpenRead("data/images/frame_silver.png"));
        else if (patron.Tier >= PatronTier.X || _creds.IsOwner(userId))
            frame = Image.Load<Rgba32>(File.OpenRead("data/images/frame_gold.png"));

        if (frame is not null)
            img.Mutate(x => x.DrawImage(frame, new Point(0, 0), new GraphicsOptions()));
    }
// #endif

    private void DrawXpBar(float percent, XpBar info, Image<Rgba32> img)
    {
        var x1 = info.PointA.X;
        var y1 = info.PointA.Y;

        var x2 = info.PointB.X;
        var y2 = info.PointB.Y;

        var length = info.Length * percent;

        float x3, x4, y3, y4;

        if (info.Direction == XpTemplateDirection.Down)
        {
            x3 = x1;
            x4 = x2;
            y3 = y1 + length;
            y4 = y2 + length;
        }
        else if (info.Direction == XpTemplateDirection.Up)
        {
            x3 = x1;
            x4 = x2;
            y3 = y1 - length;
            y4 = y2 - length;
        }
        else if (info.Direction == XpTemplateDirection.Left)
        {
            x3 = x1 - length;
            x4 = x2 - length;
            y3 = y1;
            y4 = y2;
        }
        else
        {
            x3 = x1 + length;
            x4 = x2 + length;
            y3 = y1;
            y4 = y2;
        }

        img.Mutate(x => x.FillPolygon(info.Color,
            new PointF(x1, y1),
            new PointF(x3, y3),
            new PointF(x4, y4),
            new PointF(x2, y2)));
    }

    private async Task DrawClubImage(Image<Rgba32> img, FullUserStats stats)
    {
        if (!string.IsNullOrWhiteSpace(stats.User.Club?.ImageUrl))
        {
            try
            {
                var imgUrl = new Uri(stats.User.Club.ImageUrl);
                var result = await _c.GetImageDataAsync(imgUrl);
                if (!result.TryPickT0(out var data, out _))
                {
                    using (var http = _httpFactory.CreateClient())
                    using (var temp = await http.GetAsync(imgUrl, HttpCompletionOption.ResponseHeadersRead))
                    {
                        if (!temp.IsImage() || temp.GetContentLength() > 11.Megabytes().Bytes)
                            return;

                        var imgData = await temp.Content.ReadAsByteArrayAsync();
                        using (var tempDraw = Image.Load(imgData))
                        {
                            tempDraw.Mutate(x => x
                                                 .Resize(template.Club.Icon.Size.X, template.Club.Icon.Size.Y)
                                                 .ApplyRoundedCorners(Math.Max(template.Club.Icon.Size.X,
                                                                          template.Club.Icon.Size.Y)
                                                                      / 2.0f));
                            await using (var tds = tempDraw.ToStream())
                            {
                                data = tds.ToArray();
                            }
                        }
                    }

                    await _c.SetImageDataAsync(imgUrl, data);
                }

                using var toDraw = Image.Load(data);
                if (toDraw.Size() != new Size(template.Club.Icon.Size.X, template.Club.Icon.Size.Y))
                    toDraw.Mutate(x => x.Resize(template.Club.Icon.Size.X, template.Club.Icon.Size.Y));

                img.Mutate(x => x.DrawImage(
                    toDraw,
                    new Point(template.Club.Icon.Pos.X, template.Club.Icon.Pos.Y),
                    1));
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error drawing club image");
            }
        }
    }

    public void XpReset(ulong guildId, ulong userId)
    {
        using var uow = _db.GetDbContext();
        uow.UserXpStats.ResetGuildUserXp(userId, guildId);
        uow.SaveChanges();
    }

    public void XpReset(ulong guildId)
    {
        using var uow = _db.GetDbContext();
        uow.UserXpStats.ResetGuildXp(guildId);
        uow.SaveChanges();
    }

    public async Task ResetXpRewards(ulong guildId)
    {
        await using var uow = _db.GetDbContext();
        var guildConfig = uow.GuildConfigsForId(guildId,
            set => set.Include(x => x.XpSettings)
                      .ThenInclude(x => x.CurrencyRewards)
                      .Include(x => x.XpSettings)
                      .ThenInclude(x => x.RoleRewards));

        uow.RemoveRange(guildConfig.XpSettings.RoleRewards);
        uow.RemoveRange(guildConfig.XpSettings.CurrencyRewards);
        await uow.SaveChangesAsync();
    }

    private enum NotifOf
    {
        Server,
        Global
    } // is it a server level-up or global level-up notification
}