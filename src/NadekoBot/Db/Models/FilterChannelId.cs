#nullable disable
namespace NadekoBot.Services.Database.Models;

public class FilterChannelId : DbEntity
{
    public ulong ChannelId { get; set; }

    public bool Equals(FilterChannelId other) 
        => ChannelId == other.ChannelId;

    public override bool Equals(object obj)
        => obj is FilterChannelId fci && Equals(fci); 

    public override int GetHashCode() 
        => ChannelId.GetHashCode();
}

public class FilterInvitesChannelId : DbEntity
{
    public ulong ChannelId { get; set; }

    public bool Equals(FilterInvitesChannelId other) 
        => ChannelId == other.ChannelId;

    public override bool Equals(object obj)
        => obj is FilterInvitesChannelId fci && Equals(fci); 

    public override int GetHashCode() 
        => ChannelId.GetHashCode();
}
