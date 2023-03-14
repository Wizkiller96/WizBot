#nullable disable
using Nadeko.Bot.Db;

namespace NadekoBot.Services.Database.Models;

public class RotatingPlayingStatus : DbEntity
{
    public string Status { get; set; }
    public ActivityType Type { get; set; }
}