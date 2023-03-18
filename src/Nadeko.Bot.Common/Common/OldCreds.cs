#nullable disable
namespace NadekoBot.Common;

public class OldCreds
{
    public string Token { get; set; } = string.Empty;
    public ulong[] OwnerIds { get; set; } = new ulong[1];
    public string LoLApiKey { get; set; } = string.Empty;
    public string GoogleApiKey { get; set; } = string.Empty;
    public string MashapeKey { get; set; } = string.Empty;
    public string OsuApiKey { get; set; } = string.Empty;
    public string SoundCloudClientId { get; set; } = string.Empty;
    public string CleverbotApiKey { get; set; } = string.Empty;
    public string CarbonKey { get; set; } = string.Empty;
    public int TotalShards { get; set; } = 1;
    public string PatreonAccessToken { get; set; } = string.Empty;
    public string PatreonCampaignId { get; set; } = "334038";
    public RestartConfig RestartCommand { get; set; }

    public string ShardRunCommand { get; set; } = string.Empty;
    public string ShardRunArguments { get; set; } = string.Empty;
    public int? ShardRunPort { get; set; }
    public string MiningProxyUrl { get; set; } = string.Empty;
    public string MiningProxyCreds { get; set; } = string.Empty;

    public string BotListToken { get; set; } = string.Empty;
    public string TwitchClientId { get; set; } = string.Empty;
    public string VotesToken { get; set; } = string.Empty;
    public string VotesUrl { get; set; } = string.Empty;
    public string RedisOptions { get; set; } = string.Empty;
    public string LocationIqApiKey { get; set; } = string.Empty;
    public string TimezoneDbApiKey { get; set; } = string.Empty;
    public string CoinmarketcapApiKey { get; set; } = string.Empty;

    public class RestartConfig
    {
        public string Cmd { get; set; }
        public string Args { get; set; }

        public RestartConfig(string cmd, string args)
        {
            Cmd = cmd;
            Args = args;
        }
    }
}