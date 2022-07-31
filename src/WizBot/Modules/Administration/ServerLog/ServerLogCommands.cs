﻿using WizBot.Common.TypeReaders.Models;
using WizBot.Services.Database.Models;

namespace WizBot.Modules.Administration;

public partial class Administration
{
    [Group]
    public partial class LogCommands : WizBotModule<ILogCommandService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task LogServer(PermissionAction action)
        {
            await _service.LogServer(ctx.Guild.Id, ctx.Channel.Id, action.Value);
            if (action.Value)
                await ReplyConfirmLocalizedAsync(strs.log_all);
            else
                await ReplyConfirmLocalizedAsync(strs.log_disabled);
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async partial Task LogIgnore()
        {
            var settings = _service.GetGuildLogSettings(ctx.Guild.Id);

            var chs = settings?.LogIgnores.Where(x => x.ItemType == IgnoredItemType.Channel).ToList()
                      ?? new List<IgnoredLogItem>();
            var usrs = settings?.LogIgnores.Where(x => x.ItemType == IgnoredItemType.User).ToList()
                       ?? new List<IgnoredLogItem>();

            var eb = _eb.Create(ctx)
                        .WithOkColor()
                        .AddField(GetText(strs.log_ignored_channels),
                            chs.Count == 0
                                ? "-"
                                : string.Join('\n', chs.Select(x => $"{x.LogItemId} | <#{x.LogItemId}>")))
                        .AddField(GetText(strs.log_ignored_users),
                            usrs.Count == 0
                                ? "-"
                                : string.Join('\n', usrs.Select(x => $"{x.LogItemId} | <@{x.LogItemId}>")));

            await ctx.Channel.EmbedAsync(eb);
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task LogIgnore([Leftover] ITextChannel target)
        {
            var removed = _service.LogIgnore(ctx.Guild.Id, target.Id, IgnoredItemType.Channel);

            if (!removed)
            {
                await ReplyConfirmLocalizedAsync(
                    strs.log_ignore_chan(Format.Bold(target.Mention + "(" + target.Id + ")")));
            }
            else
            {
                await ReplyConfirmLocalizedAsync(
                    strs.log_not_ignore_chan(Format.Bold(target.Mention + "(" + target.Id + ")")));
            }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task LogIgnore([Leftover] IUser target)
        {
            var removed = _service.LogIgnore(ctx.Guild.Id, target.Id, IgnoredItemType.User);

            if (!removed)
            {
                await ReplyConfirmLocalizedAsync(
                    strs.log_ignore_user(Format.Bold(target.Mention + "(" + target.Id + ")")));
            }
            else
            {
                await ReplyConfirmLocalizedAsync(
                    strs.log_not_ignore_user(Format.Bold(target.Mention + "(" + target.Id + ")")));
            }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task LogEvents()
        {
            var logSetting = _service.GetGuildLogSettings(ctx.Guild.Id);
            var str = string.Join("\n",
                Enum.GetNames(typeof(LogType))
                    .Select(x =>
                    {
                        var val = logSetting is null ? null : GetLogProperty(logSetting, Enum.Parse<LogType>(x));
                        if (val is not null)
                            return $"{Format.Bold(x)} <#{val}>";
                        return Format.Bold(x);
                    }));

            await SendConfirmAsync(Format.Bold(GetText(strs.log_events)) + "\n" + str);
        }

        private static ulong? GetLogProperty(LogSetting l, LogType type)
        {
            switch (type)
            {
                case LogType.Other:
                    return l.LogOtherId;
                case LogType.MessageUpdated:
                    return l.MessageUpdatedId;
                case LogType.MessageDeleted:
                    return l.MessageDeletedId;
                case LogType.UserJoined:
                    return l.UserJoinedId;
                case LogType.UserLeft:
                    return l.UserLeftId;
                case LogType.UserBanned:
                    return l.UserBannedId;
                case LogType.UserUnbanned:
                    return l.UserUnbannedId;
                case LogType.UserUpdated:
                    return l.UserUpdatedId;
                case LogType.ChannelCreated:
                    return l.ChannelCreatedId;
                case LogType.ChannelDestroyed:
                    return l.ChannelDestroyedId;
                case LogType.ChannelUpdated:
                    return l.ChannelUpdatedId;
                case LogType.UserPresence:
                    return l.LogUserPresenceId;
                case LogType.VoicePresence:
                    return l.LogVoicePresenceId;
                case LogType.VoicePresenceTts:
                    return l.LogVoicePresenceTTSId;
                case LogType.UserMuted:
                    return l.UserMutedId;
                default:
                    return null;
            }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task Log(LogType type)
        {
            var val = _service.Log(ctx.Guild.Id, ctx.Channel.Id, type);

            if (val)
                await ReplyConfirmLocalizedAsync(strs.log(Format.Bold(type.ToString())));
            else
                await ReplyConfirmLocalizedAsync(strs.log_stop(Format.Bold(type.ToString())));
        }
    }
}