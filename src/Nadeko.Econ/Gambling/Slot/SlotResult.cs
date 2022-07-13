namespace Nadeko.Econ.Gambling;

public readonly struct SlotResult
{
    public decimal Multiplier { get; init; }
    public int[] Rolls { get; init; }
    public decimal Won { get; init; }
}