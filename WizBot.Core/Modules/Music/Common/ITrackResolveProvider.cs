#nullable enable
using System.Threading.Tasks;

namespace WizBot.Core.Modules.Music
{
    public interface ITrackResolveProvider
    {
        Task<ITrackInfo?> QuerySongAsync(string query, MusicPlatform? forcePlatform);
    }
}