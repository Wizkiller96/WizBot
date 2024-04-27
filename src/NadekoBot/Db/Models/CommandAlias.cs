#nullable disable
namespace NadekoBot.Db.Models;

public class CommandAlias : DbEntity
{
    public string Trigger { get; set; }
    public string Mapping { get; set; }
}