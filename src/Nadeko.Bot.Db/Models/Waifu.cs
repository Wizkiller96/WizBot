#nullable disable
using NadekoBot.Db.Models;

namespace NadekoBot.Services.Database.Models;

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

public class WaifuLbResult
{
    public string Username { get; set; }
    public string Discrim { get; set; }

    public string Claimer { get; set; }
    public string ClaimerDiscrim { get; set; }

    public string Affinity { get; set; }
    public string AffinityDiscrim { get; set; }

    public long Price { get; set; }
}