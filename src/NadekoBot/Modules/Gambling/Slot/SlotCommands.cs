#nullable disable
using NadekoBot.Db.Models;
using NadekoBot.Modules.Gambling.Common;
using NadekoBot.Modules.Gambling.Services;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Text;
using Grpc.Core;
using NadekoBot.Modules.Gambling.WheelOfFortune;
using NadekoBot.Services.Currency;
using Color = SixLabors.ImageSharp.Color;
using Image = SixLabors.ImageSharp.Image;
using OneOf;

namespace NadekoBot.Modules.Gambling;

public enum SlotError
{
    InsufficientFunds,
}

public enum WofError
{
    InsufficientFunds,
}

public interface ISlotService
{
    ValueTask<OneOf<SlotResult, SlotError>> PullAsync(ulong userId, long amount);
}

public record struct WofRequest(ulong UserId, long Amount);

public record struct BetrollRequest(ulong UserId, long Amount);

public sealed class DefaultSlotService : INService
{
    private readonly GamblingConfigService _bcs;
    private readonly ICurrencyService _cs;
    // public ValueTask<OneOf<SlotResult, SlotError>> PullAsync(ulong userId, long amount)
    // {
    //     
    // }

    public DefaultSlotService(GamblingConfigService bcs, ICurrencyService cs)
    {
        _bcs = bcs;
        _cs = cs;
    }
    
    public async Task<OneOf<WofResult, WofError>> Wof(WofRequest request, ServerCallContext context)
    {
        var isTakeSuccess = await _cs.RemoveAsync(request.UserId, request.Amount, new TxData("wof", "bet"));

        if (!isTakeSuccess)
        {
            return WofError.InsufficientFunds;
        }

        var game = new WheelOfFortuneGame(_bcs.Data.WheelOfFortune.Multipliers);
        var result = game.Spin(request.Amount);

        if (result.Amount > 0)
        {
            await _cs.AddAsync(request.UserId, result.Amount, new("wof", "win"));
        }

        return result;
    }

    public override async Task<OneOf<>> BetRoll(BetRollRequest request, ServerCallContext context)
    {
        var takeRes = await _currency.TransferCurrencyAsync(new TransferCurrencyRequest
        {
            Amount = request.Amount,
            Type = "bet-roll",
            Subtype = "bet",
            FromId = request.UserId,
            ToId = 0,
        });

        if (!takeRes.Success)
        {
            return new BetRollReply
            {
                Error = GamblingError.NotEnough
            };
        }

        var game = new Betroll(_config.Data.BetRoll);
        var result = game.Roll();

        if (result.Multiplier > 0)
        {
            var won = (long)(request.Amount * result.Multiplier);

            await _currency.GrantToUserAsync(new GrantToUserRequest
            {
                Amount = won,
                Type = "bet-roll",
                Subtype = "won",
                UserId = request.UserId,
                GranterId = 0,
            });

            return new BetRollReply
            {
                WonAmount = won,
                Multiplier = result.Multiplier,
                Roll = result.Roll,
                Threshold = result.Threshold,
            };
        }

        return new BetRollReply
        {
            WonAmount = 0,
            Multiplier = result.Multiplier,
            Roll = result.Roll,
        };
    }
    
    // public override async Task<BetFlipReply> BetFlip(BetFlipRequest request, ServerCallContext context)
    // {
    //  var takeRes = await _currency.TransferCurrencyAsync(new TransferCurrencyRequest
    //  {
    //      Amount = request.Amount,
    //      Type = "bet-flip",
    //      Subtype = "bet",
    //      FromId = request.UserId,
    //      ToId = 0,
    //  });
    //
    //  if (!takeRes.Success)
    //  {
    //      return new BetFlipReply
    //      {
    //          Error = GamblingError.NotEnough
    //      };
    //  }
    //
    //  var roll = _rng.Next(0, 1000) <= 499;
    //  long won = 0;
    //
    //  if (roll == request.Guess)
    //  {
    //      won = (long) (_config.Data.Multipliers.BetFlip * request.Amount);
    //
    //      await _currency.GrantToUserAsync(new GrantToUserRequest
    //      {
    //          Amount = won,
    //          Type = "bet-flip",
    //          Subtype = "won",
    //          UserId = request.UserId,
    //          GranterId = 0,
    //      });
    //  }
    //
    //
    //  return new BetFlipReply
    //  {
    //      Result = roll
    //          ? BetFlipReply.Types.Side.Heads
    //          : BetFlipReply.Types.Side.Tails,
    //      WonAmount = won
    //  };
    // }
    //
    // public override Task<FlipReply> Flip(FlipRequest request, ServerCallContext context)
    // {
    //  if (request.Count <= 0)
    //      throw new RpcException(new Status(StatusCode.InvalidArgument, "Count has to be greater than 0."));
    //
    //  var results = Enumerable.Range(0, request.Count)
    //      .Select(x => (FlipReply.Types.Roll) _rng.Next(0, 2));
    //
    //  var toReturn = new FlipReply();
    //  toReturn.Rolls.AddRange(results);
    //  return Task.FromResult(toReturn);
    // }
    //
    // public override async Task<SlotResponse> Slot(SlotRequest request, ServerCallContext context)
    // {
    //  var takeRes = await _currency.TransferCurrencyAsync(new TransferCurrencyRequest
    //  {
    //      Amount = request.Amount,
    //      Type = "slot",
    //      Subtype = "bet",
    //      FromId = request.UserId,
    //      ToId = 0,
    //  });
    //
    //  if (!takeRes.Success)
    //  {
    //      return new SlotResponse
    //      {
    //          Error = GamblingError.NotEnough
    //      };
    //  }
    //
    //  var game = new SlotGame();
    //  var result = game.Spin();
    //  long won = 0;
    //
    //  if (result.Multiplier > 0)
    //  {
    //      won = (long) (result.Multiplier * request.Amount);
    //
    //      await _currency.GrantToUserAsync(new GrantToUserRequest
    //      {
    //          Amount = won,
    //          Type = "slot",
    //          Subtype = "won",
    //          UserId = request.UserId,
    //          GranterId = 0,
    //      });
    //  }
    //
    //  var toReturn = new SlotResponse
    //  {
    //      Multiplier = result.Multiplier,
    //      Won = won,
    //  };
    //
    //  toReturn.Rolls.AddRange(result.Rolls);
    //
    //  return toReturn;
    // }
    //
    // private readonly ConcurrentDictionary<ulong, Deck> _decks = new ConcurrentDictionary<ulong, Deck>();
    //
    // public override Task<DeckShuffleReply> DeckShuffle(DeckShuffleRequest request, ServerCallContext context)
    // {
    //  _decks.AddOrUpdate(request.Id, new Deck(), (key, old) => new Deck());
    //  return Task.FromResult(new DeckShuffleReply { });
    // }
    //
    // public override Task<DeckDrawReply> DeckDraw(DeckDrawRequest request, ServerCallContext context)
    // {
    //  if (request.Count < 1 || request.Count > 10)
    //      throw new ArgumentOutOfRangeException(nameof(request.Id));
    //
    //  var deck = request.UseNew
    //      ? new Deck()
    //      : _decks.GetOrAdd(request.Id, new Deck());
    //
    //  var list = new List<Deck.Card>(request.Count);
    //  for (int i = 0; i < request.Count; i++)
    //  {
    //      var card = deck.DrawNoRestart();
    //      if (card is null)
    //      {
    //          if (i == 0)
    //          {
    //              deck.Restart();
    //              list.Add(deck.DrawNoRestart());
    //              continue;
    //          }
    //
    //          break;
    //      }
    //
    //      list.Add(card);
    //  }
    //
    //  // todo 3.2 should replace all "placeholder" words in command strings with a link to the placeholder list explanation
    //  var cards = list
    //      .Select(x => new Card
    //      {
    //          Name = x.ToString().ToLowerInvariant().Replace(' ', '_'),
    //          Number = x.Number,
    //          Suit = (CardSuit) x.Suit
    //      });
    //
    //  var toReturn = new DeckDrawReply();
    //  toReturn.Cards.AddRange(cards);
    //
    //  return Task.FromResult(toReturn);
    // }
    //
    // public override async Task<RpsReply> Rps(RpsRequest request, ServerCallContext context)
    // {
    //  if (request.Amount > 0)
    //  {
    //      var res = await _currency.TransferCurrencyAsync(new TransferCurrencyRequest
    //      {
    //          Amount = request.Amount,
    //          FromId = request.UserId,
    //          Type = "rps",
    //          Subtype = "bet",
    //      });
    //
    //      if (!res.Success)
    //      {
    //          return new RpsReply
    //          {
    //              Result = RpsReply.Types.ResultType.NotEnough
    //          };
    //      }
    //  }
    //
    //  var botPick = _rng.Next(0, 3);
    //  var userPick = (int) request.Pick;
    //
    //  if (botPick == userPick)
    //  {
    //      if (request.Amount > 0)
    //      {
    //          await _currency.GrantToUserAsync(new GrantToUserRequest
    //          {
    //              Amount = request.Amount,
    //              GranterId = 0,
    //              Type = "rps",
    //              Subtype = "draw",
    //              UserId = request.UserId,
    //          });
    //      }
    //
    //      return new RpsReply
    //      {
    //          BotPick = (RpsPick) botPick,
    //          WonAmount = request.Amount,
    //          Result = RpsReply.Types.ResultType.Draw
    //      };
    //  }
    //
    //  if ((botPick == 1 && userPick == 2) || (botPick == 2 && userPick == 0) || (botPick == 0 && userPick == 1))
    //  {
    //      if (request.Amount > 0)
    //      {
    //          await _currency.GrantToUserAsync(new GrantToUserRequest
    //          {
    //              Amount = (long) (request.Amount * 1.95f),
    //              GranterId = 0,
    //              Type = "rps",
    //              Subtype = "draw",
    //              UserId = request.UserId,
    //          });
    //      }
    //
    //      return new RpsReply
    //      {
    //          BotPick = (RpsPick) botPick,
    //          WonAmount = (long) (request.Amount * 1.95f),
    //          Result = RpsReply.Types.ResultType.Won
    //      };
    //  }
    //
    //  return new RpsReply
    //  {
    //      BotPick = (RpsPick) botPick,
    //      WonAmount = 0,
    //      Result = RpsReply.Types.ResultType.Lost
    //  };
    // }
}

public partial class Gambling
{
    [Group]
    public partial class SlotCommands : GamblingSubmodule<GamblingService>
    {
        private static long totalBet;
        private static long totalPaidOut;

        private static readonly HashSet<ulong> _runningUsers = new();

        //here is a payout chart
        //https://lh6.googleusercontent.com/-i1hjAJy_kN4/UswKxmhrbPI/AAAAAAAAB1U/82wq_4ZZc-Y/DE6B0895-6FC1-48BE-AC4F-14D1B91AB75B.jpg
        //thanks to judge for helping me with this

        private readonly IImageCache _images;
        private readonly FontProvider _fonts;
        private readonly DbService _db;

        public SlotCommands(
            ImageCache images,
            FontProvider fonts,
            DbService db,
            GamblingConfigService gamb)
            : base(gamb)
        {
            _images = images;
            _fonts = fonts;
            _db = db;
        }

        public Task Test()
            => Task.CompletedTask;

        [Cmd]
        [OwnerOnly]
        public async partial Task SlotStats()
        {
            //i remembered to not be a moron
            var paid = totalPaidOut;
            var bet = totalBet;

            if (bet <= 0)
                bet = 1;

            var embed = _eb.Create()
                           .WithOkColor()
                           .WithTitle("Slot Stats")
                           .AddField("Total Bet", bet.ToString(), true)
                           .AddField("Paid Out", paid.ToString(), true)
                           .WithFooter($"Payout Rate: {paid * 1.0 / bet * 100:f4}%");

            await ctx.Channel.EmbedAsync(embed);
        }

        [Cmd]
        [OwnerOnly]
        public async partial Task SlotTest(int tests = 1000)
        {
            if (tests <= 0)
                return;
            //multi vs how many times it occured
            var dict = new Dictionary<int, int>();
            for (var i = 0; i < tests; i++)
            {
                var res = SlotMachine.Pull();
                if (dict.ContainsKey(res.Multiplier))
                    dict[res.Multiplier] += 1;
                else
                    dict.Add(res.Multiplier, 1);
            }

            var sb = new StringBuilder();
            var payout = 0;
            foreach (var key in dict.Keys.OrderByDescending(x => x))
            {
                sb.AppendLine($"x{key} occured {dict[key]} times. {dict[key] * 1.0f / tests * 100}%");
                payout += key * dict[key];
            }

            await SendConfirmAsync("Slot Test Results",
                sb.ToString(),
                footer: $"Total Bet: {tests} | Payout: {payout} | {payout * 1.0f / tests * 100}%");
        }

        [Cmd]
        public async partial Task Slot(ShmartNumber amount)
        {
            if (!_runningUsers.Add(ctx.User.Id))
                return;

            try
            {
                if (!await CheckBetMandatory(amount))
                    return;

                await ctx.Channel.TriggerTypingAsync();

                var result = await _service.SlotAsync(ctx.User.Id, amount);

                if (result.Error != GamblingError.None)
                {
                    if (result.Error == GamblingError.NotEnough)
                        await ReplyErrorLocalizedAsync(strs.not_enough(CurrencySign));

                    return;
                }

                Interlocked.Add(ref totalBet, amount);
                Interlocked.Add(ref totalPaidOut, result.Won);

                long ownedAmount;
                await using (var uow = _db.GetDbContext())
                {
                    ownedAmount = uow.Set<DiscordUser>().FirstOrDefault(x => x.UserId == ctx.User.Id)?.CurrencyAmount
                                  ?? 0;
                }

                var slotBg = await _images.GetSlotBgAsync();
                using (var bgImage = Image.Load<Rgba32>(slotBg, out _))
                {
                    var numbers = new int[3];
                    result.Rolls.CopyTo(numbers, 0);

                    Color fontColor = Config.Slots.CurrencyFontColor;

                    bgImage.Mutate(x => x.DrawText(new()
                        {
                            TextOptions = new()
                            {
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center,
                                WrapTextWidth = 140
                            }
                        },
                        result.Won.ToString(),
                        _fonts.DottyFont.CreateFont(65),
                        fontColor,
                        new(227, 92)));

                    var bottomFont = _fonts.DottyFont.CreateFont(50);

                    bgImage.Mutate(x => x.DrawText(new()
                        {
                            TextOptions = new()
                            {
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center,
                                WrapTextWidth = 135
                            }
                        },
                        amount.ToString(),
                        bottomFont,
                        fontColor,
                        new(129, 472)));

                    bgImage.Mutate(x => x.DrawText(new()
                        {
                            TextOptions = new()
                            {
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center,
                                WrapTextWidth = 135
                            }
                        },
                        ownedAmount.ToString(),
                        bottomFont,
                        fontColor,
                        new(325, 472)));
                    //sw.PrintLap("drew red text");

                    for (var i = 0; i < 3; i++)
                    {
                        using var img = Image.Load(await _images.GetSlotEmojiAsync(numbers[i]));
                        bgImage.Mutate(x => x.DrawImage(img, new Point(148 + (105 * i), 217), 1f));
                    }

                    var msg = GetText(strs.better_luck);
                    if (result.Multiplier > 0)
                    {
                        if (Math.Abs(result.Multiplier - 1f) <= float.Epsilon)
                            msg = GetText(strs.slot_single(CurrencySign, 1));
                        else if (Math.Abs(result.Multiplier - 4f) < float.Epsilon)
                            msg = GetText(strs.slot_two(CurrencySign, 4));
                        else if (Math.Abs(result.Multiplier - 10f) <= float.Epsilon)
                            msg = GetText(strs.slot_three(10));
                        else if (Math.Abs(result.Multiplier - 30f) <= float.Epsilon)
                            msg = GetText(strs.slot_jackpot(30));
                    }

                    await using (var imgStream = await bgImage.ToStreamAsync())
                    {
                        await ctx.Channel.SendFileAsync(imgStream,
                            "result.png",
                            Format.Bold(ctx.User.ToString()) + " " + msg);
                    }
                }
            }
            finally
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(1000);
                    _runningUsers.Remove(ctx.User.Id);
                });
            }
        }
    }
}

public sealed class SlotMachine
{
    public const int MAX_VALUE = 5;

    private static readonly List<Func<int[], int>> _winningCombos = new()
    {
        //three flowers
        arr => arr.All(a => a == MAX_VALUE) ? 30 : 0,
        //three of the same
        arr => !arr.Any(a => a != arr[0]) ? 10 : 0,
        //two flowers
        arr => arr.Count(a => a == MAX_VALUE) == 2 ? 4 : 0,
        //one flower
        arr => arr.Any(a => a == MAX_VALUE) ? 1 : 0
    };

    public static SlotResult Pull()
    {
        var numbers = new int[3];
        for (var i = 0; i < numbers.Length; i++)
            numbers[i] = new NadekoRandom().Next(0, MAX_VALUE + 1);
        var multi = 0;
        foreach (var t in _winningCombos)
        {
            multi = t(numbers);
            if (multi != 0)
                break;
        }

        return new(numbers, multi);
    }
}

public struct SlotResult
{
    public int[] Numbers { get; }
    public int Multiplier { get; }

    public SlotResult(int[] nums, int multi)
    {
        Numbers = nums;
        Multiplier = multi;
    }
}