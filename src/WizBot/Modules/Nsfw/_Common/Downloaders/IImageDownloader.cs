#nullable disable
namespace WizBot.Modules.Nsfw.Common;

public interface IImageDownloader
{
    Task<List<ImageData>> DownloadImageDataAsync(
        string[] tags,
        int page = 0,
        bool isExplicit = false,
        CancellationToken cancel = default);
}