#nullable disable
using System.Net.Http.Json;

namespace NadekoBot.Modules.Nsfw.Common;

public abstract class DapiImageDownloader : ImageDownloader<DapiImageObject>
{
    protected readonly string _baseUrl;

    public DapiImageDownloader(Booru booru, HttpClient http, string baseUrl)
        : base(booru, http)
        => _baseUrl = baseUrl;

    public abstract Task<bool> IsTagValid(string tag, CancellationToken cancel = default);

    protected async Task<bool> AllTagsValid(string[] tags, CancellationToken cancel = default)
    {
        var results = await tags.Select(tag => IsTagValid(tag, cancel)).WhenAll();

        // if any of the tags is not valid, the query is not valid
        foreach (var result in results)
        {
            if (!result)
                return false;
        }

        return true;
    }

    public override async Task<List<DapiImageObject>> DownloadImagesAsync(
        string[] tags,
        int page,
        bool isExplicit = false,
        CancellationToken cancel = default)
    {
        // up to 2 tags allowed on danbooru
        if (tags.Length > 2)
            return new();

        if (!await AllTagsValid(tags, cancel))
            return new();

        var tagString = ImageDownloaderHelper.GetTagString(tags, isExplicit);

        var uri = $"{_baseUrl}/posts.json?limit=200&tags={tagString}&page={page}";
        var imageObjects = await _http.GetFromJsonAsync<DapiImageObject[]>(uri, _serializerOptions, cancel);
        if (imageObjects is null)
            return new();
        return imageObjects.Where(x => x.FileUrl is not null).ToList();
    }
}