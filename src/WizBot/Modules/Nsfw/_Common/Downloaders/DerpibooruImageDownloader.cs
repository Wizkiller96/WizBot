﻿#nullable disable
using System.Net.Http.Json;
using Wiz.Common;

namespace WizBot.Modules.Nsfw.Common;

public class DerpibooruImageDownloader : ImageDownloader<DerpiImageObject>
{
    public DerpibooruImageDownloader(IHttpClientFactory http)
        : base(Booru.Derpibooru, http)
    {
    }

    public override async Task<List<DerpiImageObject>> DownloadImagesAsync(
        string[] tags,
        int page,
        bool isExplicit = false,
        CancellationToken cancel = default)
    {
        var tagString = ImageDownloaderHelper.GetTagString(tags, isExplicit);
        var uri =
            $"https://www.derpibooru.org/api/v1/json/search/images?q={tagString.Replace('+', ',')}&per_page=49&page={page}";
        using var req = new HttpRequestMessage(HttpMethod.Get, uri);
        req.Headers.AddFakeHeaders();
        using var http = _http.CreateClient();
        using var res = await http.SendAsync(req, cancel);
        res.EnsureSuccessStatusCode();

        var container = await res.Content.ReadFromJsonAsync<DerpiContainer>(_serializerOptions, cancel);
        if (container?.Images is null)
            return new();

        return container.Images.Where(x => !string.IsNullOrWhiteSpace(x.ViewUrl)).ToList();
    }
}