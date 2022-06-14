namespace NadekoBot.Modules.Utility.Patronage;

public readonly struct FeatureQuotaStats
{
    public (uint Cur, uint Max) Hourly { get; init; }
    public (uint Cur, uint Max) Daily { get; init; }
    public (uint Cur, uint Max) Monthly { get; init; }
}