#nullable disable
using NadekoBot.Modules.Gambling.Common;
using NadekoBot.Modules.Gambling.Services;
using System.Collections.Immutable;

namespace NadekoBot.Modules.Gambling;

public partial class Gambling
{
    public partial class WheelOfFortuneCommands : GamblingSubmodule<GamblingService>
    {
        private static readonly ImmutableArray<string> _emojis =
            new[] { "⬆", "↖", "⬅", "↙", "⬇", "↘", "➡", "↗" }.ToImmutableArray();

        private readonly ICurrencyService _cs;
        private readonly DbService _db;

        public WheelOfFortuneCommands(ICurrencyService cs, DbService db, GamblingConfigService gamblingConfService)
            : base(gamblingConfService)
        {
            _cs = cs;
            _db = db;
        }

        [Cmd]
        public async partial Task WheelOfFortune(ShmartNumber amount)
        {
            if (!await CheckBetMandatory(amount))
                return;

            if (!await _cs.RemoveAsync(ctx.User.Id, amount, new("wheel", "bet")))
            {
                await ReplyErrorLocalizedAsync(strs.not_enough(CurrencySign));
                return;
            }

            var result = await _service.WheelOfFortuneSpinAsync(ctx.User.Id, amount);

            var wofMultipliers = Config.WheelOfFortune.Multipliers;
            await SendConfirmAsync(Format.Bold($@"{ctx.User} won: {N(result.Amount)}

   『{wofMultipliers[1]}』   『{wofMultipliers[0]}』   『{wofMultipliers[7]}』

『{wofMultipliers[2]}』      {_emojis[result.Index]}      『{wofMultipliers[6]}』

     『{wofMultipliers[3]}』   『{wofMultipliers[4]}』   『{wofMultipliers[5]}』"));
        }
    }
}