﻿using System;
using System.Linq;
using System.Threading;
using Discord.WebSocket;
using WizBot.Common.Replacements;
using WizBot.Core.Services;
using WizBot.Core.Services.Database.Models;
using NLog;
using WizBot.Modules.Music.Services;
using Discord;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace WizBot.Modules.Administration.Services
{
    public class PlayingRotateService : INService
    {
        private readonly Timer _t;
        private readonly DiscordSocketClient _client;
        private readonly Logger _log;
        private readonly IDataCache _cache;
        private readonly Replacer _rep;
        private readonly DbService _db;
        private readonly IBotConfigProvider _bcp;

        public BotConfig BotConfig => _bcp.BotConfig;

        private class TimerState
        {
            public int Index { get; set; }
        }

        public PlayingRotateService(DiscordSocketClient client, IBotConfigProvider bcp,
            DbService db, IDataCache cache, WizBot bot, MusicService music)
        {
            _client = client;
            _bcp = bcp;
            _db = db;
            _log = LogManager.GetCurrentClassLogger();
            _cache = cache;

            if (client.ShardId == 0)
            {

                _rep = new ReplacementBuilder()
                    .WithClient(client)
                    .WithMusic(music)
                    .Build();

                _t = new Timer(async (objState) =>
                {
                    try
                    {
                        // bcp.Reload();

                        var state = (TimerState)objState;
                        if (!BotConfig.RotatingStatuses)
                            return;
                        if (state.Index >= BotConfig.RotatingStatusMessages.Count)
                            state.Index = 0;

                        if (!BotConfig.RotatingStatusMessages.Any())
                            return;
                        var msg = BotConfig.RotatingStatusMessages[state.Index++];
                        var status = msg.Status;
                        if (string.IsNullOrWhiteSpace(status))
                            return;

                        status = _rep.Replace(status);

                        try
                        {
                            await bot.SetGameAsync(status, msg.Type).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            _log.Warn(ex);
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.Warn("Rotating playing status errored.\n" + ex);
                    }
                }, new TimerState(), TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
            }
        }

        public async Task<string> RemovePlayingAsync(int index)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));

            string msg;
            using (var uow = _db.UnitOfWork)
            {
                var config = uow.BotConfig.GetOrCreate(set => set.Include(x => x.RotatingStatusMessages));

                if (index >= config.RotatingStatusMessages.Count)
                    return null;
                msg = config.RotatingStatusMessages[index].Status;
                var remove = config.RotatingStatusMessages[index];
                uow._context.Remove(remove);
                _bcp.BotConfig.RotatingStatusMessages = config.RotatingStatusMessages;
                await uow.CompleteAsync();
            }

            return msg;
        }

        public async Task AddPlaying(ActivityType t, string status)
        {
            using (var uow = _db.UnitOfWork)
            {
                var config = uow.BotConfig.GetOrCreate(set => set.Include(x => x.RotatingStatusMessages));
                var toAdd = new PlayingStatus { Status = status, Type = t };
                config.RotatingStatusMessages.Add(toAdd);
                _bcp.BotConfig.RotatingStatusMessages = config.RotatingStatusMessages;
                await uow.CompleteAsync();
            }
        }

        public bool ToggleRotatePlaying()
        {
            bool enabled;
            using (var uow = _db.UnitOfWork)
            {
                var config = uow.BotConfig.GetOrCreate(set => set);

                enabled = config.RotatingStatuses = !config.RotatingStatuses;
                uow.Complete();
            }
            return enabled;
        }
    }
}
