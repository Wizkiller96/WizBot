using Discord;
using Discord.Commands;
using WizBot.Extensions;
using WizBot.Core.Services;
using System.Threading.Tasks;
using Discord.WebSocket;
using WizBot.Common.Attributes;
using WizBot.Modules.Gambling.Common;
using WizBot.Modules.Gambling.Services;
using WizBot.Modules.Gambling.Common.CurrencyEvents;
using System.Net.Http;
using Newtonsoft.Json;
using System.Linq;

namespace WizBot.Modules.Gambling
{
    public partial class Gambling
    {
        [Group]
        public class CurrencyEventsCommands : WizBotSubmodule<CurrencyEventsService>
        {
            public enum CurrencyEvent
            {
                Reaction,
                SneakyGameStatus,
                BotListUpvoters
            }

            private readonly DiscordSocketClient _client;
            private readonly IBotCredentials _creds;
            private readonly ICurrencyService _cs;

            public CurrencyEventsCommands(DiscordSocketClient client, ICurrencyService cs, IBotCredentials creds)
            {
                _client = client;
                _creds = creds;
                _cs = cs;
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [AdminOnly]
            public async Task StartEvent(CurrencyEvent e, long arg = -1)
            {
                switch (e)
                {
                    case CurrencyEvent.Reaction:
                        await ReactionEvent(Context, arg).ConfigureAwait(false);
                        break;
                    case CurrencyEvent.SneakyGameStatus:
                        await SneakyGameStatusEvent(Context, arg).ConfigureAwait(false);
                        break;
#if GLOBAL_WIZBOT
                    case CurrencyEvent.BotListUpvoters:
                        await BotListUpvoters(arg);
                    break;
#endif
                    default:
                        return;
                }
            }

            private async Task BotListUpvoters(long amount)
            {
                if (amount <= 0 || string.IsNullOrWhiteSpace(_creds.BotListToken))
                    return;
                string res;
                using (var http = new HttpClient())
                {
                    http.DefaultRequestHeaders.Add("Authorization", _creds.BotListToken);
                    res = await http.GetStringAsync($"https://discordbots.org/api/bots/170849991357628416/votes?onlyids=true");
                }
                var ids = JsonConvert.DeserializeObject<ulong[]>(res);
                await _cs.AddBulkAsync(ids, ids.Select(x => "Botlist Upvoter Event"), ids.Select(x => amount), true);
                await ReplyConfirmLocalized("bot_list_awarded",
                    Format.Bold(amount.ToString()),
                    Format.Bold(ids.Length.ToString())).ConfigureAwait(false);
            }

            private async Task SneakyGameStatusEvent(ICommandContext context, long num)
            {
                if (num < 10 || num > 600)
                    num = 60;

                var ev = new SneakyEvent(_cs, _client, _bc, num);
                if (!await _service.StartSneakyEvent(ev, context.Message, context))
                    return;
                try
                {
                    var title = GetText("sneakygamestatus_title");
                    var desc = GetText("sneakygamestatus_desc", 
                        Format.Bold(100.ToString()) + _bc.BotConfig.CurrencySign,
                        Format.Bold(num.ToString()));
                    await context.Channel.SendConfirmAsync(title, desc)
                        .ConfigureAwait(false);
                }
                catch
                {
                    // ignored
                }
            }

            public async Task ReactionEvent(ICommandContext context, long amount)
            {
                if (amount <= 0)
                    amount = 100;

                var title = GetText("reaction_title");
                var desc = GetText("reaction_desc", _bc.BotConfig.CurrencySign, Format.Bold(amount.ToString()) + _bc.BotConfig.CurrencySign);
                var footer = GetText("reaction_footer", 24);
                var re = new ReactionEvent(_bc.BotConfig, _client, _cs, amount);
                var msg = await context.Channel.SendConfirmAsync(title,
                        desc, footer: footer)
                    .ConfigureAwait(false);
                await re.Start(msg, context);
            }
        }
    }
}