#nullable disable
namespace NadekoBot.Services.Database.Models;

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