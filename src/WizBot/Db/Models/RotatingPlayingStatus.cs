#nullable disable
namespace WizBot.Services.Database.Models;

public class RotatingPlayingStatus : DbEntity
{
    public string Status { get; set; }
    public ActivityType Type { get; set; }
}