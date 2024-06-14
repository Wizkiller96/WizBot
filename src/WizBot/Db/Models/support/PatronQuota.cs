#nullable disable
namespace WizBot.Db.Models;

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