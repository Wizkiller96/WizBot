#nullable disable
namespace Nadeko.Bot.Db.Models;

public class SelfAssignedRole : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong RoleId { get; set; }

    public int Group { get; set; }
    public int LevelRequirement { get; set; }
}