#nullable disable
namespace WizBot.Db.Models;

public class DelMsgOnCmdChannel : DbEntity
{
    public int GuildConfigId { get; set; }

    public ulong ChannelId { get; set; }
    public bool State { get; set; }

    public override int GetHashCode()
        => ChannelId.GetHashCode();

    public override bool Equals(object obj)
        => obj is DelMsgOnCmdChannel x && x.ChannelId == ChannelId;
}