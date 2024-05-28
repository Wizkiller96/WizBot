#nullable disable
namespace WizBot.Db.Models;

public class BanTemplate : DbEntity
{
    public ulong GuildId { get; set; }
    public string Text { get; set; }
    public int? PruneDays { get; set; }
}