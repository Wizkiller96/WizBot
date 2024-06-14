#nullable disable
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using WizBot.Common.Yml;
using Newtonsoft.Json;

namespace WizBot.Services;

public sealed class BotCredsProvider : IBotCredsProvider
{
    private const string CREDS_FILE_NAME = "creds.yml";
    private const string CREDS_EXAMPLE_FILE_NAME = "creds_example.yml";

    private string CredsPath { get; }

    private string CredsExamplePath { get; }

    private readonly int? _totalShards;


    private readonly Creds _creds = new();
    private readonly IConfigurationRoot _config;


    private readonly object _reloadLock = new();
    private readonly IDisposable _changeToken;

    public BotCredsProvider(int? totalShards = null, string credPath = null)
    {
        _totalShards = totalShards;

        if (!string.IsNullOrWhiteSpace(credPath))
        {
            CredsPath = credPath;
            CredsExamplePath = Path.Combine(Path.GetDirectoryName(credPath), CREDS_EXAMPLE_FILE_NAME);
        }
        else
        {
            CredsPath = Path.Combine(Directory.GetCurrentDirectory(), CREDS_FILE_NAME);
            CredsExamplePath = Path.Combine(Directory.GetCurrentDirectory(), CREDS_EXAMPLE_FILE_NAME);
        }

        try
        {
            if (!File.Exists(CredsExamplePath))
                File.WriteAllText(CredsExamplePath, Yaml.Serializer.Serialize(_creds));
        }
        catch
        {
            // this can fail in docker containers
        }

        MigrateCredentials();

        if (!File.Exists(CredsPath))
        {
            Log.Warning(
                "{CredsPath} is missing. Attempting to load creds from environment variables prefixed with 'WizBot_'. Example is in {CredsExamplePath}",
                CredsPath,
                CredsExamplePath);
        }

        try
        {
            _config = new ConfigurationBuilder().AddYamlFile(CredsPath, false, true)
                                                .AddEnvironmentVariables("WizBot_")
                                                .Build();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }

        _changeToken = ChangeToken.OnChange(() => _config.GetReloadToken(), Reload);
        Reload();
    }

    public void Reload()
    {
        lock (_reloadLock)
        {
            _creds.OwnerIds.Clear();
            _creds.AdminIds.Clear();
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
                    _creds.RestartCommand = new RestartConfig()
                    {
                        Args = "dotnet",
                        Cmd = "WizBot.dll -- {0}"
                    };
                }
                else
                {
                    _creds.RestartCommand = new RestartConfig()
                    {
                        Args = "WizBot.exe",
                        Cmd = "{0}"
                    };
                }
            }

            if (string.IsNullOrWhiteSpace(_creds.RedisOptions))
                _creds.RedisOptions = "127.0.0.1,syncTimeout=3000";

            // replace the old generated key with the shared key
            if (string.IsNullOrWhiteSpace(_creds.CoinmarketcapApiKey)
                || _creds.CoinmarketcapApiKey.StartsWith("e79ec505-0913"))
                _creds.CoinmarketcapApiKey = "3077537c-7dfb-4d97-9a60-56fc9a9f5035";

            _creds.TotalShards = _totalShards ?? _creds.TotalShards;
        }
    }

    public void ModifyCredsFile(Action<IBotCredentials> func)
    {
        var ymlData = File.ReadAllText(CREDS_FILE_NAME);
        var creds = Yaml.Deserializer.Deserialize<Creds>(ymlData);

        func(creds);

        ymlData = Yaml.Serializer.Serialize(creds);
        File.WriteAllText(CREDS_FILE_NAME, ymlData);
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
                AdminIds = oldCreds.AdminIds.Distinct().ToHashSet(),
                GoogleApiKey = oldCreds.GoogleApiKey,
                RapidApiKey = oldCreds.MashapeKey,
                OsuApiKey = oldCreds.OsuApiKey,
                CleverbotApiKey = oldCreds.CleverbotApiKey,
                TotalShards = oldCreds.TotalShards <= 1 ? 1 : oldCreds.TotalShards,
                Patreon = new Creds.PatreonSettings(oldCreds.PatreonAccessToken, null, null, oldCreds.PatreonCampaignId),
                Votes = new Creds.VotesSettings(oldCreds.VotesUrl, oldCreds.VotesToken, string.Empty, string.Empty),
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
            if (creds.Version <= 5)
            {
                creds.BotCache = BotCacheImplemenation.Redis;
            }
            if (creds.Version <= 6)
            {
                creds.Version = 7;
                File.WriteAllText(CREDS_FILE_NAME, Yaml.Serializer.Serialize(creds));
            }
            
            if (creds.Version <= 7)
            {
                creds.Version = 8;
                File.WriteAllText(CREDS_FILE_NAME, Yaml.Serializer.Serialize(creds));
            }
        }
    }

    public IBotCredentials GetCreds()
    {
        lock (_reloadLock)
        {
            return _creds;
        }
    }
}