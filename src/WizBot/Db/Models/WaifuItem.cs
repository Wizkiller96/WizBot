#nullable disable
namespace WizBot.Services.Database.Models;

public class WaifuItem : DbEntity
{
    public int? WaifuInfoId { get; set; }
    public string ItemEmoji { get; set; }
    public string Name { get; set; }
}