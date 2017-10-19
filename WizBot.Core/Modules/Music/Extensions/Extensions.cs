using WizBot.Modules.Music.Common;
using WizBot.Core.Services.Database.Models;
using WizBot.Core.Services.Impl;
using System;
using System.Threading.Tasks;

namespace WizBot.Modules.Music.Extensions
{
    public static class Extensions
    {
        public static Task<SongInfo> GetSongInfo(this SoundCloudVideo svideo) =>
            Task.FromResult(new SongInfo
            {
                Title = svideo.FullName,
                Provider = "SoundCloud",
                Uri = () => svideo.StreamLink(),
                ProviderType = MusicType.Soundcloud,
                Query = svideo.TrackLink,
                Thumbnail = svideo.artwork_url,
                TotalTime = TimeSpan.FromMilliseconds(svideo.Duration)
            });
    }
}
