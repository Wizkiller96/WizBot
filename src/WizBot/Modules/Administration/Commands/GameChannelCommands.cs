using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using WizBot.Attributes;
using WizBot.Services;
using WizBot.Services.Database;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using NLog;
using WizBot.Extensions;

namespace WizBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        public class GameChannelCommands : WizBotSubmodule
        {
            //private static readonly Timer _t;

            private static readonly ConcurrentHashSet<ulong> gameVoiceChannels = new ConcurrentHashSet<ulong>();

            private static new readonly Logger _log;

            static GameChannelCommands()
            {
                //_t = new Timer(_ => {

                //}, null, );

                _log = LogManager.GetCurrentClassLogger();

                gameVoiceChannels = new ConcurrentHashSet<ulong>(
                    WizBot.AllGuildConfigs.Where(gc => gc.GameVoiceChannel != null)
                                             .Select(gc => gc.GameVoiceChannel.Value));

                WizBot.Client.UserVoiceStateUpdated += Client_UserVoiceStateUpdated;

            }

            private static Task Client_UserVoiceStateUpdated(SocketUser usr, SocketVoiceState oldState, SocketVoiceState newState)
            {
                var _ = Task.Run(async () =>
                {
                    try
                    {
                        var gUser = usr as SocketGuildUser;
                        if (gUser == null)
                            return;

                        var game = gUser.Game?.Name.TrimTo(50).ToLowerInvariant();

                        if (oldState.VoiceChannel == newState.VoiceChannel ||
                            newState.VoiceChannel == null)
                            return;

                        if (!gameVoiceChannels.Contains(newState.VoiceChannel.Id) ||
                            string.IsNullOrWhiteSpace(game))
                            return;

                        var vch = gUser.Guild.VoiceChannels
                            .FirstOrDefault(x => x.Name.ToLowerInvariant() == game);

                        if (vch == null)
                            return;

                        await Task.Delay(1000).ConfigureAwait(false);
                        await gUser.ModifyAsync(gu => gu.Channel = vch).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _log.Warn(ex);
                    }
                });

                return Task.CompletedTask;
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.Administrator)]
            [RequireBotPermission(GuildPermission.MoveMembers)]
            public async Task GameVoiceChannel()
            {
                var vch = ((IGuildUser)Context.User).VoiceChannel;

                if (vch == null)
                {
                    await ReplyErrorLocalized("not_in_voice").ConfigureAwait(false);
                    return;
                }
                ulong? id;
                using (var uow = DbHandler.UnitOfWork())
                {
                    var gc = uow.GuildConfigs.For(Context.Guild.Id, set => set);

                    if (gc.GameVoiceChannel == vch.Id)
                    {
                        gameVoiceChannels.TryRemove(vch.Id);
                        id = gc.GameVoiceChannel = null;
                    }
                    else
                    {
                        if(gc.GameVoiceChannel != null)
                            gameVoiceChannels.TryRemove(gc.GameVoiceChannel.Value);
                        gameVoiceChannels.Add(vch.Id);
                        id = gc.GameVoiceChannel = vch.Id;
                    }

                    uow.Complete();
                }

                if (id == null)
                {
                    await ReplyConfirmLocalized("gvc_disabled").ConfigureAwait(false);
                }
                else
                {
                    gameVoiceChannels.Add(vch.Id);
                    await ReplyConfirmLocalized("gvc_enabled", Format.Bold(vch.Name)).ConfigureAwait(false);
                }
            }
        }
    }
}
