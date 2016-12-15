﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using WizBot.Attributes;
using WizBot.Extensions;
using WizBot.Services;
using WizBot.Services.Database.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WizBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        public class MuteCommands
        {
            private static ConcurrentDictionary<ulong, string> GuildMuteRoles { get; } = new ConcurrentDictionary<ulong, string>();

            private static ConcurrentDictionary<ulong, ConcurrentHashSet<ulong>> MutedUsers { get; } = new ConcurrentDictionary<ulong, ConcurrentHashSet<ulong>>();

            public static event Func<IGuildUser, MuteType, Task> UserMuted = delegate { return Task.CompletedTask; };
            public static event Func<IGuildUser, MuteType, Task> UserUnmuted = delegate { return Task.CompletedTask; };


            public enum MuteType {
                Voice,
                Chat,
                All
            }

            static MuteCommands() {
                using (var uow = DbHandler.UnitOfWork())
                {
                    var configs = WizBot.AllGuildConfigs;
                    GuildMuteRoles = new ConcurrentDictionary<ulong, string>(configs
                            .Where(c => !string.IsNullOrWhiteSpace(c.MuteRoleName))
                            .ToDictionary(c => c.GuildId, c => c.MuteRoleName));

                    MutedUsers = new ConcurrentDictionary<ulong, ConcurrentHashSet<ulong>>(configs.ToDictionary(
                        k => k.GuildId,
                        v => new ConcurrentHashSet<ulong>(v.MutedUsers.Select(m => m.UserId))
                    ));
                }

                WizBot.Client.UserJoined += Client_UserJoined;
            }

            private static async Task Client_UserJoined(IGuildUser usr)
            {
                ConcurrentHashSet<ulong> muted;
                MutedUsers.TryGetValue(usr.Guild.Id, out muted);

                if (muted == null || !muted.Contains(usr.Id))
                    return;
                else
                    await Mute(usr).ConfigureAwait(false);
                    
            }

            public static async Task Mute(IGuildUser usr)
            {
                await usr.ModifyAsync(x => x.Mute = true).ConfigureAwait(false);
                await usr.AddRolesAsync(await GetMuteRole(usr.Guild)).ConfigureAwait(false);
                using (var uow = DbHandler.UnitOfWork())
                {
                    var config = uow.GuildConfigs.For(usr.Guild.Id, set => set.Include(gc => gc.MutedUsers));
                    config.MutedUsers.Add(new MutedUserId()
                    {
                        UserId = usr.Id
                    });
                    ConcurrentHashSet<ulong> muted;
                    if (MutedUsers.TryGetValue(usr.Guild.Id, out muted))
                        muted.Add(usr.Id);
                    
                    await uow.CompleteAsync().ConfigureAwait(false);
                }
                await UserMuted(usr, MuteType.All).ConfigureAwait(false);
            }

            public static async Task Unmute(IGuildUser usr)
            {
                await usr.ModifyAsync(x => x.Mute = false).ConfigureAwait(false);
                await usr.RemoveRolesAsync(await GetMuteRole(usr.Guild)).ConfigureAwait(false);
                using (var uow = DbHandler.UnitOfWork())
                {
                    var config = uow.GuildConfigs.For(usr.Guild.Id, set => set.Include(gc => gc.MutedUsers));
                    config.MutedUsers.Remove(new MutedUserId()
                    {
                        UserId = usr.Id
                    });
                    ConcurrentHashSet<ulong> muted;
                    if (MutedUsers.TryGetValue(usr.Guild.Id, out muted))
                        muted.TryRemove(usr.Id);
                    await uow.CompleteAsync().ConfigureAwait(false);
                }
                await UserUnmuted(usr, MuteType.All).ConfigureAwait(false);
            }

            public static async Task<IRole> GetMuteRole(IGuild guild)
            {
                const string defaultMuteRoleName = "WizBot-Mute";

                var muteRoleName = GuildMuteRoles.GetOrAdd(guild.Id, defaultMuteRoleName);

                var muteRole = guild.Roles.FirstOrDefault(r => r.Name == muteRoleName);
                if (muteRole == null)
                {

                    //if it doesn't exist, create it 
                    try { muteRole = await guild.CreateRoleAsync(muteRoleName, GuildPermissions.None).ConfigureAwait(false); }
                    catch
                    {
                        //if creations fails,  maybe the name is not correct, find default one, if doesn't work, create default one
                        muteRole = guild.Roles.FirstOrDefault(r => r.Name == muteRoleName) ??
                            await guild.CreateRoleAsync(defaultMuteRoleName, GuildPermissions.None).ConfigureAwait(false);
                    }

                    foreach (var toOverwrite in guild.GetTextChannels())
                    {
                        try
                        {
                            await toOverwrite.AddPermissionOverwriteAsync(muteRole, new OverwritePermissions(sendMessages: PermValue.Deny, attachFiles: PermValue.Deny))
                                    .ConfigureAwait(false);
                        }
                        catch { }
                        await Task.Delay(200).ConfigureAwait(false);
                    }
                }
                return muteRole;
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequirePermission(GuildPermission.ManageRoles)]
            [Priority(1)]
            public async Task SetMuteRole(IUserMessage imsg, [Remainder] string name)
            {
                var channel = (ITextChannel)imsg.Channel;
                name = name.Trim();
                if (string.IsNullOrWhiteSpace(name))
                    return;

                using (var uow = DbHandler.UnitOfWork())
                {
                    var config = uow.GuildConfigs.For(channel.Guild.Id, set => set);
                    config.MuteRoleName = name;
                    GuildMuteRoles.AddOrUpdate(channel.Guild.Id, name, (id, old) => name);
                    await uow.CompleteAsync().ConfigureAwait(false);
                }
                await channel.SendConfirmAsync("☑️ **New mute role set.**").ConfigureAwait(false);
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequirePermission(GuildPermission.ManageRoles)]
            [Priority(0)]
            public Task SetMuteRole(IUserMessage imsg, [Remainder] IRole role)
                => SetMuteRole(imsg, role.Name);

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequirePermission(GuildPermission.ManageRoles)]
            [RequirePermission(GuildPermission.MuteMembers)]
            public async Task Mute(IUserMessage umsg, IGuildUser user)
            {
                var channel = (ITextChannel)umsg.Channel;

                try
                {
                    await Mute(user).ConfigureAwait(false);                    
                    await channel.SendConfirmAsync($"🔇 **{user}** has been **muted** from text and voice chat.").ConfigureAwait(false);
                }
                catch
                {
                    await channel.SendErrorAsync("⚠️ I most likely don't have the permission necessary for that.").ConfigureAwait(false);
                }
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequirePermission(GuildPermission.ManageRoles)]
            [RequirePermission(GuildPermission.MuteMembers)]
            public async Task Unmute(IUserMessage umsg, IGuildUser user)
            {
                var channel = (ITextChannel)umsg.Channel;

                try
                {
                    await Unmute(user).ConfigureAwait(false);
                    await channel.SendConfirmAsync($"🔉 **{user}** has been **unmuted** from text and voice chat.").ConfigureAwait(false);
                }
                catch
                {
                    await channel.SendErrorAsync("⚠️ I most likely don't have the permission necessary for that.").ConfigureAwait(false);
                }
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequirePermission(GuildPermission.ManageRoles)]
            public async Task ChatMute(IUserMessage umsg, IGuildUser user)
            {
                var channel = (ITextChannel)umsg.Channel;

                try
                {
                    await user.AddRolesAsync(await GetMuteRole(channel.Guild).ConfigureAwait(false)).ConfigureAwait(false);
                    await UserMuted(user, MuteType.Chat).ConfigureAwait(false);
                    await channel.SendConfirmAsync($"✏️🚫 **{user}** has been **muted** from chatting.").ConfigureAwait(false);
                }
                catch
                {
                    await channel.SendErrorAsync("⚠️ I most likely don't have the permission necessary for that.").ConfigureAwait(false);
                }
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequirePermission(GuildPermission.ManageRoles)]
            public async Task ChatUnmute(IUserMessage umsg, IGuildUser user)
            {
                var channel = (ITextChannel)umsg.Channel;

                try
                {
                    await user.RemoveRolesAsync(await GetMuteRole(channel.Guild).ConfigureAwait(false)).ConfigureAwait(false);
                    await UserUnmuted(user, MuteType.Chat).ConfigureAwait(false);
                    await channel.SendConfirmAsync($"✏️✅ **{user}** has been **unmuted** from chatting.").ConfigureAwait(false);
                }
                catch
                {
                    await channel.SendErrorAsync("⚠️ I most likely don't have the permission necessary for that.").ConfigureAwait(false);
                }
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequirePermission(GuildPermission.MuteMembers)]
            public async Task VoiceMute(IUserMessage umsg, IGuildUser user)
            {
                var channel = (ITextChannel)umsg.Channel;

                try
                {
                    await user.ModifyAsync(usr => usr.Mute = true).ConfigureAwait(false);
                    await UserMuted(user, MuteType.Voice).ConfigureAwait(false);
                    await channel.SendConfirmAsync($"🎙🚫 **{user}** has been **voice muted**.").ConfigureAwait(false);
                }
                catch
                {
                    await channel.SendErrorAsync("⚠️ I most likely don't have the permission necessary for that.").ConfigureAwait(false);
                }
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequirePermission(GuildPermission.MuteMembers)]
            public async Task VoiceUnmute(IUserMessage umsg, IGuildUser user)
            {
                var channel = (ITextChannel)umsg.Channel;
                try
                {
                    await user.ModifyAsync(usr => usr.Mute = false).ConfigureAwait(false);
                    await UserUnmuted(user, MuteType.Voice).ConfigureAwait(false);
                    await channel.SendConfirmAsync($"🎙✅ **{user}** has been **voice unmuted**.").ConfigureAwait(false);
                }
                catch
                {
                    await channel.SendErrorAsync("⚠️ I most likely don't have the permission necessary for that.").ConfigureAwait(false);
                }
            }
        }
    }
}
