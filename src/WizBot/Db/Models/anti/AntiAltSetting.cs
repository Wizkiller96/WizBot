namespace WizBot.Db.Models;

public class AntiAltSetting
{
    public int GuildConfigId { get; set; }
    
    public int Id { get; set; }
    public TimeSpan MinAge { get; set; }
    public PunishmentAction Action { get; set; }
    public int ActionDurationMinutes { get; set; }
    public ulong? RoleId { get; set; }
}