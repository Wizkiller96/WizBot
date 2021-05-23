using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using WizBot.Core.Services.Database.Models;
using WizBot.Core.Services.Impl;
using Serilog;
using YoutubeExplode;

namespace WizBot.Modules.Music.Common.SongResolver.Strategies
{
    public class YoutubeResolveStrategy : IResolveStrategy
    {

        public YoutubeResolveStrategy()
        {
        }

        public async Task<SongInfo> ResolveSong(string query)
        {
            try
            {
                var s = await ResolveWithYtDl(query).ConfigureAwait(false);
                if (s != null)
                    return s;
            }
            catch (Exception ex) { Log.Warning(ex.ToString()); }

            try
            {
                return await ResolveWithYtExplode(query).ConfigureAwait(false);
            }
            catch (Exception ex) { Log.Warning(ex.ToString()); }
            return null;
        }

        private async Task<SongInfo> ResolveWithYtExplode(string query)
        {
            var client = new YoutubeClient();

            Log.Information("Searching for video");
            var videos = await client.Search.GetVideosAsync(query);

            var video = videos.FirstOrDefault();

            if (video == null)
                return null;

            Log.Information("Video found");
            var streamInfo = await client.Videos.Streams.GetManifestAsync(video.Id).ConfigureAwait(false);
            var stream = streamInfo
                .GetAudio()
                .OrderByDescending(x => x.Bitrate)
                .FirstOrDefault();

            Log.Information("Got stream info");

            if (stream == null)
                return null;

            return new SongInfo
            {
                Provider = "YouTube",
                ProviderType = MusicType.YouTube,
                Query = "https://youtube.com/watch?v=" + video.Id,
                Thumbnail = video.Thumbnails.MediumResUrl,
                TotalTime = video.Duration,
                Uri = async () =>
                {
                    await Task.Yield();
                    return stream.Url;
                },
                VideoId = video.Id,
                Title = video.Title,
            };
        }

        private async Task<SongInfo> ResolveWithYtDl(string query)
        {
            string[] data;
            try
            {
                var ytdl = new YtdlOperation();
                data = (await ytdl.GetDataAsync(query).ConfigureAwait(false)).Split('\n');

                if (data.Length < 6)
                {
                    Log.Information("No song found. Data less than 6");
                    return null;
                }

                if (!TimeSpan.TryParseExact(data[4],
                    new[] { "ss", "m\\:ss", "mm\\:ss", "h\\:mm\\:ss", "hh\\:mm\\:ss", "hhh\\:mm\\:ss" },
                    CultureInfo.InvariantCulture, out var time))
                    time = TimeSpan.FromHours(24);

                return new SongInfo()
                {
                    Title = data[0],
                    VideoId = data[1],
                    Uri = async () =>
                    {
                        var ytdlo = new YtdlOperation();
                        data = (await ytdlo.GetDataAsync(query).ConfigureAwait(false)).Split('\n');
                        if (data.Length < 6)
                        {
                            Log.Information("No song found. Data less than 6");
                            return null;
                        }

                        return data[2];
                    },
                    Thumbnail = data[3],
                    TotalTime = time,
                    Provider = "YouTube",
                    ProviderType = MusicType.YouTube,
                    Query = "https://youtube.com/watch?v=" + data[1],
                };
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error resolving with ytdl");
                return null;
            }
        }
    }
}
