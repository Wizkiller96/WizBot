#nullable disable
namespace WizBot.Services.Database.Models;

public class ImageOnlyChannel : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
}