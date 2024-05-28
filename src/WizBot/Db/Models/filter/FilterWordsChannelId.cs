#nullable disable
namespace WizBot.Db.Models;

public class FilterWordsChannelId : DbEntity
{
    public int? GuildConfigId { get; set; }
    public ulong ChannelId { get; set; }

    public bool Equals(FilterWordsChannelId other)
        => ChannelId == other.ChannelId;

    public override bool Equals(object obj)
        => obj is FilterWordsChannelId fci && Equals(fci);

    public override int GetHashCode()
        => ChannelId.GetHashCode();
}