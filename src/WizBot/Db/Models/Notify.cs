using System.ComponentModel.DataAnnotations;

namespace WizBot.Db.Models;

public class Notify
{
    [Key]
    public int Id { get; set; }
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public NotifyEvent Event { get; set; }

    [MaxLength(10_000)]
    public string Message { get; set; } = string.Empty;
}

public enum NotifyEvent
{
    UserLevelUp
}