namespace NadekoBot.Services.Database.Models
{
    public class IgnoredLogItem : DbEntity
    {
        public LogSetting LogSetting { get; set; }
        public ulong LogItemId { get; set; }
        public IgnoredItemType ItemType { get; set; }
    }

    public enum IgnoredItemType
    {
        Channel,
        User,
    }
}
