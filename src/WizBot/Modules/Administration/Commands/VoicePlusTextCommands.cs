﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using WizBot.Attributes;
using WizBot.Extensions;
using WizBot.Services;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WizBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        public class VoicePlusTextCommands
        {
            private static Regex channelNameRegex = new Regex(@"[^a-zA-Z0-9 -]", RegexOptions.Compiled);
            
            private static ConcurrentHashSet<ulong> voicePlusTextCache { get; }
            static VoicePlusTextCommands()
            {
                using (var uow = DbHandler.UnitOfWork())
                {
                    voicePlusTextCache = new ConcurrentHashSet<ulong>(WizBot.AllGuildConfigs.Where(g => g.VoicePlusTextEnabled).Select(g => g.GuildId));
                }
                WizBot.Client.UserVoiceStateUpdated += UserUpdatedEventHandler;
            }

            private static Task UserUpdatedEventHandler(IUser iuser, IVoiceState before, IVoiceState after)
            {
                var user = (iuser as IGuildUser);
                var guild = user?.Guild;

                if (guild == null)
                    return Task.CompletedTask;
                var task = Task.Run(async () =>
                {
                    try
                    {
                        var botUserPerms = guild.GetCurrentUser().GuildPermissions;
                    
                        if (before.VoiceChannel == after.VoiceChannel) return;
                        
                        if (!voicePlusTextCache.Contains(guild.Id))
                            return;

                        if (!botUserPerms.ManageChannels || !botUserPerms.ManageRoles)
                        {
                            try
                            {
                                await (await guild.GetOwnerAsync()).SendErrorAsync(
                                    "⚠️ I don't have **manage server** and/or **manage channels** permission," +
                                    $" so I cannot run `voice+text` on **{guild.Name}** server.").ConfigureAwait(false);
                            }
                            catch { }
                            using (var uow = DbHandler.UnitOfWork())
                            {
                                uow.GuildConfigs.For(guild.Id, set => set).VoicePlusTextEnabled = false;
                                voicePlusTextCache.TryRemove(guild.Id);
                                await uow.CompleteAsync().ConfigureAwait(false);
                            }
                            return;
                        }


                        var beforeVch = before.VoiceChannel;
                        if (beforeVch != null)
                        {
                            var textChannel = guild.GetTextChannels().Where(t => t.Name == GetChannelName(beforeVch.Name).ToLowerInvariant()).FirstOrDefault();
                            if (textChannel != null)
                                await textChannel.AddPermissionOverwriteAsync(user,
                                    new OverwritePermissions(readMessages: PermValue.Deny,
                                                       sendMessages: PermValue.Deny)).ConfigureAwait(false);
                        }
                        var afterVch = after.VoiceChannel;
                        if (afterVch != null && guild.AFKChannelId != afterVch.Id)
                        {
                            var textChannel = guild.GetTextChannels()
                                                        .Where(t => t.Name ==  GetChannelName(afterVch.Name).ToLowerInvariant())
                                                        .FirstOrDefault();
                            if (textChannel == null)
                            {
                                textChannel = (await guild.CreateTextChannelAsync(GetChannelName(afterVch.Name).ToLowerInvariant()).ConfigureAwait(false));
                                await textChannel.AddPermissionOverwriteAsync(guild.EveryoneRole,
                                    new OverwritePermissions(readMessages: PermValue.Deny,
                                                       sendMessages: PermValue.Deny)).ConfigureAwait(false);
                            }
                            await textChannel.AddPermissionOverwriteAsync(user,
                                new OverwritePermissions(readMessages: PermValue.Allow,
                                                        sendMessages: PermValue.Allow)).ConfigureAwait(false);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                });
                return Task.CompletedTask;
            }

            private static string GetChannelName(string voiceName) =>
                channelNameRegex.Replace(voiceName, "").Trim().Replace(" ", "-").TrimTo(90, true) + "-voice";

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequirePermission(GuildPermission.ManageRoles)]
            [RequirePermission(GuildPermission.ManageChannels)]
            public async Task VoicePlusText(IUserMessage msg)
            {
                var channel = (ITextChannel)msg.Channel;
                var guild = channel.Guild;

                var botUser = await guild.GetCurrentUserAsync().ConfigureAwait(false);
                if (!botUser.GuildPermissions.ManageRoles || !botUser.GuildPermissions.ManageChannels)
                {
                    await channel.SendErrorAsync("I require atleast **manage roles** and **manage channels permissions** to enable this feature. `(preffered Administration permission)`");
                    return;
                }

                if (!botUser.GuildPermissions.Administrator)
                {
                    try
                    {
                        await channel.SendErrorAsync("⚠️ You are enabling this feature and **I do not have ADMINISTRATOR permissions**. " +
                      "`This may cause some issues, and you will have to clean up text channels yourself afterwards.`");
                    }
                    catch { }
                }
                try
                {
                    bool isEnabled;
                    using (var uow = DbHandler.UnitOfWork())
                    {
                        var conf = uow.GuildConfigs.For(guild.Id, set => set);
                        isEnabled = conf.VoicePlusTextEnabled = !conf.VoicePlusTextEnabled;
                        await uow.CompleteAsync().ConfigureAwait(false);
                    }
                    if (!isEnabled)
                    {
                        voicePlusTextCache.TryRemove(guild.Id);
                        foreach (var textChannel in guild.GetTextChannels().Where(c => c.Name.EndsWith("-voice")))
                        {
                            try { await textChannel.DeleteAsync().ConfigureAwait(false); } catch { }
                        }
                        await channel.SendConfirmAsync("ℹ️ Successfuly **removed** voice + text feature.").ConfigureAwait(false);
                        return;
                    }
                    voicePlusTextCache.Add(guild.Id);
                    await channel.SendConfirmAsync("🆗 Successfuly **enabled** voice + text feature.").ConfigureAwait(false);

                }
                catch (Exception ex)
                {
                    await channel.SendErrorAsync(ex.ToString()).ConfigureAwait(false);
                }
            }
            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequirePermission(GuildPermission.ManageChannels)]
            [RequirePermission(GuildPermission.ManageRoles)]
            public async Task CleanVPlusT(IUserMessage msg)
            {
                var channel = (ITextChannel)msg.Channel;
                var guild = channel.Guild;
                var botUser = await guild.GetCurrentUserAsync().ConfigureAwait(false);
                if (!botUser.GuildPermissions.Administrator)
                {
                    await channel.SendErrorAsync("I need **Administrator permission** to do that.").ConfigureAwait(false);
                    return;
                }

                var allTxtChannels = guild.GetTextChannels().Where(c => c.Name.EndsWith("-voice"));
                var validTxtChannelNames = guild.GetVoiceChannels().Select(c => GetChannelName(c.Name).ToLowerInvariant());

                var invalidTxtChannels = allTxtChannels.Where(c => !validTxtChannelNames.Contains(c.Name));

                foreach (var c in invalidTxtChannels)
                {
                    try { await c.DeleteAsync().ConfigureAwait(false); } catch { }
                    await Task.Delay(500);
                }

                await channel.SendConfirmAsync("Cleaned v+t.").ConfigureAwait(false);
            }
        }
    }
}
