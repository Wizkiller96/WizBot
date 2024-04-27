#nullable disable
namespace NadekoBot.Db.Models;

public class MutedUserId : DbEntity
{
    public ulong UserId { get; set; }

    public override int GetHashCode()
        => UserId.GetHashCode();

    public override bool Equals(object obj)
        => obj is MutedUserId mui ? mui.UserId == UserId : false;
}