#nullable disable
namespace NadekoBot.Db.Models;

public class IgnoredVoicePresenceChannel : DbEntity
{
    public LogSetting LogSetting { get; set; }
    public ulong ChannelId { get; set; }
}