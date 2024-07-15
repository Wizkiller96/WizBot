using WizBot.Common.ModuleBehaviors;
using WizBot.Db;
using WizBot.Db.Models;
using System.Threading.Channels;

namespace WizBot.Services;

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
    private readonly IReplacementService _repSvc;
    private readonly IMessageSenderService _sender;

    public GreetService(
        DiscordSocketClient client,
        IBot bot,
        DbService db,
        BotConfigService bss,
        IMessageSenderService sender,
        IReplacementService repSvc)
    {
        _db = db;
        _client = client;
        _bss = bss;
        _repSvc = repSvc;
        _sender = sender;

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

            await SendBoostMessage(conf, user, channel);
        };

    private async Task<bool> SendBoostMessage(GreetSettings conf, IGuildUser user, ITextChannel channel)
    {
        if (string.IsNullOrWhiteSpace(conf.BoostMessage))
            return false;

        var toSend = SmartText.CreateFrom(conf.BoostMessage);

        try
        {
            var newContent = await _repSvc.ReplaceAsync(toSend,
                new(client: _client, guild: user.Guild, channel: channel, users: user));
            var toDelete = await _sender.Response(channel).Text(newContent).Sanitize(false).SendAsync();
            if (conf.BoostMessageDeleteAfter > 0)
                toDelete.DeleteAfter(conf.BoostMessageDeleteAfter);

            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error sending boost message");
        }

        return false;
    }

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

    public GreetSettings GetGreetSettings(ulong gid)
    {
        if (_guildConfigsCache.TryGetValue(gid, out var gs))
            return gs;

        using var uow = _db.GetDbContext();
        return GreetSettings.Create(uow.GuildConfigsForId(gid, set => set));
    }

    private Task ByeUsers(GreetSettings conf, ITextChannel channel, IUser user)
        => ByeUsers(conf, channel, new[] { user });

    private async Task ByeUsers(GreetSettings conf, ITextChannel channel, IReadOnlyCollection<IUser> users)
    {
        if (!users.Any())
            return;

        var repCtx = new ReplacementContext(client: _client,
            guild: channel.Guild,
            channel: channel,
            users: users.ToArray());

        var text = SmartText.CreateFrom(conf.ChannelByeMessageText);
        text = await _repSvc.ReplaceAsync(text, repCtx);
        try
        {
            var toDelete = await _sender.Response(channel).Text(text).Sanitize(false).SendAsync();
            if (conf.AutoDeleteByeMessagesTimer > 0)
                toDelete.DeleteAfter(conf.AutoDeleteByeMessagesTimer);
        }
        catch (HttpException ex) when (ex.DiscordCode == DiscordErrorCode.InsufficientPermissions
                                       || ex.DiscordCode == DiscordErrorCode.MissingPermissions
                                       || ex.DiscordCode == DiscordErrorCode.UnknownChannel)
        {
            Log.Warning(ex,
                "Missing permissions to send a bye message, the bye message will be disabled on server: {GuildId}",
                channel.GuildId);
            await SetBye(channel.GuildId, channel.Id, false);
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

        var repCtx = new ReplacementContext(client: _client,
            guild: channel.Guild,
            channel: channel,
            users: users.ToArray());
        
        var text = SmartText.CreateFrom(conf.ChannelGreetMessageText);
        text = await _repSvc.ReplaceAsync(text, repCtx);
        try
        {
            var toDelete = await _sender.Response(channel).Text(text).Sanitize(false).SendAsync();
            if (conf.AutoDeleteGreetMessagesTimer > 0)
                toDelete.DeleteAfter(conf.AutoDeleteGreetMessagesTimer);
        }
        catch (HttpException ex) when (ex.DiscordCode == DiscordErrorCode.InsufficientPermissions
                                       || ex.DiscordCode == DiscordErrorCode.MissingPermissions
                                       || ex.DiscordCode == DiscordErrorCode.UnknownChannel)
        {
            Log.Warning(ex,
                "Missing permissions to send a bye message, the greet message will be disabled on server: {GuildId}",
                channel.GuildId);
            await SetGreet(channel.GuildId, channel.Id, false);
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
            // var rep = new ReplacementBuilder()
            // .WithUser(user)
            // .WithServer(_client, (SocketGuild)user.Guild)
            // .Build();

            var repCtx = new ReplacementContext(client: _client, guild: user.Guild, users: user);
            var smartText = SmartText.CreateFrom(conf.DmGreetMessageText);
            smartText = await _repSvc.ReplaceAsync(smartText, repCtx);

            if (smartText is SmartPlainText pt)
            {
                smartText = new SmartEmbedText()
                {
                    Description = pt.Text
                };
            }

            if (smartText is SmartEmbedText set)
            {
                smartText = set with
                {
                    Footer = CreateFooterSource(user)
                };
            }
            else if (smartText is SmartEmbedTextArray seta)
            {
                // if the greet dm message is a text array
                var ebElem = seta.Embeds.LastOrDefault();
                if (ebElem is null)
                {
                    // if there are no embeds, add an embed with the footer
                    smartText = seta with
                    {
                        Embeds =
                        [
                            new SmartEmbedArrayElementText()
                            {
                                Footer = CreateFooterSource(user)
                            }
                        ]
                    };
                }
                else
                {
                    // if the maximum amount of embeds is reached, edit the last embed
                    if (seta.Embeds.Length >= 10)
                    {
                        seta.Embeds[^1] = seta.Embeds[^1] with
                        {
                            Footer = CreateFooterSource(user)
                        };
                    }
                    else
                    {
                        // if there is less than 10 embeds, add an embed with footer only
                        seta.Embeds = seta.Embeds.Append(new SmartEmbedArrayElementText()
                                          {
                                              Footer = CreateFooterSource(user)
                                          })
                                          .ToArray();
                    }
                }
            }

            await _sender.Response(user).Text(smartText).Sanitize(false).SendAsync();
        }
        catch
        {
            return false;
        }

        return true;
    }

    private static SmartTextEmbedFooter CreateFooterSource(IGuildUser user)
        => new()
        {
            Text = $"This message was sent from {user.Guild} server.",
            IconUrl = user.Guild.IconUrl
        };

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

    public async Task<bool> ToggleBoost(ulong guildId, ulong channelId, bool? forceState = null)
    {
        await using var uow = _db.GetDbContext();
        var conf = uow.GuildConfigsForId(guildId, set => set);

        if (forceState is not bool fs)
            conf.SendBoostMessage = !conf.SendBoostMessage;
        else
            conf.SendBoostMessage = fs;

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
    
    public bool GetBoostEnabled(ulong guildId)
    {
        using var uow = _db.GetDbContext();
        var conf = uow.GuildConfigsForId(guildId, set => set);
        return conf.SendBoostMessage;
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

    public Task<bool> BoostTest(ITextChannel channel, IGuildUser user)
    {
        var conf = GetOrAddSettingsForGuild(user.GuildId);
        return SendBoostMessage(conf, user, channel);
    }

    #endregion
}