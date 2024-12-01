#nullable disable
using WizBot.Modules.Gambling.Common;
using WizBot.Modules.Gambling.Services;

namespace WizBot.Modules.Gambling;

public partial class Gambling
{
    [Group]
    public sealed class BetStatsCommands : GamblingModule<UserBetStatsService>
    {
        private readonly GamblingTxTracker _gamblingTxTracker;

        public BetStatsCommands(
            GamblingTxTracker gamblingTxTracker,
            GamblingConfigService gcs)
            : base(gcs)
        {
            _gamblingTxTracker = gamblingTxTracker;
        }

        [Cmd]
        public async Task BetStatsReset(GamblingGame? game = null)
        {
            var price = await _service.GetResetStatsPriceAsync(ctx.User.Id, game);

            var result = await PromptUserConfirmAsync(CreateEmbed()
                                                             .WithDescription(
                                                                 $"""
                                                                  Are you sure you want to reset your bet stats for **{GetGameName(game)}**?

                                                                  It will cost you {N(price)}
                                                                  """));

            if (!result)
                return;

            var success = await _service.ResetStatsAsync(ctx.User.Id, game);

            if (success)
            {
                await ctx.OkAsync();
            }
            else
            {
                await Response()
                      .Error(strs.not_enough(CurrencySign))
                      .SendAsync();
            }
        }

        private string GetGameName(GamblingGame? game)
        {
            if (game is null)
                return "all games";

            return game.ToString();
        }

        [Cmd]
        [Priority(3)]
        public async Task BetStats()
            => await BetStats(ctx.User, null);

        [Cmd]
        [Priority(2)]
        public async Task BetStats(GamblingGame game)
            => await BetStats(ctx.User, game);

        [Cmd]
        [Priority(1)]
        public async Task BetStats([Leftover] IUser user)
            => await BetStats(user, null);

        [Cmd]
        [Priority(0)]
        public async Task BetStats(IUser user, GamblingGame? game)
        {
            var stats = await _gamblingTxTracker.GetUserStatsAsync(user.Id, game);

            if (stats.Count == 0)
                stats = new()
                {
                    new()
                    {
                        TotalBet = 1
                    }
                };

            var eb = CreateEmbed()
                            .WithOkColor()
                            .WithAuthor(user)
                            .AddField("Total Won", N(stats.Sum(x => x.PaidOut)), true)
                            .AddField("Biggest Win", N(stats.Max(x => x.MaxWin)), true)
                            .AddField("Biggest Bet", N(stats.Max(x => x.MaxBet)), true)
                            .AddField("# Bets", stats.Sum(x => x.WinCount + x.LoseCount), true)
                            .AddField("Payout",
                                (stats.Sum(x => x.PaidOut) / stats.Sum(x => x.TotalBet)).ToString("P2", Culture),
                                true);
            if (game == null)
            {
                var favGame = stats.MaxBy(x => x.WinCount + x.LoseCount);
                eb.AddField("Favorite Game",
                    favGame.Game + "\n" + Format.Italics((favGame.WinCount + favGame.LoseCount) + " plays"),
                    true);
            }
            else
            {
                eb.WithDescription(game.ToString())
                  .AddField("# Wins", stats.Sum(x => x.WinCount), true);
            }

            await Response()
                  .Embed(eb)
                  .SendAsync();
        }

        [Cmd]
        public async Task GambleStats()
        {
            var stats = await _gamblingTxTracker.GetAllAsync();

            var eb = CreateEmbed()
                            .WithOkColor();

            var str = "` Feature `｜`   Bet  `｜`Paid Out`｜`  RoI  `\n";
            str += "――――――――――――――――――――\n";
            foreach (var stat in stats)
            {
                var perc = (stat.PaidOut / stat.Bet).ToString("P2", Culture);
                str += $"`{stat.Feature.PadBoth(9)}`"
                       + $"｜`{stat.Bet.ToString("N0").PadLeft(8, ' ')}`"
                       + $"｜`{stat.PaidOut.ToString("N0").PadLeft(8, ' ')}`"
                       + $"｜`{perc.PadLeft(6, ' ')}`\n";
            }

            var bet = stats.Sum(x => x.Bet);
            var paidOut = stats.Sum(x => x.PaidOut);

            if (bet == 0)
                bet = 1;

            var tPerc = (paidOut / bet).ToString("P2", Culture);
            str += "――――――――――――――――――――\n";
            str += $"` {("TOTAL").PadBoth(7)}` "
                   + $"｜**{N(bet).PadLeft(8, ' ')}**"
                   + $"｜**{N(paidOut).PadLeft(8, ' ')}**"
                   + $"｜`{tPerc.PadLeft(6, ' ')}`";

            eb.WithDescription(str);

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [OwnerOnly]
        public async Task GambleStatsReset()
        {
            if (!await PromptUserConfirmAsync(CreateEmbed()
                                                     .WithDescription(
                                                         """
                                                         Are you sure?
                                                         This will completely reset Gambling Stats.

                                                         This action is irreversible.
                                                         """)))
                return;

            await GambleStats();
            await _service.ResetGamblingStatsAsync();

            await ctx.OkAsync();
        }
    }
}