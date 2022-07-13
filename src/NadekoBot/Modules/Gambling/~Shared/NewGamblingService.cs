#nullable disable
using Nadeko.Econ.Gambling;
using NadekoBot.Modules.Gambling.Services;
using OneOf;

namespace NadekoBot.Modules.Gambling;

public sealed class NewGamblingService : IGamblingService, INService
{
    private readonly GamblingConfigService _bcs;
    private readonly ICurrencyService _cs;

    public NewGamblingService(GamblingConfigService bcs, ICurrencyService cs)
    {
        _bcs = bcs;
        _cs = cs;
    }
    
    // todo input checks
    // todo ladder of fortune
    public async Task<OneOf<WofResult, GamblingError>> WofAsync(ulong userId, long amount)
    {
        var isTakeSuccess = await _cs.RemoveAsync(userId, amount, new("wof", "bet"));

        if (!isTakeSuccess)
        {
            return GamblingError.InsufficientFunds;
        }

        var game = new WofGame(_bcs.Data.WheelOfFortune.Multipliers);
        var result = game.Spin(amount);
        
        var won = (long)result.Won;
        if (won > 0)
        {
            await _cs.AddAsync(userId, won, new("wof", "win"));
        }

        return result;
    }

    public async Task<OneOf<BetrollResult, GamblingError>> BetRollAsync(ulong userId, long amount)
    {
        var isTakeSuccess = await _cs.RemoveAsync(userId, amount, new("betroll", "bet"));

        if (!isTakeSuccess)
        {
            return GamblingError.InsufficientFunds;
        }

        var game = new BetrollGame(_bcs.Data.BetRoll.Pairs
            .Select(x => ((decimal)x.WhenAbove, (decimal)x.MultiplyBy))
            .ToList());
        var result = game.Roll(amount);
        
        var won = (long)result.Won;
        if (won > 0)
        {
            await _cs.AddAsync(userId, won, new("betroll", "win"));
        }

        return result;
    }

    public async Task<OneOf<BetflipResult, GamblingError>> BetFlipAsync(ulong userId, long amount, byte guess)
    {
        var isTakeSuccess = await _cs.RemoveAsync(userId, amount, new("betflip", "bet"));

        if (!isTakeSuccess)
        {
            return GamblingError.InsufficientFunds;
        }

        var game = new BetflipGame(_bcs.Data.BetFlip.Multiplier);
        var result = game.Flip(guess, amount);
        
        var won = (long)result.Won;
        if (won > 0)
        {
            await _cs.AddAsync(userId, won, new("betflip", "win"));
        }
        
        return result;
    }

    public async Task<OneOf<SlotResult, GamblingError>> SlotAsync(ulong userId, long amount)
    {
        if (amount > 0)
        {
            var isTakeSuccess = await _cs.RemoveAsync(userId, amount, new("slot", "bet"));

            if (!isTakeSuccess)
            {
                return GamblingError.InsufficientFunds;
            }
        }

        var game = new SlotGame();
        var result = game.Spin(amount);

        var won = (long)result.Won;
        if (won > 0)
        {
            await _cs.AddAsync(userId, won, new("slot", "won"));
        }

        return result;
    }
    
    public Task<FlipResult[]> FlipAsync(int count)
    {
        var game = new BetflipGame(0);

        var results = new FlipResult[count];
        for (var i = 0; i < count; i++)
        {
            results[i] = new()
            {
                Side = game.Flip(0, 0).Side
            };
        }

        return Task.FromResult(results);
    }
    
    // todo deck draw black/white?
    
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