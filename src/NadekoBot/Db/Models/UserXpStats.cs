#nullable disable
namespace NadekoBot.Services.Database.Models;

public class UserXpStats : DbEntity
{
    public ulong UserId { get; set; }
    public ulong GuildId { get; set; }
    public long Xp { get; set; }
    public long AwardedXp { get; set; }
    public XpNotificationLocation NotifyOnLevelUp { get; set; }
}

public enum XpNotificationLocation { None, Dm, Channel }