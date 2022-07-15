namespace Nadeko.Econ.Gambling.Betdraw;

public readonly struct BetdrawResult
{
    public decimal Won { get; init; }
    public decimal Multiplier { get; init; }
    public BetdrawResultType ResultType { get; init; }
    public Deck.Card Card { get; init; }
}