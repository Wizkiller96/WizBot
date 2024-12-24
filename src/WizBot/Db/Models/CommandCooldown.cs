#nullable disable
namespace WizBot.Db.Models;

public class CommandCooldown : DbEntity
{
    public int Seconds { get; set; }
    public string CommandName { get; set; }
}