#nullable disable
using WizBot.Modules.Gambling;
using WizBot.Modules.Gambling.Betdraw;
using WizBot.Modules.Gambling.Rps;
using WizBot.Modules.Gambling.Services;
using OneOf;

namespace WizBot.Modules.Gambling;

public sealed class NewGamblingService : IGamblingService, INService
{
    private readonly GamblingConfigService _bcs;
    private readonly ICurrencyService _cs;

    public NewGamblingService(GamblingConfigService bcs, ICurrencyService cs)
    {
        _bcs = bcs;
        _cs = cs;
    }
    
    public async Task<OneOf<LuLaResult, GamblingError>> LulaAsync(ulong userId, long amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);

        if (amount > 0)
        {
            var isTakeSuccess = await _cs.RemoveAsync(userId, amount, new("lula", "bet"));

            if (!isTakeSuccess)
            {
                return GamblingError.InsufficientFunds;
            }
        }

        var game = new LulaGame(_bcs.Data.LuckyLadder.Multipliers);
        var result = game.Spin(amount);
        
        var won = (long)result.Won;
        if (won > 0)
        {
            await _cs.AddAsync(userId, won, new("lula", "win"));
        }

        return result;
    }

    public async Task<OneOf<BetrollResult, GamblingError>> BetRollAsync(ulong userId, long amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);

        if (amount > 0)
        {
            var isTakeSuccess = await _cs.RemoveAsync(userId, amount, new("betroll", "bet"));

            if (!isTakeSuccess)
            {
                return GamblingError.InsufficientFunds;
            }
        }

        var game = new BetrollGame(_bcs.Data.BetRoll.Pairs
            .Select(x => (x.WhenAbove, (decimal)x.MultiplyBy))
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
        ArgumentOutOfRangeException.ThrowIfNegative(amount);

        ArgumentOutOfRangeException.ThrowIfGreaterThan(guess, 1);

        if (amount > 0)
        {
            var isTakeSuccess = await _cs.RemoveAsync(userId, amount, new("betflip", "bet"));

            if (!isTakeSuccess)
            {
                return GamblingError.InsufficientFunds;
            }
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
    
    public async Task<OneOf<BetdrawResult, GamblingError>> BetDrawAsync(ulong userId, long amount, byte? maybeGuessValue, byte? maybeGuessColor)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);

        if (maybeGuessColor is null && maybeGuessValue is null)
            throw new ArgumentNullException();

        if (maybeGuessColor > 1)
            throw new ArgumentOutOfRangeException(nameof(maybeGuessColor));
        
        if (maybeGuessValue > 1)
            throw new ArgumentOutOfRangeException(nameof(maybeGuessValue));

        if (amount > 0)
        {
            var isTakeSuccess = await _cs.RemoveAsync(userId, amount, new("betdraw", "bet"));

            if (!isTakeSuccess)
            {
                return GamblingError.InsufficientFunds;
            }
        }

        var game = new BetdrawGame();
        var result = game.Draw((BetdrawValueGuess?)maybeGuessValue, (BetdrawColorGuess?)maybeGuessColor, amount);
        
        var won = (long)result.Won;
        if (won > 0)
        {
            await _cs.AddAsync(userId, won, new("betdraw", "win"));
        }
        
        return result;
    }

    public async Task<OneOf<SlotResult, GamblingError>> SlotAsync(ulong userId, long amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);

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
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);

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
    
    //
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

    public async Task<OneOf<RpsResult, GamblingError>> RpsAsync(ulong userId, long amount, byte pick)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pick, 2);
        
        if (amount > 0)
        {
            var isTakeSuccess = await _cs.RemoveAsync(userId, amount, new("rps", "bet"));

            if (!isTakeSuccess)
            {
                return GamblingError.InsufficientFunds;
            }
        }

        var rps = new RpsGame();
        var result = rps.Play((RpsPick)pick, amount);
        
        var won = (long)result.Won;
        if (won > 0)
        {
            var extra = result.Result switch
            {
                RpsResultType.Draw => "draw",
                RpsResultType.Win => "win",
                _ => "lose"
            };

            await _cs.AddAsync(userId, won, new("rps", extra));
        }

        return result;
    }
}