#nullable disable
using Nadeko.Bot.Db;

namespace Nadeko.Bot.Db.Models;

public class RotatingPlayingStatus : DbEntity
{
    public string Status { get; set; }
    public ActivityType Type { get; set; }
}