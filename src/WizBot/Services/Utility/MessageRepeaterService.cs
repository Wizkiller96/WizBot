using Discord.WebSocket;
using WizBot.Services.Database.Models;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WizBot.Services.Utility
{
    //todo 50 rewrite
    public class MessageRepeaterService
    {
        //messagerepeater
        //guildid/RepeatRunners
        public ConcurrentDictionary<ulong, ConcurrentQueue<RepeatRunner>> Repeaters { get; set; }
        public bool RepeaterReady { get; private set; }

        public MessageRepeaterService(WizBot bot, DiscordShardedClient client, IEnumerable<GuildConfig> gcs)
        {
            System.Console.WriteLine(bot.Ready);
            var _ = Task.Run(async () =>
            {
                while (!bot.Ready)
                    await Task.Delay(1000);

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
