using NadekoBot.Modules.Searches.Youtube;
using StackExchange.Redis;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Nadeko.Common;

namespace NadekoBot.Modules.Searches;

public partial class Searches
{
    public partial class SearchCommands : NadekoModule
    {
        private readonly ISearchServiceFactory _searchFactory;
        private readonly IBotCache _cache;

        public SearchCommands(
            ISearchServiceFactory searchFactory,
            IBotCache cache)
        {
            _searchFactory = searchFactory;
            _cache = cache;
        }

        [Cmd]
        public async Task Google([Leftover] string? query = null)
        {
            query = query?.Trim();

            if (string.IsNullOrWhiteSpace(query))
            {
                await ErrorLocalizedAsync(strs.specify_search_params);
                return;
            }

            _ = ctx.Channel.TriggerTypingAsync();

            var search = _searchFactory.GetSearchService();
            var data = await search.SearchAsync(query);

            if (data is null or { Entries: null or { Count: 0 } })
            {
                await ReplyErrorLocalizedAsync(strs.no_results);
                return;
            }

            // 3 with an answer
            // 4 without an answer
            // 5 is ideal but it lookes horrible on mobile

            var takeCount = string.IsNullOrWhiteSpace(data.Answer)
                ? 4
                : 3;

            var descStr = data.Entries
                              .Take(takeCount)
                              .Select(static res => $@"**[{Format.Sanitize(res.Title)}]({res.Url})**
*{Format.EscapeUrl(res.DisplayUrl)}*
{Format.Sanitize(res.Description ?? "-")}")
                              .Join("\n\n");

            if (!string.IsNullOrWhiteSpace(data.Answer))
                descStr = Format.Code(data.Answer) + "\n\n" + descStr;

            descStr = descStr.TrimTo(4096);

            var embed = _eb.Create()
                           .WithOkColor()
                           .WithAuthor(ctx.User)
                           .WithTitle(query.TrimTo(64)!)
                           .WithDescription(descStr)
                           .WithFooter(
                               GetText(strs.results_in(data.Info.TotalResults, data.Info.SearchTime)),
                               "https://i.imgur.com/G46fm8J.png");

            await ctx.Channel.EmbedAsync(embed);
        }

        [Cmd]
        public async Task Image([Leftover] string? query = null)
        {
            query = query?.Trim();

            if (string.IsNullOrWhiteSpace(query))
            {
                await ErrorLocalizedAsync(strs.specify_search_params);
                return;
            }

            _ = ctx.Channel.TriggerTypingAsync();

            var search = _searchFactory.GetImageSearchService();
            var data = await search.SearchImagesAsync(query);

            if (data is null or { Entries: null or { Count: 0 } })
            {
                await ReplyErrorLocalizedAsync(strs.no_search_results);
                return;
            }

            var embeds = new List<IEmbedBuilder>(4);


            IEmbedBuilder CreateEmbed(IImageSearchResultEntry entry)
            {
                return _eb.Create(ctx)
                          .WithOkColor()
                          .WithAuthor(ctx.User)
                          .WithTitle(query)
                          .WithUrl("https://google.com")
                          .WithImageUrl(entry.Link);
            }

            embeds.Add(CreateEmbed(data.Entries.First())
                .WithFooter(
                    GetText(strs.results_in(data.Info.TotalResults, data.Info.SearchTime)),
                    "https://i.imgur.com/G46fm8J.png"));

            var random = data.Entries.Skip(1)
                             .Shuffle()
                             .Take(3)
                             .ToArray();

            foreach (var entry in random)
            {
                embeds.Add(CreateEmbed(entry));
            }

            await ctx.Channel.EmbedAsync(null, embeds: embeds);
        }

        private TypedKey<string> GetYtCacheKey(string query)
            => new($"search:youtube:{query}");
        
        private async Task AddYoutubeUrlToCacheAsync(string query, string url)
            => await _cache.AddAsync(GetYtCacheKey(query), url, expiry: 1.Hours());

        private async Task<VideoInfo?> GetYoutubeUrlFromCacheAsync(string query)
        {
            var result = await _cache.GetAsync(GetYtCacheKey(query));

            if (!result.TryGetValue(out var url) || string.IsNullOrWhiteSpace(url))
                return null;

            return new VideoInfo()
            {
                Url = url
            };
        }

        [Cmd]
        public async Task Youtube([Leftover] string? query = null)
        {
            query = query?.Trim();

            if (string.IsNullOrWhiteSpace(query))
            {
                await ErrorLocalizedAsync(strs.specify_search_params);
                return;
            }

            _ = ctx.Channel.TriggerTypingAsync();

            var maybeResult = await GetYoutubeUrlFromCacheAsync(query)
                              ?? await _searchFactory.GetYoutubeSearchService().SearchAsync(query);
            if (maybeResult is not {} result || result is {Url: null})
            {
                await ReplyErrorLocalizedAsync(strs.no_results);
                return;
            }

            await AddYoutubeUrlToCacheAsync(query, result.Url);
            await ctx.Channel.SendMessageAsync(result.Url);
        }

//     [Cmd]
//     public async Task DuckDuckGo([Leftover] string query = null)
//     {
//         query = query?.Trim();
//         if (!await ValidateQuery(query))
//             return;
//
//         _ = ctx.Channel.TriggerTypingAsync();
//
//         var data = await _service.DuckDuckGoSearchAsync(query);
//         if (data is null)
//         {
//             await ReplyErrorLocalizedAsync(strs.no_results);
//             return;
//         }
//
//         var desc = data.Results.Take(5)
//                        .Select(res => $@"[**{res.Title}**]({res.Link})
// {res.Text.TrimTo(380 - res.Title.Length - res.Link.Length)}");
//
//         var descStr = string.Join("\n\n", desc);
//
//         var embed = _eb.Create()
//                        .WithAuthor(ctx.User.ToString(),
//                            "https://upload.wikimedia.org/wikipedia/en/9/90/The_DuckDuckGo_Duck.png")
//                        .WithDescription($"{GetText(strs.search_for)} **{query}**\n\n" + descStr)
//                        .WithOkColor();
//
//         await ctx.Channel.EmbedAsync(embed);
//     }
    }
}