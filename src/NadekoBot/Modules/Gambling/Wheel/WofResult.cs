#nullable disable
namespace NadekoBot.Modules.Gambling.WheelOfFortune;

public readonly struct WofResult
{
    public int Index { get; init; }
    public decimal Multiplier { get; init; }
    public long Amount { get; init; }
}