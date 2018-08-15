using System;
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

        private readonly string _chatterBotId;
        private readonly IHttpClientFactory _httpFactory;
#if GLOBAL_WIZBOT
        private int _botId = 1;
#else
        private int _botId = 6;
#endif

        public ChatterBotSession(IHttpClientFactory httpFactory)
        {
            _chatterBotId = Rng.Next(0, 1000000).ToString().ToBase64();
            _httpFactory = httpFactory;
        }

#if GLOBAL_WIZBOT
        private string ApiEndpoint => "http://wizbot.cf/cb/chatbot/" +
                                      $"?bot_id={_botId}&" +
                                      "say={0}&" +
                                      $"convo_id=wizbot_{_chatterBotId}&" +
                                      "format=json";
#else
        private string ApiEndpoint => "http://api.program-o.com/v2/chatbot/" +
                                      $"?bot_id={_botId}&" +
                                      "say={0}&" +
                                      $"convo_id=wizbot_{_chatterBotId}&" +
                                      "format=json";
#endif

        public async Task<string> Think(string message)
        {
            using (var http = _httpFactory.CreateClient())
            {
                var res = await http.GetStringAsync(string.Format(ApiEndpoint, message)).ConfigureAwait(false);
                var cbr = JsonConvert.DeserializeObject<ChatterBotResponse>(res);
                return cbr.BotSay.Replace("<br/>", "\n", StringComparison.InvariantCulture);
            }
        }
    }
}
