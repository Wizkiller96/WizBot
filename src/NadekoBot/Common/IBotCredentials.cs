using System.Collections.Generic;
using Discord;
using System.Collections.Immutable;
using System.Linq;
using NadekoBot.Common;

namespace NadekoBot
{
    public interface IBotCredentials
    {
        string Token { get; }
        string GoogleApiKey { get; }
        ICollection<ulong> OwnerIds { get; }
        string RapidApiKey { get; }
        string PatreonAccessToken { get; }

        Creds.DbOptions Db { get; }
        string OsuApiKey { get; }
        int TotalShards { get; }
        string PatreonCampaignId { get; }
        string CleverbotApiKey { get; }
        RestartConfig RestartCommand { get; }
        Creds.VotesSettings Votes { get; }
        string BotListToken { get; }
        string RedisOptions { get; }
        string LocationIqApiKey { get; }
        string TimezoneDbApiKey { get; }
        string CoinmarketcapApiKey { get; }
        string CoordinatorUrl { get; set; }
    }
    
    public class RestartConfig
    {
        public string Cmd { get; set; }
        public string Args { get; set; }
    }
}
