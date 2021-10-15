using NadekoBot.Services;
using NadekoBot.Modules.Gambling.Common.Events;
using System.Collections.Concurrent;
using NadekoBot.Modules.Gambling.Common;
using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;
using System;
using NadekoBot.Services.Database.Models;
using System.Net.Http;
using NadekoBot.Modules.Gambling.Services;
using Serilog;

namespace NadekoBot.Modules.Gambling.Services
{
    public class CurrencyEventsService : INService
    {
        private readonly DiscordSocketClient _client;
        private readonly ICurrencyService _cs;
        private readonly GamblingConfigService _configService;

        private readonly ConcurrentDictionary<ulong, ICurrencyEvent> _events =
            new ConcurrentDictionary<ulong, ICurrencyEvent>();


        public CurrencyEventsService(
            DiscordSocketClient client,
            ICurrencyService cs,
            GamblingConfigService configService)
        {
            _client = client;
            _cs = cs;
            _configService = configService;
        }

        public async Task<bool> TryCreateEventAsync(ulong guildId, ulong channelId, CurrencyEvent.Type type,
            EventOptions opts, Func<CurrencyEvent.Type, EventOptions, long, IEmbedBuilder> embed)
        {
            SocketGuild g = _client.GetGuild(guildId);
            SocketTextChannel ch = g?.GetChannel(channelId) as SocketTextChannel;
            if (ch is null)
                return false;

            ICurrencyEvent ce;

            if (type == CurrencyEvent.Type.Reaction)
            {
                ce = new ReactionEvent(_client, _cs, g, ch, opts, _configService.Data, embed);
            }
            else if (type == CurrencyEvent.Type.GameStatus)
            {
                ce = new GameStatusEvent(_client, _cs, g, ch, opts, embed);
            }
            else
            {
                return false;
            }

            var added = _events.TryAdd(guildId, ce);
            if (added)
            {
                try
                {
                    ce.OnEnded += OnEventEnded;
                    await ce.StartEvent().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Error starting event");
                    _events.TryRemove(guildId, out ce);
                    return false;
                }
            }

            return added;
        }

        private Task OnEventEnded(ulong gid)
        {
            _events.TryRemove(gid, out _);
            return Task.CompletedTask;
        }
    }
}