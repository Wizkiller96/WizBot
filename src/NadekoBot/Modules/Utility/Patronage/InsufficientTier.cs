using NadekoBot.Db.Models;

namespace NadekoBot.Modules.Utility.Patronage;

public readonly struct InsufficientTier
{
    public FeatureType FeatureType { get; init; }
    public string Feature { get; init; }
    public PatronTier RequiredTier { get; init; }
    public PatronTier UserTier { get; init; }
}