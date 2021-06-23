using Microsoft.Extensions.Configuration;
using System.IO;
using Nadeko.Common;
using Serilog;

namespace NadekoBot.Services
{
    public static class BotCredentialsProvider
    {
        private const string _credsFileName = "creds.yml";
        private static string _oldCredsJsonFilename = Path.Combine(Directory.GetCurrentDirectory(), "credentials.json");
        
        public static Creds CreateBotCredentials()
        {
            if (!File.Exists(_credsFileName))
                Log.Warning($"{_credsFileName} is missing. " +
                            $"Attempting to load creds from environment variables prefixed with 'NadekoBot_'. " +
                            $"Example is in {Path.GetFullPath("./creds-example.yml")}");
            
            
            IConfigurationBuilder configBuilder = new ConfigurationBuilder();
            var creds = configBuilder
                .AddYamlFile(_credsFileName, false, true)
                .AddEnvironmentVariables("NadekoBot_")
                .Build()
                .Get<Creds>();

            // if(string.IsNullOrWhiteSpace(creds.RedisOptions))
            //     creds.RedisOptions = ""

            return creds;
            
            // try
            // {
            //     
            //
            //     var data = configBuilder.Build();
            //
            //     Token = data[nameof(Token)];
            //     if (string.IsNullOrWhiteSpace(Token))
            //     {
            //         Log.Error("Token is missing from credentials.json or Environment variables. Add it and restart the program.");
            //         Helpers.ReadErrorAndExit(5);
            //     }
            //
            //     OwnerIds = data.GetSection("OwnerIds").GetChildren().Select(c => ulong.Parse(c.Value))
            //         .ToImmutableArray();
            //     GoogleApiKey = data[nameof(GoogleApiKey)];
            //     MashapeKey = data[nameof(MashapeKey)];
            //     OsuApiKey = data[nameof(OsuApiKey)];
            //     PatreonAccessToken = data[nameof(PatreonAccessToken)];
            //     PatreonCampaignId = data[nameof(PatreonCampaignId)] ?? "334038";
            //     ShardRunCommand = data[nameof(ShardRunCommand)];
            //     ShardRunArguments = data[nameof(ShardRunArguments)];
            //     CleverbotApiKey = data[nameof(CleverbotApiKey)];
            //     LocationIqApiKey = data[nameof(LocationIqApiKey)];
            //     TimezoneDbApiKey = data[nameof(TimezoneDbApiKey)];
            //     CoinmarketcapApiKey = data[nameof(CoinmarketcapApiKey)];
            //     if (string.IsNullOrWhiteSpace(CoinmarketcapApiKey))
            //     {
            //         CoinmarketcapApiKey = "e79ec505-0913-439d-ae07-069e296a6079";
            //     }
            //
            //     if (!string.IsNullOrWhiteSpace(data[nameof(RedisOptions)]))
            //         RedisOptions = data[nameof(RedisOptions)];
            //     else
            //         RedisOptions = "127.0.0.1,syncTimeout=3000";
            //
            //     VotesToken = data[nameof(VotesToken)];
            //     VotesUrl = data[nameof(VotesUrl)];
            //     BotListToken = data[nameof(BotListToken)];
            //
            //     var restartSection = data.GetSection(nameof(RestartCommand));
            //     var cmd = restartSection["cmd"];
            //     var args = restartSection["args"];
            //     if (!string.IsNullOrWhiteSpace(cmd))
            //         RestartCommand = new RestartConfig(cmd, args);
            //
            //     if (Environment.OSVersion.Platform == PlatformID.Unix)
            //     {
            //         if (string.IsNullOrWhiteSpace(ShardRunCommand))
            //             ShardRunCommand = "dotnet";
            //         if (string.IsNullOrWhiteSpace(ShardRunArguments))
            //             ShardRunArguments = "run -c Release --no-build -- {0} {1}";
            //     }
            //     else //windows
            //     {
            //         if (string.IsNullOrWhiteSpace(ShardRunCommand))
            //             ShardRunCommand = "NadekoBot.exe";
            //         if (string.IsNullOrWhiteSpace(ShardRunArguments))
            //             ShardRunArguments = "{0} {1}";
            //     }
            //
            //     if (!int.TryParse(data[nameof(TotalShards)], out var ts))
            //         ts = 0;
            //     TotalShards = ts < 1 ? 1 : ts;
            //
            //     CarbonKey = data[nameof(CarbonKey)];
            //     var dbSection = data.GetSection("db");
            //     Db = new DBConfig(@"sqlite",
            //         string.IsNullOrWhiteSpace(dbSection["ConnectionString"])
            //             ? "Data Source=data/NadekoBot.db"
            //             : dbSection["ConnectionString"]);
            //
            //     TwitchClientId = data[nameof(TwitchClientId)];
            //     if (string.IsNullOrWhiteSpace(TwitchClientId))
            //     {
            //         TwitchClientId = "67w6z9i09xv2uoojdm9l0wsyph4hxo6";
            //     }
            // }
            // catch (Exception ex)
            // {
            //     Log.Error("JSON serialization has failed. Fix your credentials file and restart the bot.");
            //     Log.Fatal(ex.ToString());
            //     Helpers.ReadErrorAndExit(6);
            // }
        }
    }
}