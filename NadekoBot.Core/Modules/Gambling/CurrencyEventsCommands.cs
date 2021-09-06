using Discord;
using Discord.Commands;
using NadekoBot.Extensions;
using System.Threading.Tasks;
using NadekoBot.Common.Attributes;
using NadekoBot.Modules.Gambling.Services;
using NadekoBot.Core.Common;
using NadekoBot.Core.Services.Database.Models;
using NadekoBot.Core.Modules.Gambling.Common.Events;
using System;
using NadekoBot.Core.Modules.Gambling.Common;
using NadekoBot.Core.Modules.Gambling.Services;

namespace NadekoBot.Modules.Gambling
{
    public partial class Gambling
    {
        [Group]
        public class CurrencyEventsCommands : GamblingSubmodule<CurrencyEventsService>
        {
            public enum OtherEvent
            {
                BotListUpvoters
            }

            public CurrencyEventsCommands(GamblingConfigService gamblingConf) : base(gamblingConf)
            {
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [NadekoOptionsAttribute(typeof(EventOptions))]
            [OwnerOnly]
            public async Task EventStart(CurrencyEvent.Type ev, params string[] options)
            {
                var (opts, _) = OptionsParser.ParseFrom(new EventOptions(), options);
                if (!await _service.TryCreateEventAsync(ctx.Guild.Id,
                    ctx.Channel.Id,
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
                    ? "∞" + CurrencySign
                    : potSize.ToString() + CurrencySign);
                return GetText("new_reaction_event",
                                   CurrencySign,
                                   Format.Bold(amount + CurrencySign),
                                   potSizeStr);
            }

            private string GetGameStatusDescription(long amount, long potSize)
            {
                string potSizeStr = Format.Bold(potSize == 0
                    ? "∞" + CurrencySign
                    : potSize.ToString() + CurrencySign);
                return GetText("new_gamestatus_event",
                                   CurrencySign,
                                   Format.Bold(amount + CurrencySign),
                                   potSizeStr);
            }
        }
    }
}
