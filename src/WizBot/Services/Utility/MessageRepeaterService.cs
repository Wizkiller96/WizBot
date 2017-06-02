using Discord.WebSocket;
using WizBot.Services.Database.Models;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WizBot.Services.Utility
{
    public class MessageRepeaterService
    {
        //messagerepeater
        //guildid/RepeatRunners
        public ConcurrentDictionary<ulong, ConcurrentQueue<RepeatRunner>> Repeaters { get; set; }
        public bool RepeaterReady { get; private set; }

        public MessageRepeaterService(DiscordShardedClient client, IEnumerable<GuildConfig> gcs)
        {
            var _ = Task.Run(async () =>
            {
#if !GLOBAL_WIZBOT
                await Task.Delay(5000).ConfigureAwait(false);
#else
                    await Task.Delay(30000).ConfigureAwait(false);
#endif
                //todo this is pretty terrible :kms: no time
                Repeaters = new ConcurrentDictionary<ulong, ConcurrentQueue<RepeatRunner>>(gcs
                    .ToDictionary(gc => gc.GuildId,
                        gc => new ConcurrentQueue<RepeatRunner>(gc.GuildRepeaters
                            .Select(gr => new RepeatRunner(client, gr))
                            .Where(x => x.Guild != null))));
                RepeaterReady = true;
            });
        }
    }
}