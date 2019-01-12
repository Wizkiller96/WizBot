using Discord;
using Discord.Commands;
using WizBot.Extensions;
using WizBot.Core.Services;
using System.Threading.Tasks;
using Discord.WebSocket;
using WizBot.Common.Attributes;
using WizBot.Modules.Gambling.Services;
using WizBot.Core.Common;
using WizBot.Core.Services.Database.Models;
using WizBot.Core.Modules.Gambling.Common.Events;
using System;

namespace WizBot.Modules.Gambling
{
    public partial class Gambling
    {
        [Group]
        public class CurrencyEventsCommands : WizBotSubmodule<CurrencyEventsService>
        {
            public enum OtherEvent
            {
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
            [WizBotOptionsAttribute(typeof(EventOptions))]
            [AdminOnly]
            public async Task EventStart(CurrencyEvent.Type ev, params string[] options)
            {
                var (opts, _) = OptionsParser.ParseFrom(new EventOptions(), options);
                if (!await _service.TryCreateEventAsync(Context.Guild.Id,
                    Context.Channel.Id,
                    ev,
                    opts,
                    GetEmbed
                    ).ConfigureAwait(false))
                {
                    await ReplyErrorLocalizedAsync("start_event_fail").ConfigureAwait(false);
                    return;
                }
            }

            private EmbedBuilder GetEmbed(CurrencyEvent.Type type, EventOptions opts, long currentPot)
            {
                switch (type)
                {
                    case CurrencyEvent.Type.Reaction:
                        return new EmbedBuilder()
                            .WithOkColor()
                            .WithTitle(GetText("event_title", type.ToString()))
                            .WithDescription(GetReactionDescription(opts.Amount, currentPot))
                            .WithFooter(GetText("event_duration_footer", opts.Hours));
                    case CurrencyEvent.Type.GameStatus:
                        return new EmbedBuilder()
                            .WithOkColor()
                            .WithTitle(GetText("event_title", type.ToString()))
                            .WithDescription(GetGameStatusDescription(opts.Amount, currentPot))
                            .WithFooter(GetText("event_duration_footer", opts.Hours));
                    default:
                        break;
                }
                throw new ArgumentOutOfRangeException(nameof(type));
            }

            private string GetReactionDescription(long amount, long potSize)
            {
                string potSizeStr = Format.Bold(potSize == 0
                    ? "∞" + Bc.BotConfig.CurrencySign
                    : potSize.ToString() + Bc.BotConfig.CurrencySign);
                return GetText("new_reaction_event",
                                   Bc.BotConfig.CurrencySign,
                                   Format.Bold(amount + Bc.BotConfig.CurrencySign),
                                   potSizeStr);
            }

            private string GetGameStatusDescription(long amount, long potSize)
            {
                string potSizeStr = Format.Bold(potSize == 0
                    ? "∞" + Bc.BotConfig.CurrencySign
                    : potSize.ToString() + Bc.BotConfig.CurrencySign);
                return GetText("new_gamestatus_event",
                                   Bc.BotConfig.CurrencySign,
                                   Format.Bold(amount + Bc.BotConfig.CurrencySign),
                                   potSizeStr);
            }
        }
    }
}
