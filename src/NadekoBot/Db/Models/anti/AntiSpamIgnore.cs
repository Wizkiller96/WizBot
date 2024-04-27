namespace NadekoBot.Db.Models;

public class AntiSpamIgnore : DbEntity
{
    public ulong ChannelId { get; set; }

    public override int GetHashCode()
        => ChannelId.GetHashCode();

    public override bool Equals(object? obj)
        => obj is AntiSpamIgnore inst && inst.ChannelId == ChannelId;
}