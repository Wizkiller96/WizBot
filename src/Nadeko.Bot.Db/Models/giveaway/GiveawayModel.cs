namespace NadekoBot.Db.Models;

public sealed class GiveawayModel
{
    public int Id { get; set; }
    public ulong GuildId { get; set; }
    public ulong MessageId { get; set; }
    public string Message { get; set; }

    public IList<GiveawayUser> Participants { get; set; } = new List<GiveawayUser>();
    public DateTime EndsAt { get; set; }
}