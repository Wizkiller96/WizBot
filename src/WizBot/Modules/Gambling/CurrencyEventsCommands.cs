using Discord;
using Discord.Commands;
using WizBot.Extensions;
using System.Threading.Tasks;
using WizBot.Common.Attributes;
using WizBot.Modules.Gambling.Services;
using WizBot.Modules.Gambling.Common.Events;
using System;
using WizBot.Common;
using WizBot.Services.Database.Models;
using WizBot.Modules.Gambling.Common;

namespace WizBot.Modules.Gambling
{
    public partial class Gambling
    {
        [Group]
        public class CurrencyEventsCommands : GamblingSubmodule<CurrencyEventsService>
        {
            public CurrencyEventsCommands(GamblingConfigService gamblingConf) : base(gamblingConf)
            {
            }

            [WizBotCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            [WizBotOptionsAttribute(typeof(EventOptions))]
            [OwnerOnly]
            public async Task EventStart(CurrencyEvent.Type ev, params string[] options)
            {
                var (opts, _) = OptionsParser.ParseFrom(new EventOptions(), options);
                if (!await _service.TryCreateEventAsync(ctx.Guild.Id,
                    ctx.Channel.Id,
                    ev,
                    opts,
                    GetEmbed))
                {
                    await ReplyErrorLocalizedAsync(strs.start_event_fail).ConfigureAwait(false);
                }
            }

            private IEmbedBuilder GetEmbed(CurrencyEvent.Type type, EventOptions opts, long currentPot)
            {
                return type switch
                {
                    CurrencyEvent.Type.Reaction => _eb.Create()
                        .WithOkColor()
                        .WithTitle(GetText(strs.event_title(type.ToString())))
                        .WithDescription(GetReactionDescription(opts.Amount, currentPot))
                        .WithFooter(GetText(strs.event_duration_footer(opts.Hours))),
                    CurrencyEvent.Type.GameStatus => _eb.Create()
                        .WithOkColor()
                        .WithTitle(GetText(strs.event_title(type.ToString())))
                        .WithDescription(GetGameStatusDescription(opts.Amount, currentPot))
                        .WithFooter(GetText(strs.event_duration_footer(opts.Hours))),
                    _ => throw new ArgumentOutOfRangeException(nameof(type))
                };
            }

            private string GetReactionDescription(long amount, long potSize)
            {
                var potSizeStr = Format.Bold(potSize == 0
                    ? "∞" + CurrencySign
                    : potSize + CurrencySign);
                
                return GetText(strs.new_reaction_event(
                    CurrencySign,
                    Format.Bold(amount + CurrencySign),
                    potSizeStr));
            }

            private string GetGameStatusDescription(long amount, long potSize)
            {
                var potSizeStr = Format.Bold(potSize == 0
                    ? "∞" + CurrencySign
                    : potSize + CurrencySign);
                
                return GetText(strs.new_gamestatus_event(
                    CurrencySign,
                    Format.Bold(amount + CurrencySign),
                    potSizeStr));
            }
        }
    }
}
