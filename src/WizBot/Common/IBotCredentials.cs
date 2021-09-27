using System.Collections.Generic;
using Discord;
using System.Collections.Immutable;
using System.Linq;
using WizBot.Common;

namespace WizBot
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
        string VotesUrl { get; }
        string VotesToken { get; }
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
