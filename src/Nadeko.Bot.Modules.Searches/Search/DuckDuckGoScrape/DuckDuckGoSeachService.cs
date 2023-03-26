// using AngleSharp.Html.Dom;
// using MorseCode.ITask;
// using NadekoBot.Modules.Searches.Common;
// using System.Net;
//
// namespace NadekoBot.Modules.Searches.DuckDuckGo;
//
// public sealed class DuckDuckGoSeachService : SearchServiceBase
// {
//     private static readonly HtmlParser _googleParser = new(new()
//     {
//         IsScripting = false,
//         IsEmbedded = false,
//         IsSupportingProcessingInstructions = false,
//         IsKeepingSourceReferences = false,
//         IsNotSupportingFrames = true
//     });
//     
//     public override async ITask<SearchResultData> SearchAsync(string query)
//     {
//         query = WebUtility.UrlEncode(query)?.Replace(' ', '+');
//     
//         var fullQueryLink = "https://html.duckduckgo.com/html";
//     
//         using var http = _httpFactory.CreateClient();
//         http.DefaultRequestHeaders.Clear();
//         http.DefaultRequestHeaders.Add("User-Agent",
//             "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.77 Safari/537.36");
//     
//         using var formData = new MultipartFormDataContent();
//         formData.Add(new StringContent(query), "q");
//         using var response = await http.PostAsync(fullQueryLink, formData);
//         var content = await response.Content.ReadAsStringAsync();
//     
//         using var document = await _googleParser.ParseDocumentAsync(content);
//         var searchResults = document.QuerySelector(".results");
//         var elems = searchResults.QuerySelectorAll(".result");
//     
//         if (!elems.Any())
//             return default;
//     
//         var results = elems.Select(elem =>
//                            {
//                                if (elem.QuerySelector(".result__a") is not IHtmlAnchorElement anchor)
//                                    return null;
//     
//                                var href = anchor.Href;
//                                var name = anchor.TextContent;
//     
//                                if (string.IsNullOrWhiteSpace(href) || string.IsNullOrWhiteSpace(name))
//                                    return null;
//     
//                                var txt = elem.QuerySelector(".result__snippet")?.TextContent;
//     
//                                if (string.IsNullOrWhiteSpace(txt))
//                                    return null;
//     
//                                return new GoogleSearchResult(name, href, txt);
//                            })
//                            .Where(x => x is not null)
//                            .ToList();
//     
//         return new(results.AsReadOnly(), fullQueryLink, "0");
//     }
// }