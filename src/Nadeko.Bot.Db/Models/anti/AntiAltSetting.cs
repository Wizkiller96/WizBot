namespace NadekoBot.Services.Database.Models;

public class AntiAltSetting
{
    public int Id { get; set; }
    public int GuildConfigId { get; set; }
    public TimeSpan MinAge { get; set; }
    public PunishmentAction Action { get; set; }
    public int ActionDurationMinutes { get; set; }
    public ulong? RoleId { get; set; }
}