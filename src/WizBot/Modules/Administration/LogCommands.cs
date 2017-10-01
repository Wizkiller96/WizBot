using Discord;
using Discord.Commands;
using WizBot.Extensions;
using WizBot.Services;
using WizBot.Services.Database.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using WizBot.Common;
using WizBot.Common.Attributes;
using WizBot.Common.TypeReaders.Models;
using WizBot.Modules.Administration.Services;
using static WizBot.Modules.Administration.Services.LogCommandService;

namespace WizBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        public class LogCommands : WizBotSubmodule<LogCommandService>
        {
            private readonly DbService _db;

            public LogCommands(DbService db)
            {
                _db = db;
            }

            public enum EnableDisable
            {
                Enable,
                Disable
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.Administrator)]
            [OwnerOnly]
            public async Task LogServer(PermissionAction action)
            {
                var channel = (ITextChannel)Context.Channel;
                LogSetting logSetting;
                using (var uow = _db.UnitOfWork)
                {
                    logSetting = uow.GuildConfigs.LogSettingsFor(channel.Guild.Id).LogSetting;
                    _service.GuildLogSettings.AddOrUpdate(channel.Guild.Id, (id) => logSetting, (id, old) => logSetting);
                    logSetting.LogOtherId =
                    logSetting.MessageUpdatedId =
                    logSetting.MessageDeletedId =
                    logSetting.UserJoinedId =
                    logSetting.UserLeftId =
                    logSetting.UserBannedId =
                    logSetting.UserUnbannedId =
                    logSetting.UserUpdatedId =
                    logSetting.ChannelCreatedId =
                    logSetting.ChannelDestroyedId =
                    logSetting.ChannelUpdatedId =
                    logSetting.LogUserPresenceId =
                    logSetting.LogVoicePresenceId =
                    logSetting.UserMutedId =
                    logSetting.LogVoicePresenceTTSId = (action.Value ? channel.Id : (ulong?)null);

                    await uow.CompleteAsync().ConfigureAwait(false);
                }
                if (action.Value)
                    await ReplyConfirmLocalized("log_all").ConfigureAwait(false);
                else
                    await ReplyConfirmLocalized("log_disabled").ConfigureAwait(false);
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.Administrator)]
            [OwnerOnly]
            public async Task LogIgnore()
            {
                var channel = (ITextChannel)Context.Channel;
                int removed;
                using (var uow = _db.UnitOfWork)
                {
                    var config = uow.GuildConfigs.LogSettingsFor(channel.Guild.Id);
                    LogSetting logSetting = _service.GuildLogSettings.GetOrAdd(channel.Guild.Id, (id) => config.LogSetting);
                    removed = logSetting.IgnoredChannels.RemoveWhere(ilc => ilc.ChannelId == channel.Id);
                    config.LogSetting.IgnoredChannels.RemoveWhere(ilc => ilc.ChannelId == channel.Id);
                    if (removed == 0)
                    {
                        var toAdd = new IgnoredLogChannel { ChannelId = channel.Id };
                        logSetting.IgnoredChannels.Add(toAdd);
                        config.LogSetting.IgnoredChannels.Add(toAdd);
                    }
                    await uow.CompleteAsync().ConfigureAwait(false);
                }

                if (removed == 0)
                    await ReplyConfirmLocalized("log_ignore", Format.Bold(channel.Mention + "(" + channel.Id + ")")).ConfigureAwait(false);
                else
                    await ReplyConfirmLocalized("log_not_ignore", Format.Bold(channel.Mention + "(" + channel.Id + ")")).ConfigureAwait(false);
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.Administrator)]
            [OwnerOnly]
            public async Task LogEvents()
            {
                await Context.Channel.SendConfirmAsync(Format.Bold(GetText("log_events")) + "\n" +
                                                       $"```fix\n{string.Join(", ", Enum.GetNames(typeof(LogType)).Cast<string>())}```")
                    .ConfigureAwait(false);
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.Administrator)]
            [OwnerOnly]
            public async Task Log(LogType type)
            {
                var channel = (ITextChannel)Context.Channel;
                ulong? channelId = null;
                using (var uow = _db.UnitOfWork)
                {
                    var logSetting = uow.GuildConfigs.LogSettingsFor(channel.Guild.Id).LogSetting;
                    _service.GuildLogSettings.AddOrUpdate(channel.Guild.Id, (id) => logSetting, (id, old) => logSetting);
                    switch (type)
                    {
                        case LogType.Other:
                            channelId = logSetting.LogOtherId = (logSetting.LogOtherId == null ? channel.Id : default);
                            break;
                        case LogType.MessageUpdated:
                            channelId = logSetting.MessageUpdatedId = (logSetting.MessageUpdatedId == null ? channel.Id : default);
                            break;
                        case LogType.MessageDeleted:
                            channelId = logSetting.MessageDeletedId = (logSetting.MessageDeletedId == null ? channel.Id : default);
                            break;
                        case LogType.UserJoined:
                            channelId = logSetting.UserJoinedId = (logSetting.UserJoinedId == null ? channel.Id : default);
                            break;
                        case LogType.UserLeft:
                            channelId = logSetting.UserLeftId = (logSetting.UserLeftId == null ? channel.Id : default);
                            break;
                        case LogType.UserBanned:
                            channelId = logSetting.UserBannedId = (logSetting.UserBannedId == null ? channel.Id : default);
                            break;
                        case LogType.UserUnbanned:
                            channelId = logSetting.UserUnbannedId = (logSetting.UserUnbannedId == null ? channel.Id : default);
                            break;
                        case LogType.UserUpdated:
                            channelId = logSetting.UserUpdatedId = (logSetting.UserUpdatedId == null ? channel.Id : default);
                            break;
                        case LogType.UserMuted:
                            channelId = logSetting.UserMutedId = (logSetting.UserMutedId == null ? channel.Id : default);
                            break;
                        case LogType.ChannelCreated:
                            channelId = logSetting.ChannelCreatedId = (logSetting.ChannelCreatedId == null ? channel.Id : default);
                            break;
                        case LogType.ChannelDestroyed:
                            channelId = logSetting.ChannelDestroyedId = (logSetting.ChannelDestroyedId == null ? channel.Id : default);
                            break;
                        case LogType.ChannelUpdated:
                            channelId = logSetting.ChannelUpdatedId = (logSetting.ChannelUpdatedId == null ? channel.Id : default);
                            break;
                        case LogType.UserPresence:
                            channelId = logSetting.LogUserPresenceId = (logSetting.LogUserPresenceId == null ? channel.Id : default);
                            break;
                        case LogType.VoicePresence:
                            channelId = logSetting.LogVoicePresenceId = (logSetting.LogVoicePresenceId == null ? channel.Id : default);
                            break;
                        case LogType.VoicePresenceTTS:
                            channelId = logSetting.LogVoicePresenceTTSId = (logSetting.LogVoicePresenceTTSId == null ? channel.Id : default);
                            break;
                    }

                    await uow.CompleteAsync().ConfigureAwait(false);
                }

                if (channelId != null)
                    await ReplyConfirmLocalized("log", Format.Bold(type.ToString())).ConfigureAwait(false);
                else
                    await ReplyConfirmLocalized("log_stop", Format.Bold(type.ToString())).ConfigureAwait(false);
            }
        }
    }
}