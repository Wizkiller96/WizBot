using System;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Primitives;
using NadekoBot.Common;
using NadekoBot.Common.Yml;
using Newtonsoft.Json;
using Serilog;

namespace NadekoBot.Services
{
    public sealed class BotCredsProvider
    {
        private readonly int? _totalShards;
        private const string _credsFileName = "creds.yml";
        private const string _credsExampleFileName = "creds_example.yml";
        
        private string CredsPath => Path.Combine(Directory.GetCurrentDirectory(), _credsFileName);
        private string CredsExamplePath => Path.Combine(Directory.GetCurrentDirectory(), _credsExampleFileName);
        private string OldCredsJsonPath => Path.Combine(Directory.GetCurrentDirectory(), "credentials.json");
        private string OldCredsJsonBackupPath => Path.Combine(Directory.GetCurrentDirectory(), "credentials.json.bak");
        

        private Creds _creds = new Creds();
        private IConfigurationRoot _config;
        

        private readonly object reloadLock = new object();
        private void Reload()
        {
            lock (reloadLock)
            {
                _creds.OwnerIds.Clear();
                _config.Bind(_creds);
                
                if (string.IsNullOrWhiteSpace(_creds.Token))
                {
                    Log.Error("Token is missing from creds.yml or Environment variables.\n" +
                              "Add it and restart the program.");
                    Helpers.ReadErrorAndExit(5);
                    return;
                }
                
                if (string.IsNullOrWhiteSpace(_creds.RestartCommand?.Cmd)
                    || string.IsNullOrWhiteSpace(_creds.RestartCommand?.Args))
                {
                    if (Environment.OSVersion.Platform == PlatformID.Unix)
                    {
                        _creds.RestartCommand = new RestartConfig()
                        {
                            Args = "dotnet",
                            Cmd = "NadekoBot.dll -- {0}",
                        };
                    }
                    else
                    {
                        _creds.RestartCommand = new RestartConfig()
                        {
                            Args = "NadekoBot.exe",
                            Cmd = "{0}",
                        };
                    }
                }
                
                if (string.IsNullOrWhiteSpace(_creds.RedisOptions))
                    _creds.RedisOptions = "127.0.0.1,syncTimeout=3000";
                
                if (string.IsNullOrWhiteSpace(_creds.CoinmarketcapApiKey))
                    _creds.CoinmarketcapApiKey = "e79ec505-0913-439d-ae07-069e296a6079";

                _creds.TotalShards = _totalShards ?? _creds.TotalShards;
            }
        }

        public BotCredsProvider(int? totalShards = null)
        {
            _totalShards = totalShards;
            if (!File.Exists(CredsExamplePath))
            {
                File.WriteAllText(CredsExamplePath, Yaml.Serializer.Serialize(_creds));
            }
             
            MigrateCredentials();
            
            if (!File.Exists(CredsPath))
            {
                Log.Warning($"{CredsPath} is missing. " +
                            $"Attempting to load creds from environment variables prefixed with 'NadekoBot_'. " +
                            $"Example is in {CredsExamplePath}");
            }
            
            _config = new ConfigurationBuilder()
                .AddYamlFile(CredsPath, false, true)
                .AddEnvironmentVariables("NadekoBot_")
                .Build();
            
            ChangeToken.OnChange(
                () => _config.GetReloadToken(),
                Reload);
            
            Reload();
        }

        /// <summary>
        /// Checks if there's a V2 credentials file present, loads it if it exists,
        /// converts it to new model, and saves it to YAML. Also backs up old credentials to credentials.json.bak
        /// </summary>
        private void MigrateCredentials()
        {
            if (File.Exists(OldCredsJsonPath))
            {
                Log.Information("Migrating old creds...");
                var jsonCredentialsFileText = File.ReadAllText(OldCredsJsonPath);
                var oldCreds = JsonConvert.DeserializeObject<Creds.Old>(jsonCredentialsFileText);

                var creds = new Creds
                {
                    Version = 1,
                    Token = oldCreds.Token,
                    OwnerIds = oldCreds.OwnerIds.Distinct().ToHashSet(),
                    GoogleApiKey = oldCreds.GoogleApiKey,
                    RapidApiKey = oldCreds.MashapeKey,
                    OsuApiKey = oldCreds.OsuApiKey,
                    CleverbotApiKey = oldCreds.CleverbotApiKey,
                    TotalShards = oldCreds.TotalShards <= 1 ? 1 : oldCreds.TotalShards,
                    Patreon = new Creds.PatreonSettings(oldCreds.PatreonAccessToken,
                        null,
                        null,
                        oldCreds.PatreonCampaignId),
                    Votes = new(oldCreds.VotesUrl,
                        oldCreds.VotesToken,
                        string.Empty,
                        string.Empty),
                    BotListToken = oldCreds.BotListToken,
                    RedisOptions = oldCreds.RedisOptions,
                    LocationIqApiKey = oldCreds.LocationIqApiKey,
                    TimezoneDbApiKey = oldCreds.TimezoneDbApiKey,
                    CoinmarketcapApiKey = oldCreds.CoinmarketcapApiKey,
                };

                File.Move(OldCredsJsonPath, OldCredsJsonBackupPath, true);
                File.WriteAllText(CredsPath, Yaml.Serializer.Serialize(creds));

                Log.Warning("Data from credentials.json has been moved to creds.yml\nPlease inspect your creds.yml for correctness");
            }

            if (File.Exists(_credsFileName))
            {
                var creds = Yaml.Deserializer.Deserialize<Creds>(File.ReadAllText(_credsFileName));
                if (creds.Version <= 1)
                {
                    creds.Version = 2;
                    File.WriteAllText(_credsFileName, Yaml.Serializer.Serialize(creds));
                }
            }

        }

        public Creds GetCreds() => _creds;
    }
}