using Microsoft.Extensions.Configuration;
using System.IO;
using Microsoft.Extensions.Primitives;
using Nadeko.Common;
using NadekoBot.Common.Yml;
using Serilog;

namespace NadekoBot.Services
{
    // todo check why is memory usage so unstable
    public class BotCredsProvider
    {
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
                
                // todo load defaults for restart command, redis, and some others maybe?
            }
        }

        public BotCredsProvider()
        {
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