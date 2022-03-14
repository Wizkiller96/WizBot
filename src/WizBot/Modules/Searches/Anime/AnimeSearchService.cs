﻿#nullable disable
using AngleSharp;
using AngleSharp.Html.Dom;
using WizBot.Modules.Searches.Common;
using Newtonsoft.Json;

namespace WizBot.Modules.Searches.Services;

public class AnimeSearchService : INService
{
    private readonly IDataCache _cache;
    private readonly IHttpClientFactory _httpFactory;

    public AnimeSearchService(IDataCache cache, IHttpClientFactory httpFactory)
    {
        _cache = cache;
        _httpFactory = httpFactory;
    }

    public async Task<AnimeResult> GetAnimeData(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentNullException(nameof(query));
        try
        {
            var link = "https://aniapi.nadeko.bot/anime/"
                       + Uri.EscapeDataString(query.Replace("/", " ", StringComparison.InvariantCulture));
            link = link.ToLowerInvariant();
            var (ok, data) = await _cache.TryGetAnimeDataAsync(link);
            if (!ok)
            {
                using (var http = _httpFactory.CreateClient())
                {
                    data = await http.GetStringAsync(link);
                }

                await _cache.SetAnimeDataAsync(link, data);
            }


            return JsonConvert.DeserializeObject<AnimeResult>(data);
        }
        catch
        {
            return null;
        }
    }

    public async Task<NovelResult> GetNovelData(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentNullException(nameof(query));

        query = query.Replace(" ", "-", StringComparison.InvariantCulture);
        try
        {
            var link = "https://www.novelupdates.com/series/"
                       + Uri.EscapeDataString(query.Replace(" ", "-").Replace("/", " "));
            link = link.ToLowerInvariant();
            var (ok, data) = await _cache.TryGetNovelDataAsync(link);
            if (!ok)
            {
                var config = Configuration.Default.WithDefaultLoader();
                using var document = await BrowsingContext.New(config).OpenAsync(link);
                var imageElem = document.QuerySelector("div.seriesimg > img");
                if (imageElem is null)
                    return null;
                var imageUrl = ((IHtmlImageElement)imageElem).Source;

                var descElem = document.QuerySelector("div#editdescription > p");
                var desc = descElem.InnerHtml;

                var genres = document.QuerySelector("div#seriesgenre")
                                     .Children.Select(x => x as IHtmlAnchorElement)
                                     .Where(x => x is not null)
                                     .Select(x => $"[{x.InnerHtml}]({x.Href})")
                                     .ToArray();

                var authors = document.QuerySelector("div#showauthors")
                                      .Children.Select(x => x as IHtmlAnchorElement)
                                      .Where(x => x is not null)
                                      .Select(x => $"[{x.InnerHtml}]({x.Href})")
                                      .ToArray();

                var score = ((IHtmlSpanElement)document.QuerySelector("h5.seriesother > span.uvotes")).InnerHtml;

                var status = document.QuerySelector("div#editstatus").InnerHtml;
                var title = document.QuerySelector("div.w-blog-content > div.seriestitlenu").InnerHtml;

                var obj = new NovelResult
                {
                    Description = desc,
                    Authors = authors,
                    Genres = genres,
                    ImageUrl = imageUrl,
                    Link = link,
                    Score = score,
                    Status = status,
                    Title = title
                };

                await _cache.SetNovelDataAsync(link, JsonConvert.SerializeObject(obj));

                return obj;
            }

            return JsonConvert.DeserializeObject<NovelResult>(data);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting novel data");
            return null;
        }
    }

    public async Task<MangaResult> GetMangaData(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentNullException(nameof(query));
        try
        {
            var link = "https://aniapi.nadeko.bot/manga/"
                       + Uri.EscapeDataString(query.Replace("/", " ", StringComparison.InvariantCulture));
            link = link.ToLowerInvariant();
            var (ok, data) = await _cache.TryGetAnimeDataAsync(link);
            if (!ok)
            {
                using (var http = _httpFactory.CreateClient())
                {
                    data = await http.GetStringAsync(link);
                }

                await _cache.SetAnimeDataAsync(link, data);
            }


            return JsonConvert.DeserializeObject<MangaResult>(data);
        }
        catch
        {
            return null;
        }
    }
}