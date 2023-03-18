namespace NadekoBot.Modules.Utility.Patronage;

public readonly struct UserQuotaStats
{
    private static readonly IReadOnlyDictionary<string, FeatureQuotaStats> _emptyDictionary
        = new Dictionary<string, FeatureQuotaStats>();
    public PatronTier Tier { get; init; } 
        = PatronTier.None;

    public IReadOnlyDictionary<string, FeatureQuotaStats> Features { get; init; }
        = _emptyDictionary;
    
    public IReadOnlyDictionary<string, FeatureQuotaStats> Commands { get; init; }
        = _emptyDictionary;

    public IReadOnlyDictionary<string, FeatureQuotaStats> Groups { get; init; }
        = _emptyDictionary;

    public IReadOnlyDictionary<string, FeatureQuotaStats> Modules { get; init; }
        = _emptyDictionary;

    public UserQuotaStats()
    {
    }
}