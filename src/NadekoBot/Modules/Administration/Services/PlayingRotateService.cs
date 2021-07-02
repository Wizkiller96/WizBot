using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Discord.WebSocket;
using NadekoBot.Common.Replacements;
using NadekoBot.Services;
using NadekoBot.Services.Database.Models;
using Discord;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Common;
using Serilog;

namespace NadekoBot.Modules.Administration.Services
{
    public sealed class PlayingRotateService : INService
    {
        private readonly Timer _t;
        private readonly BotConfigService _bss;
        private readonly SelfService _selfService;
        private readonly Replacer _rep;
        private readonly DbService _db;
        private readonly Bot _bot;

        private class TimerState
        {
            public int Index { get; set; }
        }

        public PlayingRotateService(DiscordSocketClient client, DbService db, Bot bot,
            BotConfigService bss, IEnumerable<IPlaceholderProvider> phProviders, SelfService selfService)
        {
            _db = db;
            _bot = bot;
            _bss = bss;
            _selfService = selfService;

            if (client.ShardId == 0)
            {
                _rep = new ReplacementBuilder()
                    .WithClient(client)
                    .WithProviders(phProviders)
                    .Build();

                _t = new Timer(RotatingStatuses, new TimerState(), TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
            }
        }

        private async void RotatingStatuses(object objState)
        {
            try
            {
                var state = (TimerState) objState;

                if (!_bss.Data.RotateStatuses) return;

                IReadOnlyList<RotatingPlayingStatus> rotatingStatuses;
                using (var uow = _db.GetDbContext())
                {
                    rotatingStatuses = uow.RotatingStatus
                        .AsNoTracking()
                        .OrderBy(x => x.Id)
                        .ToList();
                }

                if (rotatingStatuses.Count == 0)
                    return;

                var playingStatus = state.Index >= rotatingStatuses.Count
                    ? rotatingStatuses[state.Index = 0]
                    : rotatingStatuses[state.Index++];

                var statusText = _rep.Replace(playingStatus.Status);
                await _selfService.SetGameAsync(statusText, playingStatus.Type);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Rotating playing status errored: {ErrorMessage}", ex.Message);
            }
        }

        public async Task<string> RemovePlayingAsync(int index)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));

            using var uow = _db.GetDbContext();
            var toRemove = await uow.RotatingStatus
                .AsQueryable()
                .AsNoTracking()
                .Skip(index)
                .FirstOrDefaultAsync();

            if (toRemove is null)
                return null;

            uow.Remove(toRemove);
            await uow.SaveChangesAsync();
            return toRemove.Status;
        }

        public async Task AddPlaying(ActivityType t, string status)
        {
            using var uow = _db.GetDbContext();
            var toAdd = new RotatingPlayingStatus {Status = status, Type = t};
            uow.Add(toAdd);
            await uow.SaveChangesAsync();
        }

        public bool ToggleRotatePlaying()
        {
            var enabled = false;
            _bss.ModifyConfig(bs => { enabled = bs.RotateStatuses = !bs.RotateStatuses; });
            return enabled;
        }

        public IReadOnlyList<RotatingPlayingStatus> GetRotatingStatuses()
        {
            using var uow = _db.GetDbContext();
            return uow.RotatingStatus.AsNoTracking().ToList();
        }
    }
}