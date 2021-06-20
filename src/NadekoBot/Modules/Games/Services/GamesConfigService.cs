using NadekoBot.Common;
using NadekoBot.Common.Configs;
using NadekoBot.Services;
using NadekoBot.Modules.Games.Common;

namespace NadekoBot.Modules.Games.Services
{
    public sealed class GamesConfigService : ConfigServiceBase<GamesConfig>
    {
        public override string Name { get; } = "games";
        private const string FilePath = "data/games.yml";
        private static TypedKey<GamesConfig> changeKey = new TypedKey<GamesConfig>("config.games.updated");

        public GamesConfigService(IConfigSeria serializer, IPubSub pubSub)
            : base(FilePath, serializer, pubSub, changeKey)
        {
            AddParsedProp("trivia.min_win_req", gs => gs.Trivia.MinimumWinReq, int.TryParse,
                ConfigPrinters.ToString, val => val > 0);
            AddParsedProp("trivia.currency_reward", gs => gs.Trivia.CurrencyReward, long.TryParse,
                ConfigPrinters.ToString, val => val >= 0);
        }
    }
}