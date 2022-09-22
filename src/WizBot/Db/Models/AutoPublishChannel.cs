using WizBot.Services.Database.Models;

namespace WizBot.Db.Models;

public class AutoPublishChannel : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
}