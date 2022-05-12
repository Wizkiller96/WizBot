#nullable disable
using Microsoft.EntityFrameworkCore;
using NadekoBot.Common.ModuleBehaviors;
using NadekoBot.Db;
using NadekoBot.Db.Models;
using NadekoBot.Services.Database.Models;
using Newtonsoft.Json;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using StackExchange.Redis;
using Color = SixLabors.ImageSharp.Color;
using Image = SixLabors.ImageSharp.Image;

namespace NadekoBot.Modules.Xp.Services;

public class XpService : INService, IReadyExecutor, IExecNoCommand
{
    public const int XP_REQUIRED_LVL_1 = 36;

    private readonly DbService _db;
    private readonly CommandHandler _cmd;
    private readonly IImageCache _images;
    private readonly IBotStrings _strings;
    private readonly IDataCache _cache;
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

    private readonly ConcurrentQueue<UserCacheItem> _addMessageXp = new();
    private XpTemplate template;
    private readonly DiscordSocketClient _client;

    private readonly TypedKey<bool> _xpTemplateReloadKey;

    public XpService(
        DiscordSocketClient client,
        CommandHandler cmd,
        Bot bot,
        DbService db,
        IBotStrings strings,
        IDataCache cache,
        FontProvider fonts,
        IBotCredentials creds,
        ICurrencyService cs,
        IHttpClientFactory http,
        XpConfigService xpConfig,
        IPubSub pubSub,
        IEmbedBuilderService eb)
    {
        _db = db;
        _cmd = cmd;
        _images = cache.LocalImages;
        _strings = strings;
        _cache = cache;
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
        using var timer = new PeriodicTimer(5.Seconds());
        while (await timer.WaitForNextTickAsync())
        {
            await UpdateLoop();
        }
    }

    private async Task UpdateLoop()
    {
        try
        {
            var toNotify =
                new List<(IGuild Guild, IMessageChannel MessageChannel, IUser User, long Level,
                    XpNotificationLocation NotifyType, NotifOf NotifOf)>();
            var roleRewards = new Dictionary<ulong, List<XpRoleReward>>();
            var curRewards = new Dictionary<ulong, List<XpCurrencyReward>>();

            var toAddTo = new List<UserCacheItem>();
            while (_addMessageXp.TryDequeue(out var usr))
                toAddTo.Add(usr);

            var group = toAddTo.GroupBy(x => (GuildId: x.Guild.Id, x.User));
            if (toAddTo.Count == 0)
                return;

            await using (var uow = _db.GetDbContext())
            {
                foreach (var item in group)
                {
                    var xp = item.Sum(x => x.XpAmount);

                    var usr = uow.GetOrCreateUserXpStats(item.Key.GuildId, item.Key.User.Id);
                    var du = uow.GetOrCreateUser(item.Key.User);

                    var globalXp = du.TotalXp;
                    var oldGlobalLevelData = new LevelStats(globalXp);
                    var newGlobalLevelData = new LevelStats(globalXp + xp);

                    var oldGuildLevelData = new LevelStats(usr.Xp + usr.AwardedXp);
                    usr.Xp += xp;
                    du.TotalXp += xp;
                    if (du.Club is not null)
                        du.Club.Xp += xp;
                    var newGuildLevelData = new LevelStats(usr.Xp + usr.AwardedXp);

                    if (oldGlobalLevelData.Level < newGlobalLevelData.Level)
                    {
                        du.LastLevelUp = DateTime.UtcNow;
                        var first = item.First();
                        if (du.NotifyOnLevelUp != XpNotificationLocation.None)
                        {
                            toNotify.Add((first.Guild, first.Channel, first.User, newGlobalLevelData.Level,
                                du.NotifyOnLevelUp, NotifOf.Global));
                        }
                    }

                    if (oldGuildLevelData.Level < newGuildLevelData.Level)
                    {
                        usr.LastLevelUp = DateTime.UtcNow;
                        //send level up notification
                        var first = item.First();
                        if (usr.NotifyOnLevelUp != XpNotificationLocation.None)
                        {
                            toNotify.Add((first.Guild, first.Channel, first.User, newGuildLevelData.Level,
                                usr.NotifyOnLevelUp, NotifOf.Server));
                        }

                        //give role
                        if (!roleRewards.TryGetValue(usr.GuildId, out var rrews))
                        {
                            rrews = uow.XpSettingsFor(usr.GuildId).RoleRewards.ToList();
                            roleRewards.Add(usr.GuildId, rrews);
                        }

                        if (!curRewards.TryGetValue(usr.GuildId, out var crews))
                        {
                            crews = uow.XpSettingsFor(usr.GuildId).CurrencyRewards.ToList();
                            curRewards.Add(usr.GuildId, crews);
                        }

                        //loop through levels since last level up, so if a high amount of xp is gained, reward are still applied.
                        for (var i = oldGuildLevelData.Level + 1; i <= newGuildLevelData.Level; i++)
                        {
                            var rrew = rrews.FirstOrDefault(x => x.Level == i);
                            if (rrew is not null)
                            {
                                var role = first.User.Guild.GetRole(rrew.RoleId);
                                if (role is not null)
                                {
                                    if (rrew.Remove)
                                        _ = first.User.RemoveRoleAsync(role);
                                    else
                                        _ = first.User.AddRoleAsync(role);
                                }
                            }

                            //get currency reward for this level
                            var crew = crews.FirstOrDefault(x => x.Level == i);
                            if (crew is not null)
                                //give the user the reward if it exists
                                await _cs.AddAsync(item.Key.User.Id, crew.Amount, new("xp", "level-up"));
                        }
                    }
                }

                uow.SaveChanges();
            }

            await toNotify.Select(async x =>
                          {
                              if (x.NotifOf == NotifOf.Server)
                              {
                                  if (x.NotifyType == XpNotificationLocation.Dm)
                                  {
                                      await x.User.SendConfirmAsync(_eb,
                                          _strings.GetText(strs.level_up_dm(x.User.Mention,
                                                  Format.Bold(x.Level.ToString()),
                                                  Format.Bold(x.Guild.ToString() ?? "-")),
                                              x.Guild.Id));
                                  }
                                  else if (x.MessageChannel is not null) // channel
                                  {
                                      await x.MessageChannel.SendConfirmAsync(_eb,
                                          _strings.GetText(strs.level_up_channel(x.User.Mention,
                                                  Format.Bold(x.Level.ToString())),
                                              x.Guild.Id));
                                  }
                              }
                              else
                              {
                                  IMessageChannel chan;
                                  if (x.NotifyType == XpNotificationLocation.Dm)
                                      chan = await x.User.CreateDMChannelAsync();
                                  else // channel
                                      chan = x.MessageChannel;

                                  await chan.SendConfirmAsync(_eb,
                                      _strings.GetText(strs.level_up_global(x.User.Mention,
                                              Format.Bold(x.Level.ToString())),
                                          x.Guild.Id));
                              }
                          })
                          .WhenAll();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error In the XP update loop");
        }
    }


    private void InternalReloadXpTemplate()
    {
        try
        {
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new RequireObjectPropertiesContractResolver()
            };
            template = JsonConvert.DeserializeObject<XpTemplate>(File.ReadAllText("./data/xp_template.json"),
                settings);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Xp template is invalid. Loaded default values");
            template = new();
            File.WriteAllText("./data/xp_template_backup.json",
                JsonConvert.SerializeObject(template, Formatting.Indented));
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
        Task.Run(() =>
        {
            foreach (var channel in guild.VoiceChannels)
                ScanChannelForVoiceXp(channel);
        });

        return Task.CompletedTask;
    }

    private Task Client_OnUserVoiceStateUpdated(SocketUser socketUser, SocketVoiceState before, SocketVoiceState after)
    {
        if (socketUser is not SocketGuildUser user || user.IsBot)
            return Task.CompletedTask;

        _ = Task.Run(() =>
        {
            if (before.VoiceChannel is not null)
                ScanChannelForVoiceXp(before.VoiceChannel);

            if (after.VoiceChannel is not null && after.VoiceChannel != before.VoiceChannel)
                ScanChannelForVoiceXp(after.VoiceChannel);
            else if (after.VoiceChannel is null)
                // In this case, the user left the channel and the previous for loops didn't catch
                // it because it wasn't in any new channel. So we need to get rid of it.
                UserLeftVoiceChannel(user, before.VoiceChannel);
        });

        return Task.CompletedTask;
    }

    private void ScanChannelForVoiceXp(SocketVoiceChannel channel)
    {
        if (ShouldTrackVoiceChannel(channel))
        {
            foreach (var user in channel.Users)
                ScanUserForVoiceXp(user, channel);
        }
        else
        {
            foreach (var user in channel.Users)
                UserLeftVoiceChannel(user, channel);
        }
    }

    /// <summary>
    ///     Assumes that the channel itself is valid and adding xp.
    /// </summary>
    /// <param name="user"></param>
    /// <param name="channel"></param>
    private void ScanUserForVoiceXp(SocketGuildUser user, SocketVoiceChannel channel)
    {
        if (UserParticipatingInVoiceChannel(user) && ShouldTrackXp(user, channel.Id))
            UserJoinedVoiceChannel(user);
        else
            UserLeftVoiceChannel(user, channel);
    }

    private bool ShouldTrackVoiceChannel(SocketVoiceChannel channel)
        => channel.Users.Where(UserParticipatingInVoiceChannel).Take(2).Count() >= 2;

    private bool UserParticipatingInVoiceChannel(SocketGuildUser user)
        => !user.IsDeafened && !user.IsMuted && !user.IsSelfDeafened && !user.IsSelfMuted;

    private void UserJoinedVoiceChannel(SocketGuildUser user)
    {
        var key = $"{_creds.RedisKey()}_user_xp_vc_join_{user.Id}";
        var value = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        _cache.Redis.GetDatabase()
              .StringSet(key,
                  value,
                  TimeSpan.FromMinutes(_xpConfig.Data.VoiceMaxMinutes),
                  when: When.NotExists);
    }

    private void UserLeftVoiceChannel(SocketGuildUser user, SocketVoiceChannel channel)
    {
        var key = $"{_creds.RedisKey()}_user_xp_vc_join_{user.Id}";
        var value = _cache.Redis.GetDatabase().StringGet(key);
        _cache.Redis.GetDatabase().KeyDelete(key);

        // Allow for if this function gets called multiple times when a user leaves a channel.
        if (value.IsNull)
            return;

        if (!value.TryParse(out long startUnixTime))
            return;

        var dateStart = DateTimeOffset.FromUnixTimeSeconds(startUnixTime);
        var dateEnd = DateTimeOffset.UtcNow;
        var minutes = (dateEnd - dateStart).TotalMinutes;
        var xp = _xpConfig.Data.VoiceXpPerMinute * minutes;
        var actualXp = (int)Math.Floor(xp);

        if (actualXp > 0)
        {
            _addMessageXp.Enqueue(new()
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

        _ = Task.Run(() =>
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

            if (!SetUserRewarded(user.Id))
                return;

            _addMessageXp.Enqueue(new()
            {
                Guild = user.Guild,
                Channel = arg.Channel,
                User = user,
                XpAmount = xp
            });
        });
        return Task.CompletedTask;
    }

    public void AddXpDirectly(IGuildUser user, IMessageChannel channel, int amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount));

        _addMessageXp.Enqueue(new()
        {
            Guild = user.Guild,
            Channel = channel,
            User = user,
            XpAmount = amount
        });
    }

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

    private bool SetUserRewarded(ulong userId)
    {
        var r = _cache.Redis.GetDatabase();
        var key = $"{_creds.RedisKey()}_user_xp_gain_{userId}";

        return r.StringSet(key,
            true,
            TimeSpan.FromMinutes(_xpConfig.Data.MessageXpCooldown),
            when: When.NotExists);
    }

    public async Task<FullUserStats> GetUserStatsAsync(IGuildUser user)
    {
        DiscordUser du;
        UserXpStats stats;
        long totalXp;
        int globalRank;
        int guildRank;
        await using (var uow = _db.GetDbContext())
        {
            du = uow.GetOrCreateUser(user, set => set.Include(x => x.Club));
            totalXp = du.TotalXp;
            globalRank = uow.DiscordUser.GetUserGlobalRank(user.Id);
            guildRank = uow.UserXpStats.GetUserGuildRanking(user.Id, user.GuildId);
            stats = uow.GetOrCreateUserXpStats(user.GuildId, user.Id);
            await uow.SaveChangesAsync();
        }

        return new(du, stats, new(totalXp), new(stats.Xp + stats.AwardedXp), globalRank, guildRank);
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

            using var img = Image.Load<Rgba32>(_images.XpBackground, out var imageFormat);
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

            if (template.User.GlobalLevel.Show)
            {
                img.Mutate(x =>
                {
                    x.DrawText(stats.Global.Level.ToString(),
                        _fonts.NotoSans.CreateFont(template.User.GlobalLevel.FontSize, FontStyle.Bold),
                        template.User.GlobalLevel.Color,
                        new(template.User.GlobalLevel.Pos.X, template.User.GlobalLevel.Pos.Y)); //level
                });
            }

            if (template.User.GuildLevel.Show)
            {
                img.Mutate(x =>
                {
                    x.DrawText(stats.Guild.Level.ToString(),
                        _fonts.NotoSans.CreateFont(template.User.GuildLevel.FontSize, FontStyle.Bold),
                        template.User.GuildLevel.Color,
                        new(template.User.GuildLevel.Pos.X, template.User.GuildLevel.Pos.Y));
                });
            }


            var pen = new Pen(Color.Black, 1);

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
                img.Mutate(x => x.DrawText($"{global.LevelXp}/{global.RequiredXp}",
                    _fonts.NotoSans.CreateFont(template.User.Xp.Global.FontSize, FontStyle.Bold),
                    Brushes.Solid(template.User.Xp.Global.Color),
                    pen,
                    new(template.User.Xp.Global.Pos.X, template.User.Xp.Global.Pos.Y)));
            }

            if (template.User.Xp.Guild.Show)
            {
                img.Mutate(x => x.DrawText($"{guild.LevelXp}/{guild.RequiredXp}",
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
                img.Mutate(x => x.DrawText(stats.GlobalRanking.ToString(),
                    _fonts.UniSans.CreateFont(template.User.GlobalRank.FontSize, FontStyle.Bold),
                    template.User.GlobalRank.Color,
                    new(template.User.GlobalRank.Pos.X, template.User.GlobalRank.Pos.Y)));
            }

            if (template.User.GuildRank.Show)
            {
                img.Mutate(x => x.DrawText(stats.GuildRanking.ToString(),
                    _fonts.UniSans.CreateFont(template.User.GuildRank.FontSize, FontStyle.Bold),
                    template.User.GuildRank.Color,
                    new(template.User.GuildRank.Pos.X, template.User.GuildRank.Pos.Y)));
            }

            //time on this level

            string GetTimeSpent(DateTime time, string format)
            {
                var offset = DateTime.UtcNow - time;
                return string.Format(format, offset.Days, offset.Hours, offset.Minutes);
            }

            if (template.User.TimeOnLevel.Global.Show)
            {
                img.Mutate(x => x.DrawText(GetTimeSpent(stats.User.LastLevelUp, template.User.TimeOnLevel.Format),
                    _fonts.NotoSans.CreateFont(template.User.TimeOnLevel.Global.FontSize, FontStyle.Bold),
                    template.User.TimeOnLevel.Global.Color,
                    new(template.User.TimeOnLevel.Global.Pos.X, template.User.TimeOnLevel.Global.Pos.Y)));
            }

            if (template.User.TimeOnLevel.Guild.Show)
            {
                img.Mutate(x
                    => x.DrawText(GetTimeSpent(stats.FullGuildStats.LastLevelUp, template.User.TimeOnLevel.Format),
                        _fonts.NotoSans.CreateFont(template.User.TimeOnLevel.Guild.FontSize, FontStyle.Bold),
                        template.User.TimeOnLevel.Guild.Color,
                        new(template.User.TimeOnLevel.Guild.Pos.X, template.User.TimeOnLevel.Guild.Pos.Y)));
            }
            //avatar

            if (stats.User.AvatarId is not null && template.User.Icon.Show)
            {
                try
                {
                    var avatarUrl = stats.User.RealAvatarUrl();

                    var (succ, data) = await _cache.TryGetImageDataAsync(avatarUrl);
                    if (!succ)
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

                        await _cache.SetImageDataAsync(avatarUrl, data);
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

            img.Mutate(x => x.Resize(template.OutputSize.X, template.OutputSize.Y));
            return ((Stream)img.ToStream(imageFormat), imageFormat);
        });

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
                var (succ, data) = await _cache.TryGetImageDataAsync(imgUrl);
                if (!succ)
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

                    await _cache.SetImageDataAsync(imgUrl, data);
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