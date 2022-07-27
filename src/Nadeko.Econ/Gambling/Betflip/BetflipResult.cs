namespace Nadeko.Econ.Gambling;

public readonly struct BetflipResult
{
    public decimal Won { get; init; }
    public byte Side { get; init; }
    public decimal Multiplier { get; init; }
}