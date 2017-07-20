using System.Net.Http;
using System.Threading.Tasks;
using WizBot.Common;
using WizBot.Extensions;
using Newtonsoft.Json;

namespace WizBot.Modules.Games.Common.ChatterBot
{
    public class ChatterBotSession : IChatterBotSession
    {
        private static WizBotRandom Rng { get; } = new WizBotRandom();
        private readonly string _chatterBotId; { get; }
#if GLOBAL_WIZBOT
        private int _botId = 1;
#else
        private int _botId = 6;
#endif

        public ChatterBotSession()
        {
            _chatterBotId = Rng.Next(0, 1000000).ToString().ToBase64();
        }

#if GLOBAL_WIZBOT
        private string apiEndpoint => "http://wizbot.xyz/cb/chatbot/" +
                                      $"?bot_id={_botId}&" +
                                      "say={0}&" +
                                      $"convo_id=wizbot_{_chatterBotId}&" +
                                      "format=json";
#else
        private string apiEndpoint => "http://api.program-o.com/v2/chatbot/" +
                                      $"?bot_id={_botId}&" +
                                      "say={0}&" +
                                      $"convo_id=WizBot_{_chatterBotId}&" +
                                      "format=json";
#endif

        public async Task<string> Think(string message)
        {
            using (var http = new HttpClient())
            {
                var res = await http.GetStringAsync(string.Format(apiEndpoint, message)).ConfigureAwait(false);
                var cbr = JsonConvert.DeserializeObject<ChatterBotResponse>(res);
                return cbr.BotSay.Replace("<br/>", "\n");
            }
        }
    }
}
