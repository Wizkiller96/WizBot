#nullable disable
using NadekoBot.Common.Yml;

namespace NadekoBot.Common;

public sealed class Creds : IBotCredentials
{
    [Comment(@"DO NOT CHANGE")]
    public int Version { get; set; }

    [Comment(@"Bot token. Do not share with anyone ever -> https://discordapp.com/developers/applications/")]
    public string Token { get; set; }

    [Comment(@"List of Ids of the users who have bot owner permissions
**DO NOT ADD PEOPLE YOU DON'T TRUST**")]
    public ICollection<ulong> OwnerIds { get; set; }
    
    [Comment("Keep this on 'true' unless you're sure your bot shouldn't use privileged intents or you're waiting to be accepted")]
    public bool UsePrivilegedIntents { get; set; }

    [Comment(@"The number of shards that the bot will running on.
Leave at 1 if you don't know what you're doing.")]
    public int TotalShards { get; set; }

    [Comment(   
        @"Login to https://console.cloud.google.com, create a new project, go to APIs & Services -> Library -> YouTube Data API and enable it.
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

    [Comment(
        @"Api key obtained on https://rapidapi.com (go to MyApps -> Add New App -> Enter Name -> Application key)")]
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
    
//     [Comment(@"https://polygon.io/dashboard/api-keys api key. Free plan allows for 5 queries per minute.
// Used for stocks related commands.")]
//     public string PolygonIoApiKey { get; set; }

    [Comment(@"Api key used for Osu related commands. Obtain this key at https://osu.ppy.sh/p/api")]
    public string OsuApiKey { get; set; }

    [Comment(@"Optional Trovo client id.
You should use this if Trovo stream notifications stopped working or you're getting ratelimit errors.")]
    public string TrovoClientId { get; set; }

    [Comment(@"Obtain by creating an application at https://dev.twitch.tv/console/apps")]
    public string TwitchClientId { get; set; }

    [Comment(@"Obtain by creating an application at https://dev.twitch.tv/console/apps")]
    public string TwitchClientSecret { get; set; }

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

    public Creds()
    {
        Version = 5;
        Token = string.Empty;
        UsePrivilegedIntents = true;
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

        RestartCommand = new();
    }


    public class DbOptions
    {
        [Comment(@"Database type. ""sqlite"", ""mysql"" and ""postgresql"" are supported.
Default is ""sqlite""")]
        public string Type { get; set; }

        [Comment(@"Database connection string.
You MUST change this if you're not using ""sqlite"" type.
Default is ""Data Source=data/NadekoBot.db""
Example for mysql: ""Server=localhost;Port=3306;Uid=root;Pwd=my_super_secret_mysql_password;Database=nadeko""
Example for postgresql: ""Server=localhost;Port=5432;User Id=postgres;Password=my_super_secret_postgres_password;Database=nadeko;""")]
        public string ConnectionString { get; set; }
    }

    public sealed record PatreonSettings
    {
        public string ClientId { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public string ClientSecret { get; set; }

        [Comment(
            @"Campaign ID of your patreon page. Go to your patreon page (make sure you're logged in) and type ""prompt('Campaign ID', window.patreon.bootstrap.creator.data.id);"" in the console. (ctrl + shift + i)")]
        public string CampaignId { get; set; }

        public PatreonSettings(
            string accessToken,
            string refreshToken,
            string clientSecret,
            string campaignId)
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

        public VotesSettings(
            string topggServiceUrl,
            string topggKey,
            string discordsServiceUrl,
            string discordsKey)
        {
            TopggServiceUrl = topggServiceUrl;
            TopggKey = topggKey;
            DiscordsServiceUrl = discordsServiceUrl;
            DiscordsKey = discordsKey;
        }
    }
}