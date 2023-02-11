// using AngleSharp.Html.Dom;
// using AngleSharp.Html.Parser;
// using NadekoBot.Modules.Searches.Common;
//
// namespace NadekoBot.Modules.Nsfw;
//
// public sealed class NhentaiScraperService : INhentaiService, INService
// {
//     private readonly IHttpClientFactory _httpFactory;
//
//     private static readonly HtmlParser _htmlParser = new(new()
//     {
//         IsScripting = false,
//         IsEmbedded = false,
//         IsSupportingProcessingInstructions = false,
//         IsKeepingSourceReferences = false,
//         IsNotSupportingFrames = true
//     });
//
//     public NhentaiScraperService(IHttpClientFactory httpFactory)
//     {
//         _httpFactory = httpFactory;
//     }
//
//     private HttpClient GetHttpClient()
//     {
//         var http = _httpFactory.CreateClient();
//         http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/103.0.5060.114 Safari/537.36");
//         http.DefaultRequestHeaders.Add("Cookie", "cf_clearance=I5pR71P4wJkRBFTLFjBndI.GwfKwT.Gx06uS8XNmRJo-1657214595-0-150; csrftoken=WMWRLtsQtBVQYvYkbqXKJHI9T1JwWCdd3tNhoxHn7aHLUYHAqe60XFUKAoWsJtda");
//         return http;
//     }
//     
//     public async Task<Gallery?> GetAsync(uint id)
//     {
//         using var http = GetHttpClient();
//         try
//         {
//             var url = $"https://nhentai.net/g/{id}/";
//             var strRes = await http.GetStringAsync(url);
//             var doc = await _htmlParser.ParseDocumentAsync(strRes);
//
//             var title = doc.QuerySelector("#info .title")?.TextContent;
//             var fullTitle = doc.QuerySelector("meta[itemprop=\"name\"]")?.Attributes["content"]?.Value
//                             ?? title;
//             var thumb = (doc.QuerySelector("#cover a img") as IHtmlImageElement)?.Dataset["src"];
//
//             var tagsElem = doc.QuerySelector("#tags");
//             
//             var pageCount = tagsElem?.QuerySelector("a.tag[href^=\"/search/?q=pages\"] span")?.TextContent;
//             var likes = doc.QuerySelector(".buttons .btn-disabled.btn.tooltip span span")?.TextContent?.Trim('(', ')');
//             var uploadedAt = (tagsElem?.QuerySelector(".tag-container .tags time.nobold") as IHtmlTimeElement)?.DateTime;
//
//             var tags = tagsElem?.QuerySelectorAll(".tag-container .tags > a.tag[href^=\"/tag\"]")
//                 .Cast<IHtmlAnchorElement>()
//                 .Select(x => new Tag()
//                 {
//                     Name = x.QuerySelector("span:first-child")?.TextContent,
//                     Url = $"https://nhentai.net{x.PathName}"
//                 })
//                 .ToArray();
//
//             if (string.IsNullOrWhiteSpace(fullTitle))
//                 return null;
//
//             if (!int.TryParse(pageCount, out var pc))
//                 return null;
//
//             if (!int.TryParse(likes, out var lc))
//                 return null;
//
//             if (!DateTime.TryParse(uploadedAt, out var ua))
//                 return null;
//
//             return new Gallery(id,
//                 url,
//                 fullTitle,
//                 title,
//                 thumb,
//                 pc,
//                 lc,
//                 ua,
//                 tags);
//         }
//         catch (HttpRequestException)
//         {
//             Log.Warning("Nhentai with id {NhentaiId} not found", id);
//             return null;
//         }
//     }
//
//     public async Task<IReadOnlyList<uint>> GetIdsBySearchAsync(string search)
//     {
//         using var http = GetHttpClient();
//         try
//         {
//             var url = $"https://nhentai.net/search/?q={Uri.EscapeDataString(search)}&sort=popular-today";
//             var strRes = await http.GetStringAsync(url);
//             var doc = await _htmlParser.ParseDocumentAsync(strRes);
//
//             var elems = doc.QuerySelectorAll(".container .gallery a")
//                 .Cast<IHtmlAnchorElement>()
//                 .Where(x => x.PathName.StartsWith("/g/"))
//                 .Select(x => x.PathName[3..^1])
//                 .Select(uint.Parse)
//                 .ToArray();
//             
//             return elems;
//         }
//         catch (HttpRequestException)
//         {
//             Log.Warning("Nhentai search for {NhentaiSearch} failed", search);
//             return Array.Empty<uint>();
//         }
//     }
// }