#nullable disable
namespace NadekoBot.Services.Database.Models;

public class AutoTranslateUser : DbEntity
{
    public int ChannelId { get; set; }
    public AutoTranslateChannel Channel { get; set; }
    public ulong UserId { get; set; }
    public string Source { get; set; }
    public string Target { get; set; }
}