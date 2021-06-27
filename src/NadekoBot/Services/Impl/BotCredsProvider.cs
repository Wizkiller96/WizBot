using System;
using Microsoft.Extensions.Configuration;
using System.IO;
using Microsoft.Extensions.Primitives;
using NadekoBot.Common;
using NadekoBot.Common;
using NadekoBot.Common.Yml;
using Serilog;

namespace NadekoBot.Services
{
    // todo check why is memory usage so unstable
    public class BotCredsProvider
    {
        private readonly int? _totalShards;
        private const string _credsFileName = "creds.yml";
        private string CredsPath => Path.Combine(Directory.GetCurrentDirectory(), _credsFileName);
        private const string _credsExampleFileName = "creds_example.yml";
        private string CredsExamplePath => Path.Combine(Directory.GetCurrentDirectory(), _credsExampleFileName);
        
        private string _oldCredsJsonFilename = Path.Combine(Directory.GetCurrentDirectory(), "credentials.json");

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
                    Log.Error("Token is missing from credentials.json or Environment variables.\n" +
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
        
        public Creds GetCreds() => _creds;
    }
}