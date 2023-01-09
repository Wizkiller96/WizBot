#nullable disable
namespace NadekoBot.Db.Models;

/// <summary>
/// Contains data about usage of Patron-Only commands per user
/// in order to provide support for quota limitations
/// (allow user x who is pledging amount y to use the specified command only
///  x amount of times in the specified time period)
/// </summary>
public class PatronQuota
{
    public ulong UserId { get; set; }
    public FeatureType FeatureType { get; set; }
    public string Feature { get; set; }
    public uint HourlyCount { get; set; }
    public uint DailyCount { get; set; }
    public uint MonthlyCount { get; set; }
}

public enum FeatureType
{
    Command,
    Group,
    Module,
    Limit
}

public class PatronUser
{
    public string UniquePlatformUserId { get; set; }
    public ulong UserId { get; set; }
    public int AmountCents { get; set; }
    
    public DateTime LastCharge { get; set; }
    
    // Date Only component
    public DateTime ValidThru { get; set; }
    
    public PatronUser Clone()
        => new PatronUser()
        {
            UniquePlatformUserId = this.UniquePlatformUserId,
            UserId = this.UserId,
            AmountCents = this.AmountCents,
            LastCharge = this.LastCharge,
            ValidThru = this.ValidThru
        };
}