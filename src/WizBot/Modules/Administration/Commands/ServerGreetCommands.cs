﻿using Discord;
using Discord.Commands;
using WizBot.Attributes;
using WizBot.Extensions;
using WizBot.Services;
using WizBot.Services.Database;
using WizBot.Services.Database.Models;
using NLog;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace WizBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        public class ServerGreetCommands
        {
            private static Logger _log { get; }

            static ServerGreetCommands()
            {
                WizBot.Client.UserJoined += UserJoined;
                WizBot.Client.UserLeft += UserLeft;
                _log = LogManager.GetCurrentClassLogger();
            }

            private static Task UserLeft(IGuildUser user)
            {
                var leftTask = Task.Run(async () =>
                {
                    try
                    {
                        GuildConfig conf;
                        using (var uow = DbHandler.UnitOfWork())
                        {
                            conf = uow.GuildConfigs.For(user.Guild.Id, set => set);
                        }

                        if (!conf.SendChannelByeMessage) return;
                        var channel = (await user.Guild.GetTextChannelsAsync()).SingleOrDefault(c => c.Id == conf.ByeMessageChannelId);

                        if (channel == null) //maybe warn the server owner that the channel is missing
                            return;

                        var msg = conf.ChannelByeMessageText.Replace("%user%", user.Username).Replace("%id%", user.Id.ToString()).Replace("%server%", user.Guild.Name);
                        if (string.IsNullOrWhiteSpace(msg))
                            return;
                        try
                        {
                            var toDelete = await channel.SendMessageAsync(msg.SanitizeMentions()).ConfigureAwait(false);
                            if (conf.AutoDeleteByeMessagesTimer > 0)
                            {
                                var t = Task.Run(async () =>
                                {
                                    await Task.Delay(conf.AutoDeleteByeMessagesTimer * 1000).ConfigureAwait(false); // 5 minutes
                                    try { await toDelete.DeleteAsync().ConfigureAwait(false); } catch { }
                                });
                            }
                        }
                        catch (Exception ex) { _log.Warn(ex); }
                    }
                    catch { }
                });
                return Task.CompletedTask;
            }

            private static Task UserJoined(IGuildUser user)
            {
                var joinedTask = Task.Run(async () =>
                {
                    try
                    {
                        GuildConfig conf;
                        using (var uow = DbHandler.UnitOfWork())
                        {
                            conf = uow.GuildConfigs.For(user.Guild.Id, set => set);
                        }

                        if (conf.SendChannelGreetMessage)
                        {
                            var channel = (await user.Guild.GetTextChannelsAsync()).SingleOrDefault(c => c.Id == conf.GreetMessageChannelId);
                            if (channel != null) //maybe warn the server owner that the channel is missing
                            {
                                var msg = conf.ChannelGreetMessageText.Replace("%user%", user.Mention).Replace("%id%", user.Id.ToString()).Replace("%server%", user.Guild.Name);
                                if (!string.IsNullOrWhiteSpace(msg))
                                {
                                    try
                                    {
                                        var toDelete = await channel.SendMessageAsync(msg.SanitizeMentions()).ConfigureAwait(false);
                                        if (conf.AutoDeleteGreetMessagesTimer > 0)
                                        {
                                            var t = Task.Run(async () =>
                                            {
                                                await Task.Delay(conf.AutoDeleteGreetMessagesTimer * 1000).ConfigureAwait(false); // 5 minutes
                                                try { await toDelete.DeleteAsync().ConfigureAwait(false); } catch { }
                                            });
                                        }
                                    }
                                    catch (Exception ex) { _log.Warn(ex); }
                                }
                            }
                        }

                        if (conf.SendDmGreetMessage)
                        {
                            var channel = await user.CreateDMChannelAsync();

                            if (channel != null)
                            {
                                var msg = conf.DmGreetMessageText.Replace("%user%", user.Username).Replace("%server%", user.Guild.Name);
                                if (!string.IsNullOrWhiteSpace(msg))
                                {
                                    await channel.SendConfirmAsync(msg).ConfigureAwait(false);
                                }
                            }
                        }
                    }
                    catch { }
                });
                return Task.CompletedTask;
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequirePermission(GuildPermission.ManageGuild)]
            public async Task GreetDel(IUserMessage umsg, int timer = 30)
            {
                var channel = (ITextChannel)umsg.Channel;
                if (timer < 0 || timer > 600)
                    return;

                await ServerGreetCommands.SetGreetDel(channel.Guild.Id, timer).ConfigureAwait(false);

                if (timer > 0)
                    await channel.SendConfirmAsync($"🆗 Greet messages **will be deleted** after `{timer} seconds`.").ConfigureAwait(false);
                else
                    await channel.SendConfirmAsync("ℹ️ Automatic deletion of greet messages has been **disabled**.").ConfigureAwait(false);
            }

            private static async Task SetGreetDel(ulong id, int timer)
            {
                if (timer < 0 || timer > 600)
                    return;
                
                using (var uow = DbHandler.UnitOfWork())
                {
                    var conf = uow.GuildConfigs.For(id, set => set);
                    conf.AutoDeleteGreetMessagesTimer = timer;

                    await uow.CompleteAsync().ConfigureAwait(false);
                }
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequirePermission(GuildPermission.ManageGuild)]
            public async Task Greet(IUserMessage umsg)
            {
                var channel = (ITextChannel)umsg.Channel;

                var enabled = await ServerGreetCommands.SetGreet(channel.Guild.Id, channel.Id).ConfigureAwait(false);

                if (enabled)
                    await channel.SendConfirmAsync("✅ Greeting messages **enabled** on this channel.").ConfigureAwait(false);
                else
                    await channel.SendConfirmAsync("ℹ️ Greeting messages **disabled**.").ConfigureAwait(false);
            }

            private static async Task<bool> SetGreet(ulong guildId, ulong channelId, bool? value = null)
            {
                bool enabled;
                using (var uow = DbHandler.UnitOfWork())
                {
                    var conf = uow.GuildConfigs.For(guildId, set => set);
                    enabled = conf.SendChannelGreetMessage = value ?? !conf.SendChannelGreetMessage;
                    conf.GreetMessageChannelId = channelId;

                    await uow.CompleteAsync().ConfigureAwait(false);
                }
                return enabled;
            }
            
            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequirePermission(GuildPermission.ManageGuild)]
            public async Task GreetMsg(IUserMessage umsg, [Remainder] string text = null)
            {
                var channel = (ITextChannel)umsg.Channel;

                if (string.IsNullOrWhiteSpace(text))
                {
                    string channelGreetMessageText;
                    using (var uow = DbHandler.UnitOfWork())
                    {
                        channelGreetMessageText = uow.GuildConfigs.For(channel.Guild.Id, set => set).ChannelGreetMessageText;
                    }
                    await channel.SendConfirmAsync("Current greet message: ", channelGreetMessageText?.SanitizeMentions());
                    return;
                }

                var sendGreetEnabled = ServerGreetCommands.SetGreetMessage(channel.Guild.Id, ref text);

                await channel.SendConfirmAsync("🆗 New greet message **set**.").ConfigureAwait(false);
                if (!sendGreetEnabled)
                    await channel.SendConfirmAsync("ℹ️ Enable greet messsages by typing `.greet`").ConfigureAwait(false);
            }

            public static bool SetGreetMessage(ulong guildId, ref string message)
            {
                message = message?.SanitizeMentions();

                if (string.IsNullOrWhiteSpace(message))
                    throw new ArgumentNullException(nameof(message));

                bool greetMsgEnabled;
                using (var uow = DbHandler.UnitOfWork())
                {
                    var conf = uow.GuildConfigs.For(guildId, set => set);
                    conf.ChannelGreetMessageText = message;
                    greetMsgEnabled = conf.SendChannelGreetMessage;

                    uow.Complete();
                }
                return greetMsgEnabled;
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequirePermission(GuildPermission.ManageGuild)]
            public async Task GreetDm(IUserMessage umsg)
            {
                var channel = (ITextChannel)umsg.Channel;

                var enabled = await ServerGreetCommands.SetGreetDm(channel.Guild.Id).ConfigureAwait(false);

                if (enabled)
                    await channel.SendConfirmAsync("🆗 DM Greet announcements **enabled**.").ConfigureAwait(false);
                else
                    await channel.SendConfirmAsync("ℹ️ Greet announcements **disabled**.").ConfigureAwait(false);
            }

            private static async Task<bool> SetGreetDm(ulong guildId, bool? value = null)
            {
                bool enabled;
                using (var uow = DbHandler.UnitOfWork())
                {
                    var conf = uow.GuildConfigs.For(guildId, set => set);
                    enabled = conf.SendDmGreetMessage = value ?? !conf.SendDmGreetMessage;

                    await uow.CompleteAsync().ConfigureAwait(false);
                }
                return enabled;
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequirePermission(GuildPermission.ManageGuild)]
            public async Task GreetDmMsg(IUserMessage umsg, [Remainder] string text = null)
            {
                var channel = (ITextChannel)umsg.Channel;

                if (string.IsNullOrWhiteSpace(text))
                {
                    GuildConfig config;
                    using (var uow = DbHandler.UnitOfWork())
                    {
                        config = uow.GuildConfigs.For(channel.Guild.Id);
                    }
                    await channel.SendConfirmAsync("ℹ️ Current **DM greet** message: `" + config.DmGreetMessageText?.SanitizeMentions() + "`");
                    return;
                }

                var sendGreetEnabled = ServerGreetCommands.SetGreetDmMessage(channel.Guild.Id, ref text);

                await channel.SendConfirmAsync("🆗 New DM greet message **set**.").ConfigureAwait(false);
                if (!sendGreetEnabled)
                    await channel.SendConfirmAsync($"ℹ️ Enable DM greet messsages by typing `{WizBot.ModulePrefixes[typeof(Administration).Name]}greetdm`").ConfigureAwait(false);
            }

            public static bool SetGreetDmMessage(ulong guildId, ref string message)
            {
                message = message?.SanitizeMentions();

                if (string.IsNullOrWhiteSpace(message))
                    throw new ArgumentNullException(nameof(message));

                bool greetMsgEnabled;
                using (var uow = DbHandler.UnitOfWork())
                {
                    var conf = uow.GuildConfigs.For(guildId);
                    conf.DmGreetMessageText = message;
                    greetMsgEnabled = conf.SendDmGreetMessage;

                    uow.Complete();
                }
                return greetMsgEnabled;
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequirePermission(GuildPermission.ManageGuild)]
            public async Task Bye(IUserMessage umsg)
            {
                var channel = (ITextChannel)umsg.Channel;

                var enabled = await ServerGreetCommands.SetBye(channel.Guild.Id, channel.Id).ConfigureAwait(false);

                if (enabled)
                    await channel.SendConfirmAsync("✅ Bye announcements **enabled** on this channel.").ConfigureAwait(false);
                else
                    await channel.SendConfirmAsync("ℹ️ Bye announcements **disabled**.").ConfigureAwait(false);
            }

            private static async Task<bool> SetBye(ulong guildId, ulong channelId, bool? value = null)
            {
                bool enabled;
                using (var uow = DbHandler.UnitOfWork())
                {
                    var conf = uow.GuildConfigs.For(guildId, set => set);
                    enabled = conf.SendChannelByeMessage = value ?? !conf.SendChannelByeMessage;
                    conf.ByeMessageChannelId = channelId;

                    await uow.CompleteAsync();
                }
                return enabled;
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequirePermission(GuildPermission.ManageGuild)]
            public async Task ByeMsg(IUserMessage umsg, [Remainder] string text = null)
            {
                var channel = (ITextChannel)umsg.Channel;

                if (string.IsNullOrWhiteSpace(text))
                {
                    string byeMessageText;
                    using (var uow = DbHandler.UnitOfWork())
                    {
                        byeMessageText = uow.GuildConfigs.For(channel.Guild.Id, set => set).ChannelByeMessageText;
                    }
                    await channel.SendConfirmAsync("ℹ️ Current **bye** message: `" + byeMessageText?.SanitizeMentions() + "`");
                    return;
                }

                var sendByeEnabled = ServerGreetCommands.SetByeMessage(channel.Guild.Id, ref text);

                await channel.SendConfirmAsync("🆗 New bye message **set**.").ConfigureAwait(false);
                if (!sendByeEnabled)
                    await channel.SendConfirmAsync($"ℹ️ Enable bye messsages by typing `{WizBot.ModulePrefixes[typeof(Administration).Name]}bye`").ConfigureAwait(false);
            }
            
            public static bool SetByeMessage(ulong guildId, ref string message)
            {
                message = message?.SanitizeMentions();

                if (string.IsNullOrWhiteSpace(message))
                    throw new ArgumentNullException(nameof(message));

                bool byeMsgEnabled;
                using (var uow = DbHandler.UnitOfWork())
                {
                    var conf = uow.GuildConfigs.For(guildId, set => set);
                    conf.ChannelByeMessageText = message;
                    byeMsgEnabled = conf.SendChannelByeMessage;

                    uow.Complete();
                }
                return byeMsgEnabled;
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequirePermission(GuildPermission.ManageGuild)]
            public async Task ByeDel(IUserMessage umsg, int timer = 30)
            {
                var channel = (ITextChannel)umsg.Channel;

                await ServerGreetCommands.SetByeDel(channel.Guild.Id, timer).ConfigureAwait(false);

                if (timer > 0)
                    await channel.SendConfirmAsync($"🆗 Bye messages **will be deleted** after `{timer} seconds`.").ConfigureAwait(false);
                else
                    await channel.SendConfirmAsync("ℹ️ Automatic deletion of bye messages has been **disabled**.").ConfigureAwait(false);
            }

            private static async Task SetByeDel(ulong id, int timer)
            {
                if (timer < 0 || timer > 600)
                    return;

                using (var uow = DbHandler.UnitOfWork())
                {
                    var conf = uow.GuildConfigs.For(id, set => set);
                    conf.AutoDeleteByeMessagesTimer = timer;

                    await uow.CompleteAsync().ConfigureAwait(false);
                }
            }

        }
    }
}
