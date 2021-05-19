using WizBot.Core.Common;
using WizBot.Core.Common.Configs;
using WizBot.Core.Modules.Gambling.Common;
using WizBot.Core.Services;

namespace WizBot.Core.Modules.Gambling.Services
{
    public sealed class GamblingConfigService : ConfigServiceBase<GamblingConfig>
    {
        public override string Name { get; } = "gambling";
        private const string FilePath = "data/gambling.yml";
        private static TypedKey<GamblingConfig> changeKey = new TypedKey<GamblingConfig>("config.gambling.updated");


        public GamblingConfigService(IConfigSeria serializer, IPubSub pubSub)
            : base(FilePath, serializer, pubSub, changeKey)
        {
            AddParsedProp("currency.name", gs => gs.Currency.Name, SettingParsers.String, SettingPrinters.ToString);
            AddParsedProp("currency.sign", gs => gs.Currency.Sign, SettingParsers.String, SettingPrinters.ToString);
            AddParsedProp("gen.min", gs => gs.Generation.MinAmount, int.TryParse, SettingPrinters.ToString, val => val >= 1);
            AddParsedProp("gen.max", gs => gs.Generation.MaxAmount, int.TryParse, SettingPrinters.ToString, val => val >= 1);
            AddParsedProp("gen.cd", gs => gs.Generation.GenCooldown, int.TryParse, SettingPrinters.ToString, val => val > 0);
            AddParsedProp("gen.chance", gs => gs.Generation.Chance, decimal.TryParse, SettingPrinters.ToString, val => val >= 0 && val <= 1);
            AddParsedProp("gen.has_pw", gs => gs.Generation.HasPassword, bool.TryParse, SettingPrinters.ToString);
            AddParsedProp("bf.multi", gs => gs.BetFlip.Multiplier, decimal.TryParse, SettingPrinters.ToString, val => val >= 1);
            AddParsedProp("waifu.min_price", gs => gs.Waifu.MinPrice, int.TryParse, SettingPrinters.ToString, val => val >= 0);
            AddParsedProp("waifu.multi.reset", gs => gs.Waifu.Multipliers.WaifuReset, int.TryParse, SettingPrinters.ToString, val => val >= 0);
            AddParsedProp("waifu.multi.crush_claim", gs => gs.Waifu.Multipliers.CrushClaim, decimal.TryParse, SettingPrinters.ToString, val => val >= 0);
            AddParsedProp("waifu.multi.normal_claim", gs => gs.Waifu.Multipliers.NormalClaim, decimal.TryParse, SettingPrinters.ToString, val => val > 0);
            AddParsedProp("waifu.multi.divorce_value", gs => gs.Waifu.Multipliers.DivorceNewValue, decimal.TryParse, SettingPrinters.ToString, val => val > 0);
            AddParsedProp("waifu.multi.all_gifts", gs => gs.Waifu.Multipliers.AllGiftPrices, decimal.TryParse, SettingPrinters.ToString, val => val > 0);
            AddParsedProp("waifu.multi.gift_effect", gs => gs.Waifu.Multipliers.GiftEffect, decimal.TryParse, SettingPrinters.ToString, val => val >= 0);
            AddParsedProp("decay.percent", gs => gs.Decay.Percent, decimal.TryParse, SettingPrinters.ToString, val => val >= 0 && val <= 1);
            AddParsedProp("decay.maxdecay", gs => gs.Decay.MaxDecay, int.TryParse, SettingPrinters.ToString, val => val >= 0);
            AddParsedProp("decay.threshold", gs => gs.Decay.MinThreshold, int.TryParse, SettingPrinters.ToString, val => val >= 0);
        }
    }
}