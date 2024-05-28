#nullable disable
namespace WizBot.Db.Models;

public class GroupName : DbEntity
{
    public int GuildConfigId { get; set; }
    public GuildConfig GuildConfig { get; set; }

    public int Number { get; set; }
    public string Name { get; set; }
}