#nullable disable
namespace WizBot.Db.Models;

public class RotatingPlayingStatus : DbEntity
{
    public string Status { get; set; }
    public DbActivityType Type { get; set; }
}