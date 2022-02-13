#nullable disable
using Google.Apis.Customsearch.v1.Data;

namespace NadekoBot.Services;

public interface IGoogleApiService
{
    IReadOnlyDictionary<string, string> Languages { get; }

    Task<IEnumerable<string>> GetVideoLinksByKeywordAsync(string keywords, int count = 1);
    Task<IEnumerable<(string Name, string Id, string Url)>> GetVideoInfosByKeywordAsync(string keywords, int count = 1);
    Task<IEnumerable<string>> GetPlaylistIdsByKeywordsAsync(string keywords, int count = 1);
    Task<IEnumerable<string>> GetRelatedVideosAsync(string id, int count = 1, string user = null);
    Task<IEnumerable<string>> GetPlaylistTracksAsync(string playlistId, int count = 50);
    Task<IReadOnlyDictionary<string, TimeSpan>> GetVideoDurationsAsync(IEnumerable<string> videoIds);
    Task<ImageResult> GetImageAsync(string query);
    Task<string> Translate(string sourceText, string sourceLanguage, string targetLanguage);

    Task<string> ShortenUrl(string url);
    Task<string> ShortenUrl(Uri url);
}

public struct ImageResult
{
    public Result.ImageData Image { get; }
    public string Link { get; }

    public ImageResult(Result.ImageData image, string link)
    {
        Image = image;
        Link = link;
    }
}