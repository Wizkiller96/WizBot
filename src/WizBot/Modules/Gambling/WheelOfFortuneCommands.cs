﻿using Discord;
using WizBot.Common.Attributes;
using WizBot.Extensions;
using System.Threading.Tasks;
using Wof = WizBot.Modules.Gambling.Common.WheelOfFortune.WheelOfFortuneGame;
using WizBot.Modules.Gambling.Services;
using WizBot.Modules.Gambling.Common;
using System.Collections.Immutable;
using WizBot.Common;
using WizBot.Services;

namespace WizBot.Modules.Gambling
{
    public partial class Gambling
    {
        public class WheelOfFortuneCommands : GamblingSubmodule<GamblingService>
        {
            private static readonly ImmutableArray<string> _emojis = new string[] {
            "⬆",
            "↖",
            "⬅",
            "↙",
            "⬇",
            "↘",
            "➡",
            "↗" }.ToImmutableArray();

            private readonly ICurrencyService _cs;
            private readonly DbService _db;

            public WheelOfFortuneCommands(ICurrencyService cs, DbService db, GamblingConfigService gamblingConfService)
                : base(gamblingConfService)
            {
                _cs = cs;
                _db = db;
            }

            [WizBotCommand, Aliases]
            public async Task WheelOfFortune(ShmartNumber amount)
            {
                if (!await CheckBetMandatory(amount).ConfigureAwait(false))
                    return;

                if (!await _cs.RemoveAsync(ctx.User.Id, "Wheel Of Fortune - bet", amount, gamble: true).ConfigureAwait(false))
                {
                    await ReplyErrorLocalizedAsync(strs.not_enough(CurrencySign));
                    return;
                }

                var result = await _service.WheelOfFortuneSpinAsync(ctx.User.Id, amount).ConfigureAwait(false);

                var wofMultipliers = _config.WheelOfFortune.Multipliers;
                await SendConfirmAsync(
Format.Bold($@"{ctx.User.ToString()} won: {result.Amount + CurrencySign}

   『{wofMultipliers[1]}』   『{wofMultipliers[0]}』   『{wofMultipliers[7]}』

『{wofMultipliers[2]}』      {_emojis[result.Index]}      『{wofMultipliers[6]}』

     『{wofMultipliers[3]}』   『{wofMultipliers[4]}』   『{wofMultipliers[5]}』")).ConfigureAwait(false);
            }
        }
    }
}