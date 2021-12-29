using NadekoBot.Db;
using NadekoBot.Services.Database.Models;

namespace NadekoBot.Services;

public class GreetSettingsService : INService
{
    public bool GroupGreets
        => _bss.Data.GroupGreets;

    private readonly DbService _db;

    private readonly ConcurrentDictionary<ulong, GreetSettings> _guildConfigsCache;
    private readonly DiscordSocketClient _client;

    private readonly GreetGrouper<IGuildUser> _greets = new();
    private readonly GreetGrouper<IUser> _byes = new();
    private readonly BotConfigService _bss;

    public GreetSettingsService(
        DiscordSocketClient client,
        Bot bot,
        DbService db,
        BotConfigService bss)
    {
        _db = db;
        _client = client;
        _bss = bss;

        _guildConfigsCache = new(bot.AllGuildConfigs.ToDictionary(g => g.GuildId, GreetSettings.Create));

        _client.UserJoined += UserJoined;
        _client.UserLeft += UserLeft;

        bot.JoinedGuild += Bot_JoinedGuild;
        _client.LeftGuild += _client_LeftGuild;

        _client.GuildMemberUpdated += ClientOnGuildMemberUpdated;
    }

    private Task ClientOnGuildMemberUpdated(Cacheable<SocketGuildUser, ulong> optOldUser, SocketGuildUser newUser)
    {
        // if user is a new booster
        // or boosted again the same server
        if ((optOldUser.Value is { PremiumSince: null } && newUser is { PremiumSince: not null })
            || (optOldUser.Value?.PremiumSince is { } oldDate
                && newUser.PremiumSince is { } newDate
                && newDate > oldDate))
        {
            var conf = GetOrAddSettingsForGuild(newUser.Guild.Id);
            if (!conf.SendBoostMessage) return Task.CompletedTask;

            _ = Task.Run(TriggerBoostMessage(conf, newUser));
        }

        return Task.CompletedTask;
    }

    private Func<Task> TriggerBoostMessage(GreetSettings conf, SocketGuildUser user)
        => async () =>
        {
            var channel = user.Guild.GetTextChannel(conf.BoostMessageChannelId);
            if (channel is null)
                return;

            if (string.IsNullOrWhiteSpace(conf.BoostMessage))
                return;

            var toSend = SmartText.CreateFrom(conf.BoostMessage);
            var rep = new ReplacementBuilder().WithDefault(user, channel, user.Guild, _client).Build();

            try
            {
                var toDelete = await channel.SendAsync(rep.Replace(toSend));
                if (conf.BoostMessageDeleteAfter > 0) toDelete.DeleteAfter(conf.BoostMessageDeleteAfter);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error sending boost message");
            }
        };

    private Task _client_LeftGuild(SocketGuild arg)
    {
        _guildConfigsCache.TryRemove(arg.Id, out _);
        return Task.CompletedTask;
    }

    private Task Bot_JoinedGuild(GuildConfig gc)
    {
        _guildConfigsCache.AddOrUpdate(gc.GuildId,
            GreetSettings.Create(gc),
            delegate { return GreetSettings.Create(gc); });
        return Task.CompletedTask;
    }

    private Task UserLeft(SocketGuild guild, SocketUser user)
    {
        var _ = Task.Run(async () =>
        {
            try
            {
                var conf = GetOrAddSettingsForGuild(guild.Id);

                if (!conf.SendChannelByeMessage) return;
                var channel = guild.TextChannels.FirstOrDefault(c => c.Id == conf.ByeMessageChannelId);

                if (channel is null) //maybe warn the server owner that the channel is missing
                    return;

                if (GroupGreets)
                {
                    // if group is newly created, greet that user right away,
                    // but any user which joins in the next 5 seconds will
                    // be greeted in a group greet
                    if (_byes.CreateOrAdd(guild.Id, user))
                    {
                        // greet single user
                        await ByeUsers(conf, channel, new[] { user });
                        var groupClear = false;
                        while (!groupClear)
                        {
                            await Task.Delay(5000);
                            groupClear = _byes.ClearGroup(guild.Id, 5, out var toBye);
                            await ByeUsers(conf, channel, toBye);
                        }
                    }
                }
                else
                {
                    await ByeUsers(conf, channel, new[] { user });
                }
            }
            catch
            {
                // ignored
            }
        });
        return Task.CompletedTask;
    }

    public string? GetDmGreetMsg(ulong id)
    {
        using var uow = _db.GetDbContext();
        return uow.GuildConfigsForId(id, set => set)?.DmGreetMessageText;
    }

    public string? GetGreetMsg(ulong gid)
    {
        using var uow = _db.GetDbContext();
        return uow.GuildConfigsForId(gid, set => set).ChannelGreetMessageText;
    }

    public string? GetBoostMessage(ulong gid)
    {
        using var uow = _db.GetDbContext();
        return uow.GuildConfigsForId(gid, set => set).BoostMessage;
    }

    private Task ByeUsers(GreetSettings conf, ITextChannel channel, IUser user)
        => ByeUsers(conf, channel, new[] { user });

    private async Task ByeUsers(GreetSettings conf, ITextChannel channel, IReadOnlyCollection<IUser> users)
    {
        if (!users.Any())
            return;

        var rep = new ReplacementBuilder().WithChannel(channel)
                                          .WithClient(_client)
                                          .WithServer(_client, (SocketGuild)channel.Guild)
                                          .WithManyUsers(users)
                                          .Build();

        var text = SmartText.CreateFrom(conf.ChannelByeMessageText);
        text = rep.Replace(text);
        try
        {
            var toDelete = await channel.SendAsync(text);
            if (conf.AutoDeleteByeMessagesTimer > 0) toDelete.DeleteAfter(conf.AutoDeleteByeMessagesTimer);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error embeding bye message");
        }
    }

    private Task GreetUsers(GreetSettings conf, ITextChannel channel, IGuildUser user)
        => GreetUsers(conf, channel, new[] { user });

    private async Task GreetUsers(GreetSettings conf, ITextChannel channel, IReadOnlyCollection<IGuildUser> users)
    {
        if (users.Count == 0)
            return;

        var rep = new ReplacementBuilder().WithChannel(channel)
                                          .WithClient(_client)
                                          .WithServer(_client, (SocketGuild)channel.Guild)
                                          .WithManyUsers(users)
                                          .Build();

        var text = SmartText.CreateFrom(conf.ChannelGreetMessageText);
        text = rep.Replace(text);
        try
        {
            var toDelete = await channel.SendAsync(text);
            if (conf.AutoDeleteGreetMessagesTimer > 0) toDelete.DeleteAfter(conf.AutoDeleteGreetMessagesTimer);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error embeding greet message");
        }
    }

    private async Task<bool> GreetDmUser(GreetSettings conf, IDMChannel channel, IGuildUser user)
    {
        var rep = new ReplacementBuilder().WithDefault(user, channel, (SocketGuild)user.Guild, _client).Build();

        var text = SmartText.CreateFrom(conf.DmGreetMessageText);
        rep.Replace(text);
        try
        {
            await channel.SendAsync(text);
        }
        catch
        {
            return false;
        }

        return true;
    }

    private Task UserJoined(IGuildUser user)
    {
        var _ = Task.Run(async () =>
        {
            try
            {
                var conf = GetOrAddSettingsForGuild(user.GuildId);

                if (conf.SendChannelGreetMessage)
                {
                    var channel = await user.Guild.GetTextChannelAsync(conf.GreetMessageChannelId);
                    if (channel != null)
                    {
                        if (GroupGreets)
                        {
                            // if group is newly created, greet that user right away,
                            // but any user which joins in the next 5 seconds will
                            // be greeted in a group greet
                            if (_greets.CreateOrAdd(user.GuildId, user))
                            {
                                // greet single user
                                await GreetUsers(conf, channel, new[] { user });
                                var groupClear = false;
                                while (!groupClear)
                                {
                                    await Task.Delay(5000);
                                    groupClear = _greets.ClearGroup(user.GuildId, 5, out var toGreet);
                                    await GreetUsers(conf, channel, toGreet);
                                }
                            }
                        }
                        else
                        {
                            await GreetUsers(conf, channel, new[] { user });
                        }
                    }
                }

                if (conf.SendDmGreetMessage)
                {
                    var channel = await user.CreateDMChannelAsync();

                    if (channel is not null) await GreetDmUser(conf, channel, user);
                }
            }
            catch
            {
                // ignored
            }
        });
        return Task.CompletedTask;
    }

    public string? GetByeMessage(ulong gid)
    {
        using var uow = _db.GetDbContext();
        return uow.GuildConfigsForId(gid, set => set).ChannelByeMessageText;
    }

    public GreetSettings GetOrAddSettingsForGuild(ulong guildId)
    {
        if (_guildConfigsCache.TryGetValue(guildId, out var settings))
            return settings;

        using (var uow = _db.GetDbContext())
        {
            var gc = uow.GuildConfigsForId(guildId, set => set);
            settings = GreetSettings.Create(gc);
        }

        _guildConfigsCache.TryAdd(guildId, settings);
        return settings;
    }

    public async Task<bool> SetSettings(ulong guildId, GreetSettings settings)
    {
        if (settings.AutoDeleteByeMessagesTimer is > 600 or < 0
            || settings.AutoDeleteGreetMessagesTimer is > 600 or < 0)
            return false;

        await using var uow = _db.GetDbContext();
        var conf = uow.GuildConfigsForId(guildId, set => set);
        conf.DmGreetMessageText = settings.DmGreetMessageText?.SanitizeMentions();
        conf.ChannelGreetMessageText = settings.ChannelGreetMessageText?.SanitizeMentions();
        conf.ChannelByeMessageText = settings.ChannelByeMessageText?.SanitizeMentions();

        conf.AutoDeleteGreetMessagesTimer = settings.AutoDeleteGreetMessagesTimer;
        conf.AutoDeleteGreetMessages = settings.AutoDeleteGreetMessagesTimer > 0;

        conf.AutoDeleteByeMessagesTimer = settings.AutoDeleteByeMessagesTimer;
        conf.AutoDeleteByeMessages = settings.AutoDeleteByeMessagesTimer > 0;

        conf.GreetMessageChannelId = settings.GreetMessageChannelId;
        conf.ByeMessageChannelId = settings.ByeMessageChannelId;

        conf.SendChannelGreetMessage = settings.SendChannelGreetMessage;
        conf.SendChannelByeMessage = settings.SendChannelByeMessage;

        await uow.SaveChangesAsync();

        var toAdd = GreetSettings.Create(conf);
        _guildConfigsCache.AddOrUpdate(guildId, toAdd, (_, _) => toAdd);

        return true;
    }

    public async Task<bool> SetGreet(ulong guildId, ulong channelId, bool? value = null)
    {
        bool enabled;
        await using var uow = _db.GetDbContext();
        var conf = uow.GuildConfigsForId(guildId, set => set);
        enabled = conf.SendChannelGreetMessage = value ?? !conf.SendChannelGreetMessage;
        conf.GreetMessageChannelId = channelId;

        var toAdd = GreetSettings.Create(conf);
        _guildConfigsCache.AddOrUpdate(guildId, toAdd, (_, _) => toAdd);

        await uow.SaveChangesAsync();
        return enabled;
    }

    public bool SetGreetMessage(ulong guildId, ref string message)
    {
        message = message.SanitizeMentions();

        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentNullException(nameof(message));

        using var uow = _db.GetDbContext();
        var conf = uow.GuildConfigsForId(guildId, set => set);
        conf.ChannelGreetMessageText = message;
        var greetMsgEnabled = conf.SendChannelGreetMessage;

        var toAdd = GreetSettings.Create(conf);
        _guildConfigsCache.AddOrUpdate(guildId, toAdd, (_, _) => toAdd);

        uow.SaveChanges();
        return greetMsgEnabled;
    }

    public async Task<bool> SetGreetDm(ulong guildId, bool? value = null)
    {
        await using var uow = _db.GetDbContext();
        var conf = uow.GuildConfigsForId(guildId, set => set);
        var enabled = conf.SendDmGreetMessage = value ?? !conf.SendDmGreetMessage;

        var toAdd = GreetSettings.Create(conf);
        _guildConfigsCache.AddOrUpdate(guildId, toAdd, (_, _) => toAdd);

        await uow.SaveChangesAsync();
        return enabled;
    }

    public bool SetGreetDmMessage(ulong guildId, ref string? message)
    {
        message = message?.SanitizeMentions();

        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentNullException(nameof(message));

        using var uow = _db.GetDbContext();
        var conf = uow.GuildConfigsForId(guildId, set => set);
        conf.DmGreetMessageText = message;

        var toAdd = GreetSettings.Create(conf);
        _guildConfigsCache.AddOrUpdate(guildId, toAdd, (_, _) => toAdd);

        uow.SaveChanges();
        return conf.SendDmGreetMessage;
    }

    public async Task<bool> SetBye(ulong guildId, ulong channelId, bool? value = null)
    {
        await using var uow = _db.GetDbContext();
        var conf = uow.GuildConfigsForId(guildId, set => set);
        var enabled = conf.SendChannelByeMessage = value ?? !conf.SendChannelByeMessage;
        conf.ByeMessageChannelId = channelId;

        var toAdd = GreetSettings.Create(conf);
        _guildConfigsCache.AddOrUpdate(guildId, toAdd, (_, _) => toAdd);

        await uow.SaveChangesAsync();
        return enabled;
    }

    public bool SetByeMessage(ulong guildId, ref string? message)
    {
        message = message?.SanitizeMentions();

        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentNullException(nameof(message));

        using var uow = _db.GetDbContext();
        var conf = uow.GuildConfigsForId(guildId, set => set);
        conf.ChannelByeMessageText = message;

        var toAdd = GreetSettings.Create(conf);
        _guildConfigsCache.AddOrUpdate(guildId, toAdd, (_, _) => toAdd);

        uow.SaveChanges();
        return conf.SendChannelByeMessage;
    }

    public async Task SetByeDel(ulong guildId, int timer)
    {
        if (timer is < 0 or > 600)
            return;

        await using var uow = _db.GetDbContext();
        var conf = uow.GuildConfigsForId(guildId, set => set);
        conf.AutoDeleteByeMessagesTimer = timer;

        var toAdd = GreetSettings.Create(conf);
        _guildConfigsCache.AddOrUpdate(guildId, toAdd, (_, _) => toAdd);

        await uow.SaveChangesAsync();
    }

    public async Task SetGreetDel(ulong id, int timer)
    {
        if (timer is < 0 or > 600)
            return;

        await using var uow = _db.GetDbContext();
        var conf = uow.GuildConfigsForId(id, set => set);
        conf.AutoDeleteGreetMessagesTimer = timer;

        var toAdd = GreetSettings.Create(conf);
        _guildConfigsCache.AddOrUpdate(id, toAdd, (_, _) => toAdd);

        await uow.SaveChangesAsync();
    }

    public bool SetBoostMessage(ulong guildId, ref string message)
    {
        message = message.SanitizeMentions();

        using var uow = _db.GetDbContext();
        var conf = uow.GuildConfigsForId(guildId, set => set);
        conf.BoostMessage = message;

        var toAdd = GreetSettings.Create(conf);
        _guildConfigsCache.AddOrUpdate(guildId, toAdd, (_, _) => toAdd);

        uow.SaveChanges();
        return conf.SendBoostMessage;
    }

    public async Task SetBoostDel(ulong guildId, int timer)
    {
        if (timer is < 0 or > 600)
            throw new ArgumentOutOfRangeException(nameof(timer));

        await using var uow = _db.GetDbContext();
        var conf = uow.GuildConfigsForId(guildId, set => set);
        conf.BoostMessageDeleteAfter = timer;

        var toAdd = GreetSettings.Create(conf);
        _guildConfigsCache.AddOrUpdate(guildId, toAdd, (_, _) => toAdd);

        await uow.SaveChangesAsync();
    }

    public async Task<bool> ToggleBoost(ulong guildId, ulong channelId)
    {
        await using var uow = _db.GetDbContext();
        var conf = uow.GuildConfigsForId(guildId, set => set);
        conf.SendBoostMessage = !conf.SendBoostMessage;
        conf.BoostMessageChannelId = channelId;
        await uow.SaveChangesAsync();

        var toAdd = GreetSettings.Create(conf);
        _guildConfigsCache.AddOrUpdate(guildId, toAdd, (_, _) => toAdd);
        return conf.SendBoostMessage;
    }

    #region Get Enabled Status

    public bool GetGreetDmEnabled(ulong guildId)
    {
        using var uow = _db.GetDbContext();
        var conf = uow.GuildConfigsForId(guildId, set => set);
        return conf.SendDmGreetMessage;
    }

    public bool GetGreetEnabled(ulong guildId)
    {
        using var uow = _db.GetDbContext();
        var conf = uow.GuildConfigsForId(guildId, set => set);
        return conf.SendChannelGreetMessage;
    }

    public bool GetByeEnabled(ulong guildId)
    {
        using var uow = _db.GetDbContext();
        var conf = uow.GuildConfigsForId(guildId, set => set);
        return conf.SendChannelByeMessage;
    }

    #endregion

    #region Test Messages

    public Task ByeTest(ITextChannel channel, IGuildUser user)
    {
        var conf = GetOrAddSettingsForGuild(user.GuildId);
        return ByeUsers(conf, channel, user);
    }

    public Task GreetTest(ITextChannel channel, IGuildUser user)
    {
        var conf = GetOrAddSettingsForGuild(user.GuildId);
        return GreetUsers(conf, channel, user);
    }

    public Task<bool> GreetDmTest(IDMChannel channel, IGuildUser user)
    {
        var conf = GetOrAddSettingsForGuild(user.GuildId);
        return GreetDmUser(conf, channel, user);
    }

    #endregion
}