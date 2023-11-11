#nullable disable
using NadekoBot.Db.Models;

namespace Nadeko.Bot.Db.Models;

public class WaifuInfo : DbEntity
{
    public int WaifuId { get; set; }
    public DiscordUser Waifu { get; set; }
    
    public int? ClaimerId { get; set; }
    public DiscordUser Claimer { get; set; }

    public int? AffinityId { get; set; }
    public DiscordUser Affinity { get; set; }

    public long Price { get; set; }
    public List<WaifuItem> Items { get; set; } = new();
}