using NadekoBot.Common.Yml;
using Cloneable;

namespace NadekoBot.Modules.Utility.Patronage;

[Cloneable]
public partial class PatronConfigData : ICloneable<PatronConfigData>
{
    [Comment("DO NOT CHANGE")]
    public int Version { get; set; } = 2;
    
    [Comment("Whether the patronage feature is enabled")]
    public bool IsEnabled { get; set; }

    [Comment("List of patron only features and relevant quota data")]
    public FeatureQuotas Quotas { get; set; }

    public PatronConfigData()
    {
        Quotas = new();
    }

    public class FeatureQuotas
    {
        [Comment("Dictionary of feature names with their respective limits. Set to null for unlimited")]
        public Dictionary<string, Dictionary<PatronTier, int?>> Features { get; set; } = new();
        
        [Comment("Dictionary of commands with their respective quota data")]
        public Dictionary<string, Dictionary<PatronTier, Dictionary<QuotaPer, uint>?>> Commands { get; set; } = new();

        [Comment("Dictionary of groups with their respective quota data")]
        public Dictionary<string, Dictionary<PatronTier, Dictionary<QuotaPer, uint>?>> Groups { get; set; } = new();

        [Comment("Dictionary of modules with their respective quota data")]
        public Dictionary<string, Dictionary<PatronTier, Dictionary<QuotaPer, uint>?>> Modules { get; set; } = new();
    }
}