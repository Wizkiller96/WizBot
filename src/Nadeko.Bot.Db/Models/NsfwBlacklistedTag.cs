#nullable disable
namespace Nadeko.Bot.Db.Models;

public class NsfwBlacklistedTag : DbEntity
{
    public ulong GuildId { get; set; }
    public string Tag { get; set; }

    public override int GetHashCode()
        => Tag.GetHashCode(StringComparison.InvariantCulture);

    public override bool Equals(object obj)
        => obj is NsfwBlacklistedTag x && x.Tag == Tag;
}