#nullable disable
namespace Nadeko.Bot.Db.Models;

public class CommandCooldown : DbEntity
{
    public int Seconds { get; set; }
    public string CommandName { get; set; }
}