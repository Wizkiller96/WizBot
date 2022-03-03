#nullable disable
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using NadekoBot.Common.Yml;
using Newtonsoft.Json;

namespace NadekoBot.Services;

public interface IBotCredsProvider
{
    public void Reload();
    public IBotCredentials GetCreds();
    public void ModifyCredsFile(Action<Creds> func);
}

public sealed class BotCredsProvider : IBotCredsProvider
{
    private const string CREDS_FILE_NAME = "creds.yml";
    private const string CREDS_EXAMPLE_FILE_NAME = "creds_example.yml";

    private string CredsPath
        => Path.Combine(Directory.GetCurrentDirectory(), CREDS_FILE_NAME);

    private string CredsExamplePath
        => Path.Combine(Directory.GetCurrentDirectory(), CREDS_EXAMPLE_FILE_NAME);

    private readonly int? _totalShards;


    private readonly Creds _creds = new();
    private readonly IConfigurationRoot _config;


    private readonly object _reloadLock = new();

    public BotCredsProvider(int? totalShards = null)
    {
        _totalShards = totalShards;
        if (!File.Exists(CredsExamplePath))
            File.WriteAllText(CredsExamplePath, Yaml.Serializer.Serialize(_creds));

        MigrateCredentials();

        if (!File.Exists(CredsPath))
        {
            Log.Warning(
                "{CredsPath} is missing. Attempting to load creds from environment variables prefixed with 'NadekoBot_'. Example is in {CredsExamplePath}",
                CredsPath,
                CredsExamplePath);
        }

        _config = new ConfigurationBuilder().AddYamlFile(CredsPath, false, true)
                                            .AddEnvironmentVariables("NadekoBot_")
                                            .Build();

        ChangeToken.OnChange(() => _config.GetReloadToken(), Reload);

        Reload();
    }

    public void Reload()
    {
        lock (_reloadLock)
        {
            _creds.OwnerIds.Clear();
            _config.Bind(_creds);

            if (string.IsNullOrWhiteSpace(_creds.Token))
            {
                Log.Error("Token is missing from creds.yml or Environment variables.\nAdd it and restart the program");
                Helpers.ReadErrorAndExit(5);
                return;
            }

            if (string.IsNullOrWhiteSpace(_creds.RestartCommand?.Cmd)
                || string.IsNullOrWhiteSpace(_creds.RestartCommand?.Args))
            {
                if (Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    _creds.RestartCommand = new()
                    {
                        Args = "dotnet",
                        Cmd = "NadekoBot.dll -- {0}"
                    };
                }
                else
                {
                    _creds.RestartCommand = new()
                    {
                        Args = "NadekoBot.exe",
                        Cmd = "{0}"
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

    public void ModifyCredsFile(Action<Creds> func)
    {
        var ymlData = File.ReadAllText(CREDS_FILE_NAME);
        var creds = Yaml.Deserializer.Deserialize<Creds>(ymlData);

        func(creds);

        ymlData = Yaml.Serializer.Serialize(creds);
        File.WriteAllText(CREDS_FILE_NAME, ymlData);

        Reload();
    }
    
    private string OldCredsJsonPath
        => Path.Combine(Directory.GetCurrentDirectory(), "credentials.json");

    private string OldCredsJsonBackupPath
        => Path.Combine(Directory.GetCurrentDirectory(), "credentials.json.bak");
    
    private void MigrateCredentials()
    {
        if (File.Exists(OldCredsJsonPath))
        {
            Log.Information("Migrating old creds...");
            var jsonCredentialsFileText = File.ReadAllText(OldCredsJsonPath);
            var oldCreds = JsonConvert.DeserializeObject<OldCreds>(jsonCredentialsFileText);

            if (oldCreds is null)
            {
                Log.Error("Error while reading old credentials file. Make sure that the file is formatted correctly");
                return;
            }

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
                Patreon = new(oldCreds.PatreonAccessToken, null, null, oldCreds.PatreonCampaignId),
                Votes = new(oldCreds.VotesUrl, oldCreds.VotesToken, string.Empty, string.Empty),
                BotListToken = oldCreds.BotListToken,
                RedisOptions = oldCreds.RedisOptions,
                LocationIqApiKey = oldCreds.LocationIqApiKey,
                TimezoneDbApiKey = oldCreds.TimezoneDbApiKey,
                CoinmarketcapApiKey = oldCreds.CoinmarketcapApiKey
            };

            File.Move(OldCredsJsonPath, OldCredsJsonBackupPath, true);
            File.WriteAllText(CredsPath, Yaml.Serializer.Serialize(creds));

            Log.Warning(
                "Data from credentials.json has been moved to creds.yml\nPlease inspect your creds.yml for correctness");
        }
        
        if (File.Exists(CREDS_FILE_NAME))
        {
            var creds = Yaml.Deserializer.Deserialize<Creds>(File.ReadAllText(CREDS_FILE_NAME));
            if (creds.Version <= 3)
            {
                creds.Version = 4;
                File.WriteAllText(CREDS_FILE_NAME, Yaml.Serializer.Serialize(creds));
            }
        }
    }

    public IBotCredentials GetCreds()
        => _creds;
}