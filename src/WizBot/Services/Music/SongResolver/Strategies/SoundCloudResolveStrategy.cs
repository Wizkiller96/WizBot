using WizBot.Services.Impl;
using WizBot.Services.Music.Extensions;
using System.Threading.Tasks;

namespace WizBot.Services.Music.SongResolver.Strategies
{
    public class SoundcloudResolveStrategy : IResolveStrategy
    {
        private readonly SoundCloudApiService _sc;

        public SoundcloudResolveStrategy(SoundCloudApiService sc)
        {
            _sc = sc;
        }

        public async Task<SongInfo> ResolveSong(string query)
        {
            var svideo = !_sc.IsSoundCloudLink(query) ?
                await _sc.GetVideoByQueryAsync(query).ConfigureAwait(false) :
                await _sc.ResolveVideoAsync(query).ConfigureAwait(false);

            if (svideo == null)
                return null;
            return await svideo.GetSongInfo();
        }
    }
}