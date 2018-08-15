﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using WizBot.Common.ModuleBehaviors;
using WizBot.Extensions;
using WizBot.Modules.Administration.Common;
using WizBot.Core.Services;
using WizBot.Core.Services.Database.Models;
using NLog;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using WizBot.Core.Common;
using CommandLine;

namespace WizBot.Modules.Administration.Services
{
    public class SlowmodeService : IEarlyBehavior, INService
    {
        public class Options : IWizBotCommandOptions
        {
            [Option('m', "message-count", Required = false, Default = 1, HelpText = "Number of messages user can send.")]
            public int MessageCount { get; set; } = 1;

            [Option('s', "seconds", Required = false, Default = 5, HelpText = "Interval in which the user can send the specified number of messages.")]
            public int PerSec { get; set; } = 5;

            public void NormalizeOptions()
            {
                if (MessageCount < 1 || MessageCount > 100)
                    MessageCount = 1;
                if (PerSec < 1 || PerSec > 3600)
                    PerSec = 5;
            }
        }

        public class SlowmodeIgnores
        {
            public HashSet<ulong> IgnoredRoles { get; set; }
            public HashSet<ulong> IgnoredUsers { get; set; }
        }

        private readonly DbService _db;
        private readonly Logger _log;
        private readonly DiscordSocketClient _client;

        private readonly Timer _deleteTimer;
        // channels with a queue of messages scheduled for deletion
        public ConcurrentDictionary<(ulong GuildId, ulong ChannelId), ConcurrentQueue<ulong>> SlowmodeToDelete { get; }
            = new ConcurrentDictionary<(ulong, ulong), ConcurrentQueue<ulong>>();

        // ignored roles and users
        private readonly ConcurrentDictionary<ulong, SlowmodeIgnores> _ignores
            = new ConcurrentDictionary<ulong, SlowmodeIgnores>();

        // where the slowmode is actually running
        public ConcurrentDictionary<ulong, Slowmoder> SlowmodeChannels { get; }
            = new ConcurrentDictionary<ulong, Slowmoder>();

        public int Priority => -49;
        public ModuleBehaviorType BehaviorType => ModuleBehaviorType.Blocker;

        public SlowmodeService(DiscordSocketClient client, WizBot bot, DbService db)
        {
            _log = LogManager.GetCurrentClassLogger();
            _client = client;
            _db = db;

            _ignores = new ConcurrentDictionary<ulong, SlowmodeIgnores>(bot.AllGuildConfigs
                .ToDictionary(x => x.GuildId,
                    x => GetIgnores(x)));

            _deleteTimer = new Timer(TimerFunc, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(1));

            _client.LeftGuild += _client_LeftGuild;
            bot.JoinedGuild += Bot_JoinedGuild;
        }

        private SlowmodeIgnores GetIgnores(GuildConfig gc)
        {
            return new SlowmodeIgnores()
            {
                IgnoredRoles = new HashSet<ulong>(gc.SlowmodeIgnoredRoles.Select(y => y.RoleId)),
                IgnoredUsers = new HashSet<ulong>(gc.SlowmodeIgnoredUsers.Select(y => y.UserId)),
            };
        }

        private Task Bot_JoinedGuild(GuildConfig gc)
        {
            _ignores.AddOrUpdate(gc.GuildId,
                GetIgnores(gc),
                delegate
                {
                    return GetIgnores(gc);
                });
            return Task.CompletedTask;
        }

        private Task _client_LeftGuild(SocketGuild guild)
        {
            _ignores.TryRemove(guild.Id, out _);
            return Task.CompletedTask;
        }

        public async void TimerFunc(object _)
        {
            try
            {
                await Task.WhenAll(SlowmodeToDelete.Keys
                    .ToArray()
                    .Select(async x =>
                    {
                        await Task.Yield();
                        if (SlowmodeToDelete.TryRemove(x, out var q))
                        {
                            var list = new List<ulong>();
                            while (q.TryDequeue(out var id))
                            {
                                list.Add(id);
                            }

                            var g = _client.GetGuild(x.GuildId);
                            var ch = g?.GetChannel(x.ChannelId) as SocketTextChannel;
                            if (ch == null)
                                return;

                            if (!list.Any())
                                return;

                            var manualDelete = list.Take(5);
                            var msgs = await Task.WhenAll(manualDelete.Select(y => ch.GetMessageAsync(y))).ConfigureAwait(false);
                            var __ = Task.WhenAll(msgs
                                .Where(m => m != null)
                                .Select(m => m.DeleteAsync()));

                            var rem = list.Skip(5).ToArray();
                            if (rem.Any())
                            {
                                await ch.DeleteMessagesAsync(rem).ConfigureAwait(false);
                            }
                        }
                    })).ConfigureAwait(false);
            }
            catch { }
        }

        public async Task<bool> RunBehavior(DiscordSocketClient _, IGuild g, IUserMessage usrMsg)
        {
            await Task.Yield();
            try
            {
                var guild = g as SocketGuild;
                var channel = usrMsg?.Channel as SocketTextChannel;
                if (guild == null || channel == null || usrMsg == null || usrMsg.IsAuthor(_client))
                    return false;

                var user = usrMsg.Author as SocketGuildUser;
                if (user != null)
                {
                    // ignore users with managemessages permission
                    if (guild.OwnerId == user.Id || user.GetPermissions(channel).ManageMessages)
                        return false;
                }
                // see if there is a slowmode active
                if (!SlowmodeChannels.TryGetValue(channel.Id, out Slowmoder limiter))
                    return false;

                if (CheckUserSlowmode(limiter, channel.Guild.Id, usrMsg.Author.Id, user))
                {
                    // if message is falls under slowmode, schedule it for deletion
                    SlowmodeToDelete.AddOrUpdate((channel.Guild.Id, channel.Id),
                        new ConcurrentQueue<ulong>(new[] { usrMsg.Id }),
                        (key, old) =>
                        {
                            old.Enqueue(usrMsg.Id);
                            return old;
                        });
                    return true;
                }
            }
            catch (Exception ex)
            {
                _log.Warn(ex);

            }
            return false;
        }

        private bool CheckUserSlowmode(Slowmoder rl, ulong guildId, ulong userId, SocketGuildUser optUser)
        {
            if (_ignores.TryGetValue(guildId, out var ig))
            {
                // if user is ignored, ignore him
                if (ig.IgnoredUsers.Contains(userId))
                    return false;

                // if user has any of the roles which are ignored, ignore him
                if (optUser != null && optUser.Roles.Any(x => ig.IgnoredRoles.Contains(x.Id)))
                    return false;
            }

            // increment users message count
            var msgCount = rl.Users.AddOrUpdate(userId, 1, (key, old) => ++old);

            //if the message count is greater than allowed, decrement it and block the message
            if (msgCount > rl.MaxMessages)
            {
                // we're decrementing here because otherwise even the blocked messages will increase the value,
                // and if user is spamming, he will never get unblocked
                // (this can be kinda cool, punishing people who spam on slowmode, but not the point here)
                var test = rl.Users.AddOrUpdate(userId, 0, (key, old) => --old);
                return true;
            }

            var _ = Task.Run(async () =>
            {
                await Task.Delay(rl.PerSeconds * 1000).ConfigureAwait(false);
                var newVal = rl.Users.AddOrUpdate(userId, 0, (key, old) => --old);
            });
            return false;
        }

        public bool ToggleWhitelistUser(ulong guildId, ulong userId)
        {
            // create db object
            var siu = new SlowmodeIgnoredUser
            {
                UserId = userId
            };

            HashSet<SlowmodeIgnoredUser> usrs;
            bool removed;
            using (var uow = _db.UnitOfWork)
            {
                usrs = uow.GuildConfigs.ForId(guildId, set => set.Include(x => x.SlowmodeIgnoredUsers))
                    .SlowmodeIgnoredUsers;

                var toDelete = usrs.FirstOrDefault(x => x == siu);
                removed = toDelete != null;
                // try removing - if remove is unsuccessful, add
                if (removed)
                {
                    uow._context.Remove(toDelete);
                }
                else
                {
                    usrs.Add(siu);
                }

                uow.Complete();
            }

            // update ignored users in the dictionary
            _ignores.AddOrUpdate(guildId,
                new SlowmodeIgnores
                {
                    IgnoredRoles = new HashSet<ulong>(),
                    IgnoredUsers = new HashSet<ulong>(usrs.Select(x => x.UserId)),
                },
                (key, old) => new SlowmodeIgnores
                {
                    IgnoredRoles = old.IgnoredRoles,
                    IgnoredUsers = new HashSet<ulong>(usrs.Select(x => x.UserId)),
                });

            return !removed;
        }

        public bool ToggleWhitelistRole(ulong guildId, ulong roleId)
        {
            // create the database object
            var sir = new SlowmodeIgnoredRole
            {
                RoleId = roleId
            };

            HashSet<SlowmodeIgnoredRole> roles;
            bool removed;
            using (var uow = _db.UnitOfWork)
            {
                roles = uow.GuildConfigs.ForId(guildId, set => set.Include(x => x.SlowmodeIgnoredRoles))
                    .SlowmodeIgnoredRoles;
                var toDelete = roles.FirstOrDefault(x => x == sir);
                removed = toDelete != null;
                if (removed)
                {
                    uow._context.Remove(toDelete);
                }
                else
                {
                    roles.Add(sir);
                }

                uow.Complete();
            }
            // completely update the ignored roles in the dictionary
            _ignores.AddOrUpdate(guildId,
                new SlowmodeIgnores
                {
                    IgnoredUsers = new HashSet<ulong>(),
                    IgnoredRoles = new HashSet<ulong>(roles.Select(x => x.RoleId)),
                },
                (key, old) => new SlowmodeIgnores
                {
                    IgnoredUsers = old.IgnoredUsers,
                    IgnoredRoles = new HashSet<ulong>(roles.Select(x => x.RoleId)),
                });

            return !removed;
        }

        public bool StartSlowmode(ulong id, int msgCount, int perSec)
        {
            if (msgCount < 0)
                return false;
            // create a new ratelimiter object which holds the settings
            var rl = new Slowmoder
            {
                MaxMessages = (uint)msgCount,
                PerSeconds = perSec,
            };
            // return whether it's added. If it's not added, the new settings are not applied
            return SlowmodeChannels.TryAdd(id, rl);
        }

        public bool StopSlowmode(ulong id)
        {
            // all we need to do to stop is to remove the settings object from the dictionary
            return SlowmodeChannels.TryRemove(id, out var x);
        }
    }
}
