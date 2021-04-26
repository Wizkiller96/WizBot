using StackExchange.Redis;

namespace WizBot.Core.Services
{
    public class RedisBotStringsProvider : IBotStringsProvider
    {
        private readonly ConnectionMultiplexer _redis;

        public RedisBotStringsProvider(ConnectionMultiplexer redis)
        {
            _redis = redis;
            Reload();
        }

        public string GetText(string langName, string key)
        {
            var value = _redis.GetDatabase().StringGet(key);
            return value;
        }

        public void Reload()
        {

        }
    }
}
