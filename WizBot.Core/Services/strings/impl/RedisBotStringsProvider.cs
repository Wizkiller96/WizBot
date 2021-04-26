using System.Linq;
using Discord.WebSocket;
using StackExchange.Redis;

namespace WizBot.Core.Services
{
    /// <summary>
    /// Uses <see cref="IStringsSource"/> to load strings into redis hash (only on Shard 0)
    /// and retrieves them from redis via <see cref="GetText"/> 
    /// </summary>
    public class RedisBotStringsProvider : IBotStringsProvider
    {
        private readonly ConnectionMultiplexer _redis;
        private readonly IStringsSource _source;

        public RedisBotStringsProvider(ConnectionMultiplexer redis, DiscordSocketClient discordClient, IStringsSource source)
        {
            _redis = redis;
            _source = source;

            if (discordClient.ShardId == 0)
                Reload();
        }

        public string GetText(string localeName, string key)
        {
            var value = _redis.GetDatabase().HashGet($"responses:{localeName}", key);
            return value;
        }

        public void Reload()
        {
            var redisDb = _redis.GetDatabase();
            foreach (var (localeName, localeStrings) in _source.GetResponseStrings())
            {
                var hashFields = localeStrings
                    .Select(x => new HashEntry(x.Key, x.Value))
                    .ToArray();

                redisDb.HashSet($"responses:{localeName}", hashFields);
            }
        }
    }
}