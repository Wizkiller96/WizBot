// using AngleSharp.Html.Dom;
// using MorseCode.ITask;
// using WizBot.Modules.Searches.Common;
//
// namespace WizBot.Modules.Searches.GoogleScrape;
//
// public sealed class GoogleScrapeService : SearchServiceBase
// {
//     public override async ITask<GoogleSearchResultData> SearchAsync(string query)
//     {
//         ArgumentNullException.ThrowIfNull(query);
//         
//         query = Uri.EscapeDataString(query)?.Replace(' ', '+');
//
//         var fullQueryLink = $"https://www.google.ca/search?q={query}&safe=on&lr=lang_eng&hl=en&ie=utf-8&oe=utf-8";
//
//         using var msg = new HttpRequestMessage(HttpMethod.Get, fullQueryLink);
//         msg.Headers.Add("User-Agent",
//             "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.77 Safari/537.36");
//         msg.Headers.Add("Cookie", "CONSENT=YES+shp.gws-20210601-0-RC2.en+FX+423;");
//
//         using var http = _httpFactory.CreateClient();
//         http.DefaultRequestHeaders.Clear();
//
//         using var response = await http.SendAsync(msg);
//         await using var content = await response.Content.ReadAsStreamAsync();
//
//         using var document = await _googleParser.ParseDocumentAsync(content);
//         var elems = document.QuerySelectorAll("div.g > div > div");
//
//         var resultsElem = document.QuerySelectorAll("#resultStats").FirstOrDefault();
//         var totalResults = resultsElem?.TextContent;
//         //var time = resultsElem.Children.FirstOrDefault()?.TextContent
//         //^ this doesn't work for some reason, <nobr> is completely missing in parsed collection
//         if (!elems.Any())
//             return default;
//
//         var results = elems.Select(elem =>
//                            {
//                                var children = elem.Children.ToList();
//                                if (children.Count < 2)
//                                    return null;
//
//                                var href = (children[0].QuerySelector("a") as IHtmlAnchorElement)?.Href;
//                                var name = children[0].QuerySelector("h3")?.TextContent;
//
//                                if (href is null || name is null)
//                                    return null;
//
//                                var txt = children[1].TextContent;
//
//                                if (string.IsNullOrWhiteSpace(txt))
//                                    return null;
//
//                                return new GoogleSearchResult(name, href, txt);
//                            })
//                            .Where(x => x is not null)
//                            .ToList();
//
//         return new(results.AsReadOnly(), fullQueryLink, totalResults);
//     }
// }