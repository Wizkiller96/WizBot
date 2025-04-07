#nullable disable
namespace WizBot.Db.Models;

public class XpSettings : DbEntity
{
    public ulong GuildId { get; set; }

    public HashSet<XpRoleReward> RoleRewards { get; set; } = new();
    public HashSet<XpCurrencyReward> CurrencyRewards { get; set; } = new();
}