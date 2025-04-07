#nullable disable
namespace WizBot.Db.Models;

public class DiscordPermOverride : DbEntity
{
    public GuildPerm Perm { get; set; }

    public ulong? GuildId { get; set; }
    public string Command { get; set; }
}