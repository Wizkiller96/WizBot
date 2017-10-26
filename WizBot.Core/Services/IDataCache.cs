using StackExchange.Redis;
using System;
using System.Threading.Tasks;

namespace WizBot.Core.Services
{
    public interface IDataCache
    {
        ConnectionMultiplexer Redis { get; }
        Task<(bool Success, byte[] Data)> TryGetImageDataAsync(string key);
        Task<(bool Success, string Data)> TryGetAnimeDataAsync(string key);
        Task SetImageDataAsync(string key, byte[] data);
        Task SetAnimeDataAsync(string link, string data);
        TimeSpan? AddTimelyClaim(ulong id, int period);
    }
}
