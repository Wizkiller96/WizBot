using System.Collections.Generic;
using NadekoBot.Common.Yml;
using YamlDotNet.Serialization;

namespace NadekoBot.Common
{
    public sealed class Creds : IBotCredentials
    {
        public Creds()
        {
            Version = 1;
            Token = string.Empty;
            OwnerIds = new List<ulong>();
            TotalShards = 1;
            GoogleApiKey = string.Empty;
            Votes = new(string.Empty, string.Empty, string.Empty, string.Empty);
            Patreon = new(string.Empty, string.Empty, string.Empty, string.Empty);
            BotListToken = string.Empty;
            CleverbotApiKey = string.Empty;
            RedisOptions = "localhost:6379,syncTimeout=30000,responseTimeout=30000,allowAdmin=true,password=";
            Db = new()
            {
                Type = "sqlite",
                ConnectionString = "Data Source=data/NadekoBot.db"
            };

            CoordinatorUrl = "http://localhost:3442";

            RestartCommand = new()
            {
            };
        }

        [Comment(@"DO NOT CHANGE")]
        public int Version { get; set; }
        
        [Comment(@"Bot token. Do not share with anyone ever -> https://discordapp.com/developers/applications/")]
        public string Token { get; set; }

        [Comment(@"List of Ids of the users who have bot owner permissions
**DO NOT ADD PEOPLE YOU DON'T TRUST**")]
        public ICollection<ulong> OwnerIds { get; set; }
        
        [Comment(@"The number of shards that the bot will running on.
Leave at 1 if you don't know what you're doing.")]
        public int TotalShards { get; set; }
        
        [Comment(@"Login to https://console.cloud.google.com, create a new project, go to APIs & Services -> Library -> YouTube Data API and enable it.
Then, go to APIs and Services -> Credentials and click Create credentials -> API key.
Used only for Youtube Data Api (at the moment).")]
        public string GoogleApiKey { get; set; }
        
        [Comment(@"Settings for voting system for discordbots. Meant for use on global Nadeko.")]
        public VotesSettings Votes { get; set; }
        
        [Comment(@"Patreon auto reward system settings.
go to https://www.patreon.com/portal -> my clients -> create client")]
        public PatreonSettings Patreon { get; set; }
        
        [Comment(@"Api key for sending stats to DiscordBotList.")]
        public string BotListToken { get; set; }
        
        [Comment(@"Official cleverbot api key.")]
        public string CleverbotApiKey { get; set; }
        
        [Comment(@"Redis connection string. Don't change if you don't know what you're doing.")]
        public string RedisOptions { get; set; }
        
        [Comment(@"Database options. Don't change if you don't know what you're doing. Leave null for default values")]
        public DbOptions Db { get; set; }

        [Comment(@"Address and port of the coordinator endpoint. Leave empty for default.
Change only if you've changed the coordinator address or port.")]
        public string CoordinatorUrl { get; set; }
        
        [YamlIgnore]
        public string PatreonCampaignId => Patreon?.CampaignId;
        [YamlIgnore]
        public string PatreonAccessToken => Patreon?.AccessToken;
        
        [Comment(@"Api key obtained on https://rapidapi.com (go to MyApps -> Add New App -> Enter Name -> Application key)")]
        public string RapidApiKey { get; set; }

        [Comment(@"https://locationiq.com api key (register and you will receive the token in the email).
Used only for .time command.")]
        public string LocationIqApiKey { get; set; }

        [Comment(@"https://timezonedb.com api key (register and you will receive the token in the email).
Used only for .time command")]
        public string TimezoneDbApiKey { get; set; }

        [Comment(@"https://pro.coinmarketcap.com/account/ api key. There is a free plan for personal use.
Used for cryptocurrency related commands.")]
        public string CoinmarketcapApiKey { get; set; }
        
        [Comment(@"Api key used for Osu related commands. Obtain this key at https://osu.ppy.sh/p/api")]
        public string OsuApiKey { get; set; }

        [Comment(@"Command and args which will be used to restart the bot.
Only used if bot is executed directly (NOT through the coordinator)
placeholders: 
    {0} -> shard id 
    {1} -> total shards
Linux default
    cmd: dotnet
    args: ""NadekoBot.dll -- {0}""
Windows default
    cmd: NadekoBot.exe
    args: {0}")]
        public RestartConfig RestartCommand { get; set; }


        public class DbOptions
        {
            [Comment(@"Database type. Only sqlite supported atm")]
            public string Type { get; set; }
            [Comment(@"Connection string. Will default to ""Data Source=data/NadekoBot.db""")]
            public string ConnectionString { get; set; }
         }

        // todo fixup patreon
        public sealed record PatreonSettings
        {
            [Comment(@"Access token. You have to manually update this 1st of each month by refreshing the token on https://patreon.com/portal")]
            public string AccessToken { get; set; }
            [Comment(@"Unused atm")]
            public string RefreshToken { get; set; }
            [Comment(@"Unused atm")]
            public string ClientSecret { get; set; }

            [Comment(@"Campaign ID of your patreon page. Go to your patreon page (make sure you're logged in) and type ""prompt('Campaign ID', window.patreon.bootstrap.creator.data.id);"" in the console. (ctrl + shift + i)")]
            public string CampaignId { get; set; }

            public PatreonSettings(string accessToken, string refreshToken, string clientSecret, string campaignId)
            {
                AccessToken = accessToken;
                RefreshToken = refreshToken;
                ClientSecret = clientSecret;
                CampaignId = campaignId;
            }

            public PatreonSettings()
            {
                
            }
        }

        public sealed record VotesSettings
        {
            [Comment(@"top.gg votes service url
This is the url of your instance of the NadekoBot.Votes api
Example: https://votes.my.cool.bot.com")]
            public string TopggServiceUrl { get; set; }
            
            [Comment(@"Authorization header value sent to the TopGG service url with each request
This should be equivalent to the TopggKey in your NadekoBot.Votes api appsettings.json file")]
            public string TopggKey { get; set; }
            
            [Comment(@"discords.com votes service url
This is the url of your instance of the NadekoBot.Votes api
Example: https://votes.my.cool.bot.com")]
            public string DiscordsServiceUrl { get; set; }
            
            [Comment(@"Authorization header value sent to the Discords service url with each request
This should be equivalent to the DiscordsKey in your NadekoBot.Votes api appsettings.json file")]
            public string DiscordsKey { get; set; }

            public VotesSettings()
            {
                
            }
            
            public VotesSettings(string topggServiceUrl, string topggKey, string discordsServiceUrl, string discordsKey)
            {
                TopggServiceUrl = topggServiceUrl;
                TopggKey = topggKey;
                DiscordsServiceUrl = discordsServiceUrl;
                DiscordsKey = discordsKey;
            }
        }

        public class Old
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
            public RestartConfig RestartCommand { get; set; } = null;

            public string ShardRunCommand { get; set; } = string.Empty;
            public string ShardRunArguments { get; set; } = string.Empty;
            public int? ShardRunPort { get; set; } = null;
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
                public RestartConfig(string cmd, string args)
                {
                    this.Cmd = cmd;
                    this.Args = args;
                }

                public string Cmd { get; set; }
                public string Args { get; set; }
            }
        }
    }
}
