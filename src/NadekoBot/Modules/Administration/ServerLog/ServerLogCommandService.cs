using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using NadekoBot.Common.ModuleBehaviors;
using NadekoBot.Db;
using NadekoBot.Modules.Administration.Services;
using NadekoBot.Services.Database.Models;

namespace NadekoBot.Modules.Administration;

public sealed class LogCommandService : ILogCommandService, IReadyExecutor
    #if !GLOBAL_NADEKO
    , INService // don't load this service on global nadeko
    #endif
{
    public ConcurrentDictionary<ulong, LogSetting> GuildLogSettings { get; }

    private ConcurrentDictionary<ITextChannel, List<string>> PresenceUpdates { get; } = new();
    private readonly DiscordSocketClient _client;

    private readonly IBotStrings _strings;
    private readonly DbService _db;
    private readonly MuteService _mute;
    private readonly ProtectionService _prot;
    private readonly GuildTimezoneService _tz;
    private readonly IEmbedBuilderService _eb;
    private readonly IMemoryCache _memoryCache;

    private readonly ConcurrentHashSet<ulong> _ignoreMessageIds = new();

    public LogCommandService(
        DiscordSocketClient client,
        IBotStrings strings,
        DbService db,
        MuteService mute,
        ProtectionService prot,
        GuildTimezoneService tz,
        IMemoryCache memoryCache,
        IEmbedBuilderService eb)
    {
        _client = client;
        _memoryCache = memoryCache;
        _eb = eb;
        _strings = strings;
        _db = db;
        _mute = mute;
        _prot = prot;
        _tz = tz;
        using (var uow = db.GetDbContext())
        {
            var guildIds = client.Guilds.Select(x => x.Id).ToList();
            var configs = uow.LogSettings.AsQueryable()
                             .AsNoTracking()
                             .Where(x => guildIds.Contains(x.GuildId))
                             .Include(ls => ls.LogIgnores)
                             .ToList();

            GuildLogSettings = configs.ToDictionary(ls => ls.GuildId).ToConcurrent();
        }

        //_client.MessageReceived += _client_MessageReceived;
        _client.MessageUpdated += _client_MessageUpdated;
        _client.MessageDeleted += _client_MessageDeleted;
        _client.UserBanned += _client_UserBanned;
        _client.UserUnbanned += _client_UserUnbanned;
        _client.UserJoined += _client_UserJoined;
        _client.UserLeft += _client_UserLeft;
        // _client.PresenceUpdated += _client_UserPresenceUpdated;
        _client.UserVoiceStateUpdated += _client_UserVoiceStateUpdated;
        _client.UserVoiceStateUpdated += _client_UserVoiceStateUpdated_TTS;
        _client.GuildMemberUpdated += _client_GuildUserUpdated;
        _client.UserUpdated += _client_UserUpdated;
        _client.ChannelCreated += _client_ChannelCreated;
        _client.ChannelDestroyed += _client_ChannelDestroyed;
        _client.ChannelUpdated += _client_ChannelUpdated;
        _client.RoleDeleted += _client_RoleDeleted;

        _mute.UserMuted += MuteCommands_UserMuted;
        _mute.UserUnmuted += MuteCommands_UserUnmuted;

        _prot.OnAntiProtectionTriggered += TriggeredAntiProtection;
    }

    public async Task OnReadyAsync()
        => await Task.WhenAll(PresenceUpdateTask(), IgnoreMessageIdsClearTask());

    private async Task IgnoreMessageIdsClearTask()
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        while (await timer.WaitForNextTickAsync())
            _ignoreMessageIds.Clear();
    }

    private async Task PresenceUpdateTask()
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));
        while (await timer.WaitForNextTickAsync())
        {
            try
            {
                var keys = PresenceUpdates.Keys.ToList();

                await keys.Select(key =>
                          {
                              if (!((SocketGuild)key.Guild).CurrentUser.GetPermissions(key).SendMessages)
                                  return Task.CompletedTask;

                              if (PresenceUpdates.TryRemove(key, out var msgs))
                              {
                                  var title = GetText(key.Guild, strs.presence_updates);
                                  var desc = string.Join(Environment.NewLine, msgs);
                                  return key.SendConfirmAsync(_eb, title, desc.TrimTo(2048)!);
                              }

                              return Task.CompletedTask;
                          })
                          .WhenAll();
            }
            catch { }
        }
    }

    public LogSetting? GetGuildLogSettings(ulong guildId)
    {
        GuildLogSettings.TryGetValue(guildId, out var logSetting);
        return logSetting;
    }

    public void AddDeleteIgnore(ulong messageId)
        => _ignoreMessageIds.Add(messageId);

    public bool LogIgnore(ulong gid, ulong itemId, IgnoredItemType itemType)
    {
        using var uow = _db.GetDbContext();
        var logSetting = uow.LogSettingsFor(gid);
        var removed = logSetting.LogIgnores.RemoveAll(x => x.ItemType == itemType && itemId == x.LogItemId);

        if (removed == 0)
        {
            var toAdd = new IgnoredLogItem
            {
                LogItemId = itemId,
                ItemType = itemType
            };
            logSetting.LogIgnores.Add(toAdd);
        }

        uow.SaveChanges();
        GuildLogSettings.AddOrUpdate(gid, logSetting, (_, _) => logSetting);
        return removed > 0;
    }

    private string GetText(IGuild guild, LocStr str)
        => _strings.GetText(str, guild.Id);

    private string PrettyCurrentTime(IGuild? g)
    {
        var time = DateTime.UtcNow;
        if (g is not null)
            time = TimeZoneInfo.ConvertTime(time, _tz.GetTimeZoneOrUtc(g.Id));
        return $"【{time:HH:mm:ss}】";
    }

    private string CurrentTime(IGuild? g)
    {
        var time = DateTime.UtcNow;
        if (g is not null)
            time = TimeZoneInfo.ConvertTime(time, _tz.GetTimeZoneOrUtc(g.Id));

        return $"{time:HH:mm:ss}";
    }

    public async Task LogServer(ulong guildId, ulong channelId, bool value)
    {
        await using var uow = _db.GetDbContext();
        var logSetting = uow.LogSettingsFor(guildId);

        logSetting.LogOtherId = logSetting.MessageUpdatedId = logSetting.MessageDeletedId = logSetting.UserJoinedId =
            logSetting.UserLeftId = logSetting.UserBannedId = logSetting.UserUnbannedId = logSetting.UserUpdatedId =
                logSetting.ChannelCreatedId = logSetting.ChannelDestroyedId = logSetting.ChannelUpdatedId =
                    logSetting.LogUserPresenceId = logSetting.LogVoicePresenceId = logSetting.UserMutedId =
                        logSetting.LogVoicePresenceTTSId = value ? channelId : null;
        await uow.SaveChangesAsync();
        GuildLogSettings.AddOrUpdate(guildId, _ => logSetting, (_, _) => logSetting);
    }

    private Task _client_UserUpdated(SocketUser before, SocketUser uAfter)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                if (uAfter is not SocketGuildUser after)
                    return;

                var g = after.Guild;

                if (!GuildLogSettings.TryGetValue(g.Id, out var logSetting) || logSetting.UserUpdatedId is null)
                    return;

                ITextChannel? logChannel;
                if ((logChannel = await TryGetLogChannel(g, logSetting, LogType.UserUpdated)) is null)
                    return;

                var embed = _eb.Create();

                if (before.Username != after.Username)
                {
                    embed.WithTitle("👥 " + GetText(g, strs.username_changed))
                         .WithDescription($"{before.Username}#{before.Discriminator} | {before.Id}")
                         .AddField("Old Name", $"{before.Username}", true)
                         .AddField("New Name", $"{after.Username}", true)
                         .WithFooter(CurrentTime(g))
                         .WithOkColor();
                }
                else if (before.AvatarId != after.AvatarId)
                {
                    embed.WithTitle("👥" + GetText(g, strs.avatar_changed))
                         .WithDescription($"{before.Username}#{before.Discriminator} | {before.Id}")
                         .WithFooter(CurrentTime(g))
                         .WithOkColor();

                    var bav = before.RealAvatarUrl();
                    if (bav.IsAbsoluteUri)
                        embed.WithThumbnailUrl(bav.ToString());

                    var aav = after.RealAvatarUrl();
                    if (aav.IsAbsoluteUri)
                        embed.WithImageUrl(aav.ToString());
                }
                else
                    return;

                await logChannel.EmbedAsync(embed);
            }
            catch
            {
                // ignored
            }
        });
        return Task.CompletedTask;
    }

    public bool Log(ulong gid, ulong? cid, LogType type /*, string options*/)
    {
        ulong? channelId = null;
        using (var uow = _db.GetDbContext())
        {
            var logSetting = uow.LogSettingsFor(gid);
            GuildLogSettings.AddOrUpdate(gid, _ => logSetting, (_, _) => logSetting);
            switch (type)
            {
                case LogType.Other:
                    channelId = logSetting.LogOtherId = logSetting.LogOtherId is null ? cid : default;
                    break;
                case LogType.MessageUpdated:
                    channelId = logSetting.MessageUpdatedId = logSetting.MessageUpdatedId is null ? cid : default;
                    break;
                case LogType.MessageDeleted:
                    channelId = logSetting.MessageDeletedId = logSetting.MessageDeletedId is null ? cid : default;
                    //logSetting.DontLogBotMessageDeleted = (options == "nobot");
                    break;
                case LogType.UserJoined:
                    channelId = logSetting.UserJoinedId = logSetting.UserJoinedId is null ? cid : default;
                    break;
                case LogType.UserLeft:
                    channelId = logSetting.UserLeftId = logSetting.UserLeftId is null ? cid : default;
                    break;
                case LogType.UserBanned:
                    channelId = logSetting.UserBannedId = logSetting.UserBannedId is null ? cid : default;
                    break;
                case LogType.UserUnbanned:
                    channelId = logSetting.UserUnbannedId = logSetting.UserUnbannedId is null ? cid : default;
                    break;
                case LogType.UserUpdated:
                    channelId = logSetting.UserUpdatedId = logSetting.UserUpdatedId is null ? cid : default;
                    break;
                case LogType.UserMuted:
                    channelId = logSetting.UserMutedId = logSetting.UserMutedId is null ? cid : default;
                    break;
                case LogType.ChannelCreated:
                    channelId = logSetting.ChannelCreatedId = logSetting.ChannelCreatedId is null ? cid : default;
                    break;
                case LogType.ChannelDestroyed:
                    channelId = logSetting.ChannelDestroyedId = logSetting.ChannelDestroyedId is null ? cid : default;
                    break;
                case LogType.ChannelUpdated:
                    channelId = logSetting.ChannelUpdatedId = logSetting.ChannelUpdatedId is null ? cid : default;
                    break;
                case LogType.UserPresence:
                    channelId = logSetting.LogUserPresenceId = logSetting.LogUserPresenceId is null ? cid : default;
                    break;
                case LogType.VoicePresence:
                    channelId = logSetting.LogVoicePresenceId = logSetting.LogVoicePresenceId is null ? cid : default;
                    break;
                case LogType.VoicePresenceTts:
                    channelId = logSetting.LogVoicePresenceTTSId =
                        logSetting.LogVoicePresenceTTSId is null ? cid : default;
                    break;
            }

            uow.SaveChanges();
        }

        return channelId is not null;
    }

    private Task _client_UserVoiceStateUpdated_TTS(SocketUser iusr, SocketVoiceState before, SocketVoiceState after)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                if (iusr is not IGuildUser usr)
                    return;

                var beforeVch = before.VoiceChannel;
                var afterVch = after.VoiceChannel;

                if (beforeVch == afterVch)
                    return;

                if (!GuildLogSettings.TryGetValue(usr.Guild.Id, out var logSetting)
                    || logSetting.LogVoicePresenceTTSId is null)
                    return;

                ITextChannel? logChannel;
                if ((logChannel = await TryGetLogChannel(usr.Guild, logSetting, LogType.VoicePresenceTts)) is null)
                    return;

                var str = string.Empty;
                if (beforeVch?.Guild == afterVch?.Guild)
                    str = GetText(logChannel.Guild, strs.log_vc_moved(usr.Username, beforeVch?.Name, afterVch?.Name));
                else if (beforeVch is null)
                    str = GetText(logChannel.Guild, strs.log_vc_joined(usr.Username, afterVch?.Name));
                else if (afterVch is null)
                    str = GetText(logChannel.Guild, strs.log_vc_left(usr.Username, beforeVch.Name));

                var toDelete = await logChannel.SendMessageAsync(str, true);
                toDelete.DeleteAfter(5);
            }
            catch
            {
                // ignored
            }
        });
        return Task.CompletedTask;
    }

    private void MuteCommands_UserMuted(
        IGuildUser usr,
        IUser mod,
        MuteType muteType,
        string reason)
        => _ = Task.Run(async () =>
        {
            try
            {
                if (!GuildLogSettings.TryGetValue(usr.Guild.Id, out var logSetting) || logSetting.UserMutedId is null)
                    return;

                ITextChannel? logChannel;
                if ((logChannel = await TryGetLogChannel(usr.Guild, logSetting, LogType.UserMuted)) is null)
                    return;
                var mutes = string.Empty;
                var mutedLocalized = GetText(logChannel.Guild, strs.muted_sn);
                switch (muteType)
                {
                    case MuteType.Voice:
                        mutes = "🔇 " + GetText(logChannel.Guild, strs.xmuted_voice(mutedLocalized, mod.ToString()));
                        break;
                    case MuteType.Chat:
                        mutes = "🔇 " + GetText(logChannel.Guild, strs.xmuted_text(mutedLocalized, mod.ToString()));
                        break;
                    case MuteType.All:
                        mutes = "🔇 "
                                + GetText(logChannel.Guild, strs.xmuted_text_and_voice(mutedLocalized, mod.ToString()));
                        break;
                }

                var embed = _eb.Create()
                               .WithAuthor(mutes)
                               .WithTitle($"{usr.Username}#{usr.Discriminator} | {usr.Id}")
                               .WithFooter(CurrentTime(usr.Guild))
                               .WithOkColor();

                await logChannel.EmbedAsync(embed);
            }
            catch
            {
                // ignored
            }
        });

    private void MuteCommands_UserUnmuted(
        IGuildUser usr,
        IUser mod,
        MuteType muteType,
        string reason)
        => _ = Task.Run(async () =>
        {
            try
            {
                if (!GuildLogSettings.TryGetValue(usr.Guild.Id, out var logSetting) || logSetting.UserMutedId is null)
                    return;

                ITextChannel? logChannel;
                if ((logChannel = await TryGetLogChannel(usr.Guild, logSetting, LogType.UserMuted)) is null)
                    return;

                var mutes = string.Empty;
                var unmutedLocalized = GetText(logChannel.Guild, strs.unmuted_sn);
                switch (muteType)
                {
                    case MuteType.Voice:
                        mutes = "🔊 " + GetText(logChannel.Guild, strs.xmuted_voice(unmutedLocalized, mod.ToString()));
                        break;
                    case MuteType.Chat:
                        mutes = "🔊 " + GetText(logChannel.Guild, strs.xmuted_text(unmutedLocalized, mod.ToString()));
                        break;
                    case MuteType.All:
                        mutes = "🔊 "
                                + GetText(logChannel.Guild,
                                    strs.xmuted_text_and_voice(unmutedLocalized, mod.ToString()));
                        break;
                }

                var embed = _eb.Create()
                               .WithAuthor(mutes)
                               .WithTitle($"{usr.Username}#{usr.Discriminator} | {usr.Id}")
                               .WithFooter($"{CurrentTime(usr.Guild)}")
                               .WithOkColor();

                if (!string.IsNullOrWhiteSpace(reason))
                    embed.WithDescription(reason);

                await logChannel.EmbedAsync(embed);
            }
            catch
            {
                // ignored
            }
        });

    public Task TriggeredAntiProtection(PunishmentAction action, ProtectionType protection, params IGuildUser[] users)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                if (users.Length == 0)
                    return;

                if (!GuildLogSettings.TryGetValue(users.First().Guild.Id, out var logSetting)
                    || logSetting.LogOtherId is null)
                    return;

                ITextChannel? logChannel;
                if ((logChannel = await TryGetLogChannel(users.First().Guild, logSetting, LogType.Other)) is null)
                    return;

                var punishment = string.Empty;
                switch (action)
                {
                    case PunishmentAction.Mute:
                        punishment = "🔇 " + GetText(logChannel.Guild, strs.muted_pl).ToUpperInvariant();
                        break;
                    case PunishmentAction.Kick:
                        punishment = "👢 " + GetText(logChannel.Guild, strs.kicked_pl).ToUpperInvariant();
                        break;
                    case PunishmentAction.Softban:
                        punishment = "☣ " + GetText(logChannel.Guild, strs.soft_banned_pl).ToUpperInvariant();
                        break;
                    case PunishmentAction.Ban:
                        punishment = "⛔️ " + GetText(logChannel.Guild, strs.banned_pl).ToUpperInvariant();
                        break;
                    case PunishmentAction.RemoveRoles:
                        punishment = "⛔️ " + GetText(logChannel.Guild, strs.remove_roles_pl).ToUpperInvariant();
                        break;
                }

                var embed = _eb.Create()
                               .WithAuthor($"🛡 Anti-{protection}")
                               .WithTitle(GetText(logChannel.Guild, strs.users) + " " + punishment)
                               .WithDescription(string.Join("\n", users.Select(u => u.ToString())))
                               .WithFooter(CurrentTime(logChannel.Guild))
                               .WithOkColor();

                await logChannel.EmbedAsync(embed);
            }
            catch
            {
                // ignored
            }
        });
        return Task.CompletedTask;
    }

    private string GetRoleDeletedKey(ulong roleId)
        => $"role_deleted_{roleId}";

    private Task _client_RoleDeleted(SocketRole socketRole)
    {
        Serilog.Log.Information("Role deleted {RoleId}", socketRole.Id);
        _memoryCache.Set(GetRoleDeletedKey(socketRole.Id), true, TimeSpan.FromMinutes(5));
        return Task.CompletedTask;
    }

    private bool IsRoleDeleted(ulong roleId)
    {
        var isDeleted = _memoryCache.TryGetValue(GetRoleDeletedKey(roleId), out _);
        return isDeleted;
    }

    private Task _client_GuildUserUpdated(Cacheable<SocketGuildUser, ulong> optBefore, SocketGuildUser after)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var before = await optBefore.GetOrDownloadAsync();

                if (before is null)
                    return;

                if (!GuildLogSettings.TryGetValue(before.Guild.Id, out var logSetting)
                    || logSetting.LogIgnores.Any(ilc
                        => ilc.LogItemId == after.Id && ilc.ItemType == IgnoredItemType.User))
                    return;

                ITextChannel? logChannel;
                if (logSetting.UserUpdatedId is not null
                    && (logChannel = await TryGetLogChannel(before.Guild, logSetting, LogType.UserUpdated)) is not null)
                {
                    var embed = _eb.Create()
                                   .WithOkColor()
                                   .WithFooter(CurrentTime(before.Guild))
                                   .WithTitle($"{before.Username}#{before.Discriminator} | {before.Id}");
                    if (before.Nickname != after.Nickname)
                    {
                        embed.WithAuthor("👥 " + GetText(logChannel.Guild, strs.nick_change))
                             .AddField(GetText(logChannel.Guild, strs.old_nick),
                                 $"{before.Nickname}#{before.Discriminator}")
                             .AddField(GetText(logChannel.Guild, strs.new_nick),
                                 $"{after.Nickname}#{after.Discriminator}");

                        await logChannel.EmbedAsync(embed);
                    }
                    else if (!before.Roles.SequenceEqual(after.Roles))
                    {
                        if (before.Roles.Count < after.Roles.Count)
                        {
                            var diffRoles = after.Roles.Where(r => !before.Roles.Contains(r)).Select(r => r.Name);
                            embed.WithAuthor("⚔ " + GetText(logChannel.Guild, strs.user_role_add))
                                 .WithDescription(string.Join(", ", diffRoles).SanitizeMentions());

                            await logChannel.EmbedAsync(embed);
                        }
                        else if (before.Roles.Count > after.Roles.Count)
                        {
                            await Task.Delay(1000);
                            var diffRoles = before.Roles.Where(r => !after.Roles.Contains(r) && !IsRoleDeleted(r.Id))
                                                  .Select(r => r.Name)
                                                  .ToList();

                            if (diffRoles.Any())
                            {
                                embed.WithAuthor("⚔ " + GetText(logChannel.Guild, strs.user_role_rem))
                                     .WithDescription(string.Join(", ", diffRoles).SanitizeMentions());

                                await logChannel.EmbedAsync(embed);
                            }
                        }
                    }
                }

                if (!before.IsBot
                    && logSetting.LogUserPresenceId is not null
                    && (logChannel =
                        await TryGetLogChannel(before.Guild, logSetting, LogType.UserPresence)) is not null)
                {
                    if (before.Status != after.Status)
                    {
                        var str = "🎭"
                                  + Format.Code(PrettyCurrentTime(after.Guild))
                                  + GetText(logChannel.Guild,
                                      strs.user_status_change("👤" + Format.Bold(after.Username),
                                          Format.Bold(after.Status.ToString())));
                        PresenceUpdates.AddOrUpdate(logChannel,
                            new List<string>
                            {
                                str
                            },
                            (_, list) =>
                            {
                                list.Add(str);
                                return list;
                            });
                    }
                    else if (before.Activities.FirstOrDefault()?.Name != after.Activities.FirstOrDefault()?.Name)
                    {
                        var str =
                            $"👾`{PrettyCurrentTime(after.Guild)}`👤__**{after.Username}**__ is now playing **{after.Activities.FirstOrDefault()?.Name ?? "-"}**.";
                        PresenceUpdates.AddOrUpdate(logChannel,
                            new List<string>
                            {
                                str
                            },
                            (_, list) =>
                            {
                                list.Add(str);
                                return list;
                            });
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

    private Task _client_ChannelUpdated(IChannel cbefore, IChannel cafter)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                if (cbefore is not IGuildChannel before)
                    return;

                var after = (IGuildChannel)cafter;

                if (!GuildLogSettings.TryGetValue(before.Guild.Id, out var logSetting)
                    || logSetting.ChannelUpdatedId is null
                    || logSetting.LogIgnores.Any(ilc
                        => ilc.LogItemId == after.Id && ilc.ItemType == IgnoredItemType.Channel))
                    return;

                ITextChannel? logChannel;
                if ((logChannel = await TryGetLogChannel(before.Guild, logSetting, LogType.ChannelUpdated)) is null)
                    return;

                var embed = _eb.Create().WithOkColor().WithFooter(CurrentTime(before.Guild));

                var beforeTextChannel = cbefore as ITextChannel;
                var afterTextChannel = cafter as ITextChannel;

                if (before.Name != after.Name)
                {
                    embed.WithTitle("ℹ️ " + GetText(logChannel.Guild, strs.ch_name_change))
                         .WithDescription($"{after} | {after.Id}")
                         .AddField(GetText(logChannel.Guild, strs.ch_old_name), before.Name);
                }
                else if (beforeTextChannel?.Topic != afterTextChannel?.Topic)
                {
                    embed.WithTitle("ℹ️ " + GetText(logChannel.Guild, strs.ch_topic_change))
                         .WithDescription($"{after} | {after.Id}")
                         .AddField(GetText(logChannel.Guild, strs.old_topic), beforeTextChannel?.Topic ?? "-")
                         .AddField(GetText(logChannel.Guild, strs.new_topic), afterTextChannel?.Topic ?? "-");
                }
                else
                    return;

                await logChannel.EmbedAsync(embed);
            }
            catch
            {
                // ignored
            }
        });
        return Task.CompletedTask;
    }

    private Task _client_ChannelDestroyed(IChannel ich)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                if (ich is not IGuildChannel ch)
                    return;

                if (!GuildLogSettings.TryGetValue(ch.Guild.Id, out var logSetting)
                    || logSetting.ChannelDestroyedId is null
                    || logSetting.LogIgnores.Any(ilc
                        => ilc.LogItemId == ch.Id && ilc.ItemType == IgnoredItemType.Channel))
                    return;

                ITextChannel? logChannel;
                if ((logChannel = await TryGetLogChannel(ch.Guild, logSetting, LogType.ChannelDestroyed)) is null)
                    return;
                string title;
                if (ch is IVoiceChannel)
                    title = GetText(logChannel.Guild, strs.voice_chan_destroyed);
                else
                    title = GetText(logChannel.Guild, strs.text_chan_destroyed);

                await logChannel.EmbedAsync(_eb.Create()
                                               .WithOkColor()
                                               .WithTitle("🆕 " + title)
                                               .WithDescription($"{ch.Name} | {ch.Id}")
                                               .WithFooter(CurrentTime(ch.Guild)));
            }
            catch
            {
                // ignored
            }
        });
        return Task.CompletedTask;
    }

    private Task _client_ChannelCreated(IChannel ich)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                if (ich is not IGuildChannel ch)
                    return;

                if (!GuildLogSettings.TryGetValue(ch.Guild.Id, out var logSetting)
                    || logSetting.ChannelCreatedId is null)
                    return;

                ITextChannel? logChannel;
                if ((logChannel = await TryGetLogChannel(ch.Guild, logSetting, LogType.ChannelCreated)) is null)
                    return;
                string title;
                if (ch is IVoiceChannel)
                    title = GetText(logChannel.Guild, strs.voice_chan_created);
                else
                    title = GetText(logChannel.Guild, strs.text_chan_created);

                await logChannel.EmbedAsync(_eb.Create()
                                               .WithOkColor()
                                               .WithTitle("🆕 " + title)
                                               .WithDescription($"{ch.Name} | {ch.Id}")
                                               .WithFooter(CurrentTime(ch.Guild)));
            }
            catch (Exception)
            {
                // ignored
            }
        });
        return Task.CompletedTask;
    }

    private Task _client_UserVoiceStateUpdated(SocketUser iusr, SocketVoiceState before, SocketVoiceState after)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                if (iusr is not IGuildUser usr || usr.IsBot)
                    return;

                var beforeVch = before.VoiceChannel;
                var afterVch = after.VoiceChannel;

                if (beforeVch == afterVch)
                    return;

                if (!GuildLogSettings.TryGetValue(usr.Guild.Id, out var logSetting)
                    || logSetting.LogVoicePresenceId is null
                    || logSetting.LogIgnores.Any(
                        ilc => ilc.LogItemId == iusr.Id && ilc.ItemType == IgnoredItemType.User))
                    return;

                ITextChannel? logChannel;
                if ((logChannel = await TryGetLogChannel(usr.Guild, logSetting, LogType.VoicePresence)) is null)
                    return;

                var str = string.Empty;
                if (beforeVch?.Guild == afterVch?.Guild)
                {
                    str = "🎙"
                          + Format.Code(PrettyCurrentTime(usr.Guild))
                          + GetText(logChannel.Guild,
                              strs.user_vmoved("👤" + Format.Bold(usr.Username + "#" + usr.Discriminator),
                                  Format.Bold(beforeVch?.Name ?? ""),
                                  Format.Bold(afterVch?.Name ?? "")));
                }
                else if (beforeVch is null)
                {
                    str = "🎙"
                          + Format.Code(PrettyCurrentTime(usr.Guild))
                          + GetText(logChannel.Guild,
                              strs.user_vjoined("👤" + Format.Bold(usr.Username + "#" + usr.Discriminator),
                                  Format.Bold(afterVch?.Name ?? "")));
                }
                else if (afterVch is null)
                {
                    str = "🎙"
                          + Format.Code(PrettyCurrentTime(usr.Guild))
                          + GetText(logChannel.Guild,
                              strs.user_vleft("👤" + Format.Bold(usr.Username + "#" + usr.Discriminator),
                                  Format.Bold(beforeVch.Name ?? "")));
                }

                if (!string.IsNullOrWhiteSpace(str))
                {
                    PresenceUpdates.AddOrUpdate(logChannel,
                        new List<string>
                        {
                            str
                        },
                        (_, list) =>
                        {
                            list.Add(str);
                            return list;
                        });
                }
            }
            catch
            {
                // ignored
            }
        });
        return Task.CompletedTask;
    }

    private Task _client_UserLeft(SocketGuild guild, SocketUser usr)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                if (!GuildLogSettings.TryGetValue(guild.Id, out var logSetting)
                    || logSetting.UserLeftId is null
                    || logSetting.LogIgnores.Any(ilc
                        => ilc.LogItemId == usr.Id && ilc.ItemType == IgnoredItemType.User))
                    return;

                ITextChannel? logChannel;
                if ((logChannel = await TryGetLogChannel(guild, logSetting, LogType.UserLeft)) is null)
                    return;
                var embed = _eb.Create()
                               .WithOkColor()
                               .WithTitle("❌ " + GetText(logChannel.Guild, strs.user_left))
                               .WithDescription(usr.ToString())
                               .AddField("Id", usr.Id.ToString())
                               .WithFooter(CurrentTime(guild));

                if (Uri.IsWellFormedUriString(usr.GetAvatarUrl(), UriKind.Absolute))
                    embed.WithThumbnailUrl(usr.GetAvatarUrl());

                await logChannel.EmbedAsync(embed);
            }
            catch
            {
                // ignored
            }
        });
        return Task.CompletedTask;
    }

    private Task _client_UserJoined(IGuildUser usr)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                if (!GuildLogSettings.TryGetValue(usr.Guild.Id, out var logSetting) || logSetting.UserJoinedId is null)
                    return;

                ITextChannel? logChannel;
                if ((logChannel = await TryGetLogChannel(usr.Guild, logSetting, LogType.UserJoined)) is null)
                    return;

                var embed = _eb.Create()
                               .WithOkColor()
                               .WithTitle("✅ " + GetText(logChannel.Guild, strs.user_joined))
                               .WithDescription($"{usr.Mention} `{usr}`")
                               .AddField("Id", usr.Id.ToString())
                               .AddField(GetText(logChannel.Guild, strs.joined_server),
                                   $"{usr.JoinedAt?.ToString("dd.MM.yyyy HH:mm") ?? "?"}",
                                   true)
                               .AddField(GetText(logChannel.Guild, strs.joined_discord),
                                   $"{usr.CreatedAt:dd.MM.yyyy HH:mm}",
                                   true)
                               .WithFooter(CurrentTime(usr.Guild));

                if (Uri.IsWellFormedUriString(usr.GetAvatarUrl(), UriKind.Absolute))
                    embed.WithThumbnailUrl(usr.GetAvatarUrl());

                await logChannel.EmbedAsync(embed);
            }
            catch (Exception)
            {
                // ignored
            }
        });
        return Task.CompletedTask;
    }

    private Task _client_UserUnbanned(IUser usr, IGuild guild)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                if (!GuildLogSettings.TryGetValue(guild.Id, out var logSetting)
                    || logSetting.UserUnbannedId is null
                    || logSetting.LogIgnores.Any(ilc
                        => ilc.LogItemId == usr.Id && ilc.ItemType == IgnoredItemType.User))
                    return;

                ITextChannel? logChannel;
                if ((logChannel = await TryGetLogChannel(guild, logSetting, LogType.UserUnbanned)) is null)
                    return;
                var embed = _eb.Create()
                               .WithOkColor()
                               .WithTitle("♻️ " + GetText(logChannel.Guild, strs.user_unbanned))
                               .WithDescription(usr.ToString())
                               .AddField("Id", usr.Id.ToString())
                               .WithFooter(CurrentTime(guild));

                if (Uri.IsWellFormedUriString(usr.GetAvatarUrl(), UriKind.Absolute))
                    embed.WithThumbnailUrl(usr.GetAvatarUrl());

                await logChannel.EmbedAsync(embed);
            }
            catch (Exception)
            {
                // ignored
            }
        });
        return Task.CompletedTask;
    }

    private Task _client_UserBanned(IUser usr, IGuild guild)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                if (!GuildLogSettings.TryGetValue(guild.Id, out var logSetting)
                    || logSetting.UserBannedId is null
                    || logSetting.LogIgnores.Any(ilc
                        => ilc.LogItemId == usr.Id && ilc.ItemType == IgnoredItemType.User))
                    return;

                ITextChannel? logChannel;
                if ((logChannel = await TryGetLogChannel(guild, logSetting, LogType.UserBanned)) == null)
                    return;
                var embed = _eb.Create()
                               .WithOkColor()
                               .WithTitle("🚫 " + GetText(logChannel.Guild, strs.user_banned))
                               .WithDescription(usr.ToString())
                               .AddField("Id", usr.Id.ToString())
                               .WithFooter(CurrentTime(guild));

                var avatarUrl = usr.GetAvatarUrl();

                if (Uri.IsWellFormedUriString(avatarUrl, UriKind.Absolute))
                    embed.WithThumbnailUrl(usr.GetAvatarUrl());

                await logChannel.EmbedAsync(embed);
            }
            catch (Exception)
            {
                // ignored
            }
        });
        return Task.CompletedTask;
    }

    private Task _client_MessageDeleted(Cacheable<IMessage, ulong> optMsg, Cacheable<IMessageChannel, ulong> optCh)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                if (optMsg.Value is not IUserMessage msg || msg.IsAuthor(_client))
                    return;

                if (_ignoreMessageIds.Contains(msg.Id))
                    return;

                var ch = optCh.Value;
                if (ch is not ITextChannel channel)
                    return;

                if (!GuildLogSettings.TryGetValue(channel.Guild.Id, out var logSetting)
                    || logSetting.MessageDeletedId is null
                    || logSetting.LogIgnores.Any(ilc
                        => ilc.LogItemId == channel.Id && ilc.ItemType == IgnoredItemType.Channel))
                    return;

                ITextChannel? logChannel;
                if ((logChannel = await TryGetLogChannel(channel.Guild, logSetting, LogType.MessageDeleted)) is null
                    || logChannel.Id == msg.Id)
                    return;

                var resolvedMessage = msg.Resolve(TagHandling.FullName);
                var embed = _eb.Create()
                               .WithOkColor()
                               .WithTitle("🗑 "
                                          + GetText(logChannel.Guild, strs.msg_del(((ITextChannel)msg.Channel).Name)))
                               .WithDescription(msg.Author.ToString())
                               .AddField(GetText(logChannel.Guild, strs.content),
                                   string.IsNullOrWhiteSpace(resolvedMessage) ? "-" : resolvedMessage)
                               .AddField("Id", msg.Id.ToString())
                               .WithFooter(CurrentTime(channel.Guild));
                if (msg.Attachments.Any())
                {
                    embed.AddField(GetText(logChannel.Guild, strs.attachments),
                        string.Join(", ", msg.Attachments.Select(a => a.Url)));
                }

                await logChannel.EmbedAsync(embed);
            }
            catch (Exception)
            {
                // ignored
            }
        });
        return Task.CompletedTask;
    }

    private Task _client_MessageUpdated(
        Cacheable<IMessage, ulong> optmsg,
        SocketMessage imsg2,
        ISocketMessageChannel ch)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                if (imsg2 is not IUserMessage after || after.IsAuthor(_client))
                    return;

                if ((optmsg.HasValue ? optmsg.Value : null) is not IUserMessage before)
                    return;

                if (ch is not ITextChannel channel)
                    return;

                if (before.Content == after.Content)
                    return;

                if (before.Author.IsBot)
                    return;

                if (!GuildLogSettings.TryGetValue(channel.Guild.Id, out var logSetting)
                    || logSetting.MessageUpdatedId is null
                    || logSetting.LogIgnores.Any(ilc
                        => ilc.LogItemId == channel.Id && ilc.ItemType == IgnoredItemType.Channel))
                    return;

                ITextChannel? logChannel;
                if ((logChannel = await TryGetLogChannel(channel.Guild, logSetting, LogType.MessageUpdated)) is null
                    || logChannel.Id == after.Channel.Id)
                    return;

                var embed = _eb.Create()
                               .WithOkColor()
                               .WithTitle("📝 "
                                          + GetText(logChannel.Guild,
                                              strs.msg_update(((ITextChannel)after.Channel).Name)))
                               .WithDescription(after.Author.ToString())
                               .AddField(GetText(logChannel.Guild, strs.old_msg),
                                   string.IsNullOrWhiteSpace(before.Content)
                                       ? "-"
                                       : before.Resolve(TagHandling.FullName))
                               .AddField(GetText(logChannel.Guild, strs.new_msg),
                                   string.IsNullOrWhiteSpace(after.Content) ? "-" : after.Resolve(TagHandling.FullName))
                               .AddField("Id", after.Id.ToString())
                               .WithFooter(CurrentTime(channel.Guild));

                await logChannel.EmbedAsync(embed);
            }
            catch
            {
                // ignored
            }
        });
        return Task.CompletedTask;
    }

    private async Task<ITextChannel?> TryGetLogChannel(IGuild guild, LogSetting logSetting, LogType logChannelType)
    {
        ulong? id = null;
        switch (logChannelType)
        {
            case LogType.Other:
                id = logSetting.LogOtherId;
                break;
            case LogType.MessageUpdated:
                id = logSetting.MessageUpdatedId;
                break;
            case LogType.MessageDeleted:
                id = logSetting.MessageDeletedId;
                break;
            case LogType.UserJoined:
                id = logSetting.UserJoinedId;
                break;
            case LogType.UserLeft:
                id = logSetting.UserLeftId;
                break;
            case LogType.UserBanned:
                id = logSetting.UserBannedId;
                break;
            case LogType.UserUnbanned:
                id = logSetting.UserUnbannedId;
                break;
            case LogType.UserUpdated:
                id = logSetting.UserUpdatedId;
                break;
            case LogType.ChannelCreated:
                id = logSetting.ChannelCreatedId;
                break;
            case LogType.ChannelDestroyed:
                id = logSetting.ChannelDestroyedId;
                break;
            case LogType.ChannelUpdated:
                id = logSetting.ChannelUpdatedId;
                break;
            case LogType.UserPresence:
                id = logSetting.LogUserPresenceId;
                break;
            case LogType.VoicePresence:
                id = logSetting.LogVoicePresenceId;
                break;
            case LogType.VoicePresenceTts:
                id = logSetting.LogVoicePresenceTTSId;
                break;
            case LogType.UserMuted:
                id = logSetting.UserMutedId;
                break;
        }

        if (id is null or 0)
        {
            UnsetLogSetting(guild.Id, logChannelType);
            return null;
        }

        var channel = await guild.GetTextChannelAsync(id.Value);

        if (channel is null)
        {
            UnsetLogSetting(guild.Id, logChannelType);
            return null;
        }

        return channel;
    }

    private void UnsetLogSetting(ulong guildId, LogType logChannelType)
    {
        using var uow = _db.GetDbContext();
        var newLogSetting = uow.LogSettingsFor(guildId);
        switch (logChannelType)
        {
            case LogType.Other:
                newLogSetting.LogOtherId = null;
                break;
            case LogType.MessageUpdated:
                newLogSetting.MessageUpdatedId = null;
                break;
            case LogType.MessageDeleted:
                newLogSetting.MessageDeletedId = null;
                break;
            case LogType.UserJoined:
                newLogSetting.UserJoinedId = null;
                break;
            case LogType.UserLeft:
                newLogSetting.UserLeftId = null;
                break;
            case LogType.UserBanned:
                newLogSetting.UserBannedId = null;
                break;
            case LogType.UserUnbanned:
                newLogSetting.UserUnbannedId = null;
                break;
            case LogType.UserUpdated:
                newLogSetting.UserUpdatedId = null;
                break;
            case LogType.UserMuted:
                newLogSetting.UserMutedId = null;
                break;
            case LogType.ChannelCreated:
                newLogSetting.ChannelCreatedId = null;
                break;
            case LogType.ChannelDestroyed:
                newLogSetting.ChannelDestroyedId = null;
                break;
            case LogType.ChannelUpdated:
                newLogSetting.ChannelUpdatedId = null;
                break;
            case LogType.UserPresence:
                newLogSetting.LogUserPresenceId = null;
                break;
            case LogType.VoicePresence:
                newLogSetting.LogVoicePresenceId = null;
                break;
            case LogType.VoicePresenceTts:
                newLogSetting.LogVoicePresenceTTSId = null;
                break;
        }

        GuildLogSettings.AddOrUpdate(guildId, newLogSetting, (_, _) => newLogSetting);
        uow.SaveChanges();
    }
}