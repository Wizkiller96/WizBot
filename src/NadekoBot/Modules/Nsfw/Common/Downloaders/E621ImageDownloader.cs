#nullable disable
using System.Net.Http.Json;

namespace NadekoBot.Modules.Nsfw.Common;

public class E621ImageDownloader : ImageDownloader<E621Object>
{
    public E621ImageDownloader(HttpClient http)
        : base(Booru.E621, http)
    {
    }

    public override async Task<List<E621Object>> DownloadImagesAsync(
        string[] tags,
        int page,
        bool isExplicit = false,
        CancellationToken cancel = default)
    {
        var tagString = ImageDownloaderHelper.GetTagString(tags, isExplicit);
        var uri = $"https://e621.net/posts.json?limit=32&tags={tagString}&page={page}";
        using var req = new HttpRequestMessage(HttpMethod.Get, uri);
        req.Headers.AddFakeHeaders();
        using var res = await _http.SendAsync(req, cancel);
        res.EnsureSuccessStatusCode();

        var data = await res.Content.ReadFromJsonAsync<E621Response>(_serializerOptions, cancel);
        if (data?.Posts is null)
            return new();

        return data.Posts.Where(x => !string.IsNullOrWhiteSpace(x.File?.Url)).ToList();
    }
}