using NadekoBot.Db.Models;

namespace NadekoBot.Modules.Utility.Patronage;

/// <summary>
/// Represents information about why the user has triggered a quota limit
/// </summary>
public readonly struct QuotaLimit
{
    /// <summary>
    /// Amount of usages reached, which is the limit
    /// </summary>
    public uint Quota { get; init; }
    
    /// <summary>
    /// Which period is this quota limit for (hourly, daily, monthly, etc...)
    /// </summary>
    public QuotaPer QuotaPeriod { get; init; }
    
    /// <summary>
    /// When does this quota limit reset
    /// </summary>
    public DateTime ResetsAt { get; init; }
    
    /// <summary>
    /// Type of the feature this quota limit is for
    /// </summary>
    public FeatureType FeatureType { get; init; }
    
    /// <summary>
    /// Name of the feature this quota limit is for
    /// </summary>
    public string Feature { get; init; }
    
    /// <summary>
    /// Whether it is the user's own quota (true), or server owners (false)
    /// </summary>
    public bool IsOwnQuota { get; init; }
}


/// <summary>
/// Respresent information about the feature limit
/// </summary>
public readonly struct FeatureLimit
{

    /// <summary>
    /// Whether this limit comes from the patronage system 
    /// </summary>
    public bool IsPatronLimit { get; init; } = false;

    /// <summary>
    /// Maximum limit allowed
    /// </summary>
    public int? Quota { get; init; } = null;

    /// <summary>
    /// Name of the limit
    /// </summary>
    public string Name { get; init; } = string.Empty;
    
    public FeatureLimit()
    {
    }
}