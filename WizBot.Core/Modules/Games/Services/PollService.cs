﻿using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using WizBot.Common.ModuleBehaviors;
using WizBot.Modules.Games.Common;
using WizBot.Core.Services;
using WizBot.Core.Services.Impl;
using NLog;

namespace WizBot.Modules.Games.Services
{
    public class PollService : IEarlyBlockingExecutor, INService
    {
        public ConcurrentDictionary<ulong, Poll> ActivePolls = new ConcurrentDictionary<ulong, Poll>();
        private readonly Logger _log;
        private readonly DiscordSocketClient _client;
        private readonly WizBotStrings _strings;

        public PollService(DiscordSocketClient client, WizBotStrings strings)
        {
            _log = LogManager.GetCurrentClassLogger();
            _client = client;
            _strings = strings;
        }

        public async Task<bool?> StartPoll(ulong guildId, IUserMessage msg, string arg)
        {
            if (string.IsNullOrWhiteSpace(arg) || !arg.Contains(";"))
                return null;
            var data = arg.Split(';');
            if (data.Length < 3)
                return null;

            var poll = new Poll(_client, _strings, msg, data[0], data.Skip(1));
            if (ActivePolls.TryAdd(guildId, poll))
            {
                poll.OnEnded += (gid) =>
                {
                    ActivePolls.TryRemove(gid, out _);
                };

                await poll.StartPoll().ConfigureAwait(false);
                return true;
            }
            return false;
        }

        public async Task<bool> TryExecuteEarly(DiscordSocketClient client, IGuild guild, IUserMessage msg)
        {
            if (guild == null)
                return false;

            if (!ActivePolls.TryGetValue(guild.Id, out var poll))
                return false;

            try
            {
                return await poll.TryVote(msg).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _log.Warn(ex);
            }

            return false;
        }
    }
}