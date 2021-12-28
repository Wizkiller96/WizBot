#nullable disable
using System.Text.Json;

namespace NadekoBot.Modules.Nsfw.Common;

public class GelbooruImageDownloader : ImageDownloader<DapiImageObject>
{
    public GelbooruImageDownloader(HttpClient http) : base(Booru.Gelbooru, http)
    {
    }

    public override async Task<List<DapiImageObject>> DownloadImagesAsync(string[] tags, int page, bool isExplicit = false, CancellationToken cancel = default)
    {
        var tagString = ImageDownloaderHelper.GetTagString(tags, isExplicit);
        var uri = $"http://gelbooru.com/index.php?page=dapi&s=post&json=1&q=index&limit=100" +
                  $"&tags={tagString}&pid={page}";
        using var req = new HttpRequestMessage(HttpMethod.Get, uri);
        using var res = await _http.SendAsync(req, cancel);
        res.EnsureSuccessStatusCode();
        var resString = await res.Content.ReadAsStringAsync(cancel);
        if (string.IsNullOrWhiteSpace(resString))
            return new();
            
        var images = JsonSerializer.Deserialize<List<DapiImageObject>>(resString, _serializerOptions);
        if (images is null)
            return new();

        return images.Where(x => x.FileUrl is not null).ToList();
    }
}
