using System.Collections.Generic;
using NadekoBot.Common.Yml;


namespace Nadeko.Common
{
    public sealed record Creds
    {
        public Creds()
        {
            Token = string.Empty;
            OwnerIds = new()
            {
                105635576866156544
            };
            TotalShards = 1;
            GoogleApiKey = string.Empty;
            Votes = new(string.Empty, string.Empty);
            Patreon = new(string.Empty, string.Empty, string.Empty, string.Empty);
            BotListToken = string.Empty;
            CleverbotApiKey = string.Empty;
            RedisOptions = "redis:6379,syncTimeout=30000,responseTimeout=30000,allowAdmin=true,password=";
            Db = new DbOptions();
            Version = 1;
        }

        [Comment(@"Bot token. Do not share with anyone ever -> https://discordapp.com/developers/applications/")]
        public string Token { get; }

        [Comment(@"List of Ids of the users who have bot owner permissions
**DO NOT ADD PEOPLE YOU DON'T TRUST**")]
        public HashSet<ulong> OwnerIds { get; }
        
        [Comment(@"The number of shards that the bot will running on.
Leave at 1 if you don't know what you're doing.")]
        public int TotalShards { get; }
        
        [Comment(@"Login to https://console.cloud.google.com, create a new project, go to APIs & Services -> Library -> YouTube Data API and enable it.
Then, go to APIs and Services -> Credentials and click Create credentials -> API key.
Used only for Youtube Data Api (at the moment).")]
        public string GoogleApiKey { get; }
        
        [Comment(@"Settings for voting system for discordbots. Meant for use on global Nadeko.")]
        public VotesSettings Votes { get; }
        
        [Comment(@"Patreon auto reward system settings.
go to https://www.patreon.com/portal -> my clients -> create client")]
        public PatreonSettings Patreon { get; }
        
        [Comment(@"Api key for sending stats to DiscordBotList.")]
        public string BotListToken { get; }
        
        [Comment(@"Official cleverbot api key.")]
        public string CleverbotApiKey { get; }
        
        [Comment(@"Redis connection string. Don't change if you don't know what you're doing.")]
        public string RedisOptions { get; }
        
        [Comment(@"Database options. Don't change if you don't know what you're doing. Leave null for default values")]
        public DbOptions Db { get; }
        
        [Comment(@"DO NOT CHANGE")]
        public int Version { get; }

        public class DbOptions
        {
            [Comment(@"Database type. Only sqlite supported atm")]
            public string Type { get; } = "";
            [Comment(@"Connection string. Will default to ""Data Source=data/NadekoBot.db""")]
            public string ConnectionString { get; } = string.Empty;
         }

        public sealed record PatreonSettings
        {
            [Comment(@"")]
            public string AccessToken { get; }
            [Comment(@"")]
            public string RefreshToken { get; }
            [Comment(@"")]
            public string ClientSecret { get; }

            [Comment(@"Campaign ID of your patreon page. Go to your patreon page (make sure you're logged in) and type ""prompt('Campaign ID', window.patreon.bootstrap.creator.data.id);"" in the console. (ctrl + shift + i)")]
            public string CampaignId { get; }

            public PatreonSettings(string accessToken, string refreshToken, string clientSecret, string campaignId)
            {
                AccessToken = accessToken;
                RefreshToken = refreshToken;
                ClientSecret = clientSecret;
                CampaignId = campaignId;
            }
        }

        public sealed record VotesSettings
        {
            [Comment(@"")]
            public string Url { get; }
            [Comment(@"")]
            public string Key { get; }

            public VotesSettings(string url, string key)
            {
                Url = url;
                Key = key;
            }
        }

        public class Old
        {
            public string Token { get; } = string.Empty;
            public ulong[] OwnerIds { get; } = new ulong[1];
            public string LoLApiKey { get; } = string.Empty;
            public string GoogleApiKey { get; } = string.Empty;
            public string MashapeKey { get; } = string.Empty;
            public string OsuApiKey { get; } = string.Empty;
            public string SoundCloudClientId { get; } = string.Empty;
            public string CleverbotApiKey { get; } = string.Empty;
            public string CarbonKey { get; } = string.Empty;
            public int TotalShards { get; } = 1;
            public string PatreonAccessToken { get; } = string.Empty;
            public string PatreonCampaignId { get; } = "334038";
            public RestartConfig? RestartCommand { get; } = null;

            public string ShardRunCommand { get; } = string.Empty;
            public string ShardRunArguments { get; } = string.Empty;
            public int? ShardRunPort { get; } = null;
            public string MiningProxyUrl { get; } = string.Empty;
            public string MiningProxyCreds { get; } = string.Empty;

            public string BotListToken { get; } = string.Empty;
            public string TwitchClientId { get; } = string.Empty;
            public string VotesToken { get; } = string.Empty;
            public string VotesUrl { get; } = string.Empty;
            public string RedisOptions { get; } = string.Empty;

            public class RestartConfig
            {
                public RestartConfig(string cmd, string args)
                {
                    this.Cmd = cmd;
                    this.Args = args;
                }

                public string Cmd { get; }
                public string Args { get; }
            }
        }
    }
}
