#nullable disable
using System.Text.Json;

namespace WizBot.Modules.Nsfw.Common;

public sealed class SankakuImageDownloader : ImageDownloader<SankakuImageObject>
{
    private readonly string _baseUrl;

    public SankakuImageDownloader(HttpClient http)
        : base(Booru.Sankaku, http)
    {
        _baseUrl = "https://capi-v2.sankakucomplex.com";
        _http.AddFakeHeaders();
    }

    public override async Task<List<SankakuImageObject>> DownloadImagesAsync(
        string[] tags,
        int page,
        bool isExplicit = false,
        CancellationToken cancel = default)
    {
        // explicit probably not supported
        var tagString = ImageDownloaderHelper.GetTagString(tags);

        var uri = $"{_baseUrl}/posts?tags={tagString}&limit=50";
        var data = await _http.GetStringAsync(uri);
        return JsonSerializer.Deserialize<SankakuImageObject[]>(data, _serializerOptions)
                             .Where(x => !string.IsNullOrWhiteSpace(x.FileUrl) && x.FileType.StartsWith("image"))
                             .ToList();
    }
}