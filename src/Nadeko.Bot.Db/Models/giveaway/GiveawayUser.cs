namespace NadekoBot.Db.Models;

public sealed class GiveawayUser
{
    public int Id { get; set; }
    public int GiveawayId { get; set; }
    public ulong UserId { get; set; }
    public string Name { get; set; }
}