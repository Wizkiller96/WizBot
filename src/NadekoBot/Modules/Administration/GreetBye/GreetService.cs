using NadekoBot.Common.ModuleBehaviors;
using NadekoBot.Db;
using NadekoBot.Services.Database.Models;
using System.Threading.Channels;

namespace NadekoBot.Services;

public class GreetService : INService, IReadyExecutor
{
    public bool GroupGreets
        => _bss.Data.GroupGreets;

    private readonly DbService _db;

    private readonly ConcurrentDictionary<ulong, GreetSettings> _guildConfigsCache;
    private readonly DiscordSocketClient _client;

    private readonly GreetGrouper<IGuildUser> _greets = new();
    private readonly GreetGrouper<IUser> _byes = new();
    private readonly BotConfigService _bss;

    public GreetService(
        DiscordSocketClient client,
        Bot bot,
        DbService db,
        BotConfigService bss)
    {
        _db = db;
        _client = client;
        _bss = bss;

        _guildConfigsCache = new(bot.AllGuildConfigs.ToDictionary(g => g.GuildId, GreetSettings.Create));

        _client.UserJoined += OnUserJoined;
        _client.UserLeft += OnUserLeft;

        bot.JoinedGuild += OnBotJoinedGuild;
        _client.LeftGuild += OnClientLeftGuild;

        _client.GuildMemberUpdated += ClientOnGuildMemberUpdated;
    }

    public async Task OnReadyAsync()
    {
        while (true)
        {
            var (conf, user, compl) = await _greetDmQueue.Reader.ReadAsync();
            var res = await GreetDmUserInternal(conf, user);
            compl.TrySetResult(res);
            await Task.Delay(2000);
        }
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
            if (!conf.SendBoostMessage)
                return Task.CompletedTask;

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
                if (conf.BoostMessageDeleteAfter > 0)
                    toDelete.DeleteAfter(conf.BoostMessageDeleteAfter);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error sending boost message");
            }
        };

    private Task OnClientLeftGuild(SocketGuild arg)
    {
        _guildConfigsCache.TryRemove(arg.Id, out _);
        return Task.CompletedTask;
    }

    private Task OnBotJoinedGuild(GuildConfig gc)
    {
        _guildConfigsCache[gc.GuildId] = GreetSettings.Create(gc);
        return Task.CompletedTask;
    }

    private Task OnUserLeft(SocketGuild guild, SocketUser user)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var conf = GetOrAddSettingsForGuild(guild.Id);

                if (!conf.SendChannelByeMessage)
                    return;
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
                    await ByeUsers(conf, channel, new[] { user });
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
        return uow.GuildConfigsForId(id, set => set).DmGreetMessageText;
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
            if (conf.AutoDeleteByeMessagesTimer > 0)
                toDelete.DeleteAfter(conf.AutoDeleteByeMessagesTimer);
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
            if (conf.AutoDeleteGreetMessagesTimer > 0)
                toDelete.DeleteAfter(conf.AutoDeleteGreetMessagesTimer);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error embeding greet message");
        }
    }

    private readonly Channel<(GreetSettings, IGuildUser, TaskCompletionSource<bool>)> _greetDmQueue =
        Channel.CreateBounded<(GreetSettings, IGuildUser, TaskCompletionSource<bool>)>(new BoundedChannelOptions(60)
        {
            // The limit of 60 users should be only hit when there's a raid. In that case 
            // probably the best thing to do is to drop newest (raiding) users
            FullMode = BoundedChannelFullMode.DropNewest
        });

    private async Task<bool> GreetDmUser(GreetSettings conf, IGuildUser user)
    {
        var completionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        await _greetDmQueue.Writer.WriteAsync((conf, user, completionSource));
        return await completionSource.Task;
    }

    private async Task<bool> GreetDmUserInternal(GreetSettings conf, IGuildUser user)
    {
        try
        {
            var rep = new ReplacementBuilder()
                      .WithUser(user)
                      .WithServer(_client, (SocketGuild)user.Guild)
                      .Build();

            var text = SmartText.CreateFrom(conf.DmGreetMessageText);
            text = rep.Replace(text);

            if (text is SmartPlainText pt)
            {
                text = new SmartEmbedText()
                {
                    PlainText = pt.Text
                };
            }

            ((SmartEmbedText)text).Footer = new()
            {
                Text = $"This message was sent from {user.Guild} server.",
                IconUrl = user.Guild.IconUrl
            };

            await user.SendAsync(text);
        }
        catch
        {
            return false;
        }

        return true;
    }

    private Task OnUserJoined(IGuildUser user)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var conf = GetOrAddSettingsForGuild(user.GuildId);

                if (conf.SendChannelGreetMessage)
                {
                    var channel = await user.Guild.GetTextChannelAsync(conf.GreetMessageChannelId);
                    if (channel is not null)
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
                            await GreetUsers(conf, channel, new[] { user });
                    }
                }

                if (conf.SendDmGreetMessage)
                    await GreetDmUser(conf, user);
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

    public async Task<bool> SetGreet(ulong guildId, ulong channelId, bool? value = null)
    {
        await using var uow = _db.GetDbContext();
        var conf = uow.GuildConfigsForId(guildId, set => set);
        var enabled = conf.SendChannelGreetMessage = value ?? !conf.SendChannelGreetMessage;
        conf.GreetMessageChannelId = channelId;

        var toAdd = GreetSettings.Create(conf);
        _guildConfigsCache[guildId] = toAdd;

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
        _guildConfigsCache[guildId] = toAdd;

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
        _guildConfigsCache[guildId] = toAdd;

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
        _guildConfigsCache[guildId] = toAdd;

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
        _guildConfigsCache[guildId] = toAdd;

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
        _guildConfigsCache[guildId] = toAdd;

        await uow.SaveChangesAsync();
    }

    public async Task SetGreetDel(ulong guildId, int timer)
    {
        if (timer is < 0 or > 600)
            return;

        await using var uow = _db.GetDbContext();
        var conf = uow.GuildConfigsForId(guildId, set => set);
        conf.AutoDeleteGreetMessagesTimer = timer;

        var toAdd = GreetSettings.Create(conf);
        _guildConfigsCache[guildId] = toAdd;

        await uow.SaveChangesAsync();
    }

    public bool SetBoostMessage(ulong guildId, ref string message)
    {
        message = message.SanitizeMentions();

        using var uow = _db.GetDbContext();
        var conf = uow.GuildConfigsForId(guildId, set => set);
        conf.BoostMessage = message;

        var toAdd = GreetSettings.Create(conf);
        _guildConfigsCache[guildId] = toAdd;

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
        _guildConfigsCache[guildId] = toAdd;

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
        _guildConfigsCache[guildId] = toAdd;
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

    public Task<bool> GreetDmTest(IGuildUser user)
    {
        var conf = GetOrAddSettingsForGuild(user.GuildId);
        return GreetDmUser(conf, user);
    }

    #endregion
}