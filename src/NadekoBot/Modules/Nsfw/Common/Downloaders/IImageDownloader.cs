using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Nsfw.Common
{
    public interface IImageDownloader
    {
        Task<List<ImageData>> DownloadImageDataAsync(string[] tags, int page = 0,
            bool isExplicit = false, CancellationToken cancel = default);
    }
}