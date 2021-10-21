using AngleSharp;
using AngleSharp.Html.Dom;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.Caching.Memory;
using NadekoBot.Common;
using NadekoBot.Common.Attributes;
using NadekoBot.Modules.Searches.Common;
using NadekoBot.Services;
using NadekoBot.Extensions;
using NadekoBot.Modules.Searches.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using NadekoBot.Modules.Administration.Services;
using NadekoBot.Modules.Nsfw.Common;
using Serilog;
using Configuration = AngleSharp.Configuration;

namespace NadekoBot.Modules.Searches
{
    public partial class Searches : NadekoModule<SearchesService>
    {
        private readonly IBotCredentials _creds;
        private readonly IGoogleApiService _google;
        private readonly IHttpClientFactory _httpFactory;
        private readonly IMemoryCache _cache;
        private readonly GuildTimezoneService _tzSvc;

        public Searches(IBotCredentials creds, IGoogleApiService google, IHttpClientFactory factory, IMemoryCache cache,
            GuildTimezoneService tzSvc)
        {
            _creds = creds;
            _google = google;
            _httpFactory = factory;
            _cache = cache;
            _tzSvc = tzSvc;
        }

        [NadekoCommand, Aliases]
        public async Task Rip([Leftover] IGuildUser usr)
        {
            var av = usr.RealAvatarUrl(128);
            if (av is null)
                return;
            using (var picStream = await _service.GetRipPictureAsync(usr.Nickname ?? usr.Username, av).ConfigureAwait(false))
            {
                await ctx.Channel.SendFileAsync(
                    picStream,
                    "rip.png",
                    $"Rip {Format.Bold(usr.ToString())} \n\t- " +
                        Format.Italics(ctx.User.ToString()))
                    .ConfigureAwait(false);
            }
        }

        [NadekoCommand, Aliases]
        public async Task Weather([Leftover] string query)
        {
            if (!await ValidateQuery(ctx.Channel, query).ConfigureAwait(false))
                return;

            var embed = _eb.Create();
            var data = await _service.GetWeatherDataAsync(query).ConfigureAwait(false);

            if (data is null)
            {
                embed.WithDescription(GetText(strs.city_not_found))
                    .WithErrorColor();
            }
            else
            {
                Func<double, double> f = StandardConversions.CelsiusToFahrenheit;
                
                var tz = ctx.Guild is null
                    ? TimeZoneInfo.Utc
                    : _tzSvc.GetTimeZoneOrUtc(ctx.Guild.Id);
                var sunrise = data.Sys.Sunrise.ToUnixTimestamp();
                var sunset = data.Sys.Sunset.ToUnixTimestamp();
                sunrise = sunrise.ToOffset(tz.GetUtcOffset(sunrise));
                sunset = sunset.ToOffset(tz.GetUtcOffset(sunset));
                var timezone = $"UTC{sunrise:zzz}";

                embed.AddField("🌍 " + Format.Bold(GetText(strs.location)), $"[{data.Name + ", " + data.Sys.Country}](https://openweathermap.org/city/{data.Id})", true)
                    .AddField("📏 " + Format.Bold(GetText(strs.latlong)), $"{data.Coord.Lat}, {data.Coord.Lon}", true)
                    .AddField("☁ " + Format.Bold(GetText(strs.condition)), string.Join(", ", data.Weather.Select(w => w.Main)), true)
                    .AddField("😓 " + Format.Bold(GetText(strs.humidity)), $"{data.Main.Humidity}%", true)
                    .AddField("💨 " + Format.Bold(GetText(strs.wind_speed)), data.Wind.Speed + " m/s", true)
                    .AddField("🌡 " + Format.Bold(GetText(strs.temperature)), $"{data.Main.Temp:F1}°C / {f(data.Main.Temp):F1}°F", true)
                    .AddField("🔆 " + Format.Bold(GetText(strs.min_max)), $"{data.Main.TempMin:F1}°C - {data.Main.TempMax:F1}°C\n{f(data.Main.TempMin):F1}°F - {f(data.Main.TempMax):F1}°F", true)
                    .AddField("🌄 " + Format.Bold(GetText(strs.sunrise)), $"{sunrise:HH:mm} {timezone}", true)
                    .AddField("🌇 " + Format.Bold(GetText(strs.sunset)), $"{sunset:HH:mm} {timezone}", true)
                    .WithOkColor()
                    .WithFooter("Powered by openweathermap.org", $"http://openweathermap.org/img/w/{data.Weather[0].Icon}.png");
            }
            await ctx.Channel.EmbedAsync(embed).ConfigureAwait(false);
        }

        [NadekoCommand, Aliases]
        public async Task Time([Leftover] string query)
        {
            if (!await ValidateQuery(ctx.Channel, query).ConfigureAwait(false))
                return;

            await ctx.Channel.TriggerTypingAsync().ConfigureAwait(false);

            var (data, err) = await _service.GetTimeDataAsync(query).ConfigureAwait(false);
            if (err is not null)
            {
                LocStr errorKey;
                switch (err)
                {
                    case TimeErrors.ApiKeyMissing:
                        errorKey = strs.api_key_missing;
                        break;
                    case TimeErrors.InvalidInput:
                        errorKey = strs.invalid_input;
                        break;
                    case TimeErrors.NotFound:
                        errorKey = strs.not_found;
                        break;
                    default:
                        errorKey = strs.error_occured;
                        break;
                }
                await ReplyErrorLocalizedAsync(errorKey).ConfigureAwait(false);
                return;
            }
            else if (string.IsNullOrWhiteSpace(data.TimeZoneName))
            {
                await ReplyErrorLocalizedAsync(strs.timezone_db_api_key).ConfigureAwait(false);
                return;
            }

            var eb = _eb.Create()
                .WithOkColor()
                .WithTitle(GetText(strs.time_new))
                .WithDescription(Format.Code(data.Time.ToString()))
                .AddField(GetText(strs.location), string.Join('\n', data.Address.Split(", ")), true)
                .AddField(GetText(strs.timezone), data.TimeZoneName, true);

            await ctx.Channel.SendMessageAsync(embed: eb.Build()).ConfigureAwait(false);
        }

        [NadekoCommand, Aliases]
        public async Task Youtube([Leftover] string query = null)
        {
            if (!await ValidateQuery(ctx.Channel, query).ConfigureAwait(false))
                return;

            var result = (await _google.GetVideoLinksByKeywordAsync(query, 1).ConfigureAwait(false)).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(result))
            {
                await ReplyErrorLocalizedAsync(strs.no_results).ConfigureAwait(false);
                return;
            }

            await ctx.Channel.SendMessageAsync(result).ConfigureAwait(false);
        }

        [NadekoCommand, Aliases]
        public async Task Movie([Leftover] string query = null)
        {
            if (!await ValidateQuery(ctx.Channel, query).ConfigureAwait(false))
                return;

            await ctx.Channel.TriggerTypingAsync().ConfigureAwait(false);

            var movie = await _service.GetMovieDataAsync(query).ConfigureAwait(false);
            if (movie is null)
            {
                await ReplyErrorLocalizedAsync(strs.imdb_fail).ConfigureAwait(false);
                return;
            }
            await ctx.Channel.EmbedAsync(_eb.Create().WithOkColor()
                .WithTitle(movie.Title)
                .WithUrl($"http://www.imdb.com/title/{movie.ImdbId}/")
                .WithDescription(movie.Plot.TrimTo(1000))
                .AddField("Rating", movie.ImdbRating, true)
                .AddField("Genre", movie.Genre, true)
                .AddField("Year", movie.Year, true)
                .WithImageUrl(movie.Poster)).ConfigureAwait(false);
        }

        [NadekoCommand, Aliases]
        public Task RandomCat() => InternalRandomImage(SearchesService.ImageTag.Cats);

        [NadekoCommand, Aliases]
        public Task RandomDog() => InternalRandomImage(SearchesService.ImageTag.Dogs);

        [NadekoCommand, Aliases]
        public Task RandomFood() => InternalRandomImage(SearchesService.ImageTag.Food);

        [NadekoCommand, Aliases]
        public Task RandomBird() => InternalRandomImage(SearchesService.ImageTag.Birds);

        private Task InternalRandomImage(SearchesService.ImageTag tag)
        {
            var url = _service.GetRandomImageUrl(tag);
            return ctx.Channel.EmbedAsync(_eb.Create()
                .WithOkColor()
                .WithImageUrl(url));
        }

        [NadekoCommand, Aliases]
        public async Task Image([Leftover] string query = null)
        {
            var oterms = query?.Trim();
            if (!await ValidateQuery(ctx.Channel, query).ConfigureAwait(false))
                return;
            query = WebUtility.UrlEncode(oterms).Replace(' ', '+');
            try
            {
                var res = await _google.GetImageAsync(oterms).ConfigureAwait(false);
                var embed = _eb.Create()
                    .WithOkColor()
                    .WithAuthor(GetText(strs.image_search_for) + " " + oterms.TrimTo(50),
                        "http://i.imgur.com/G46fm8J.png",
                        $"https://www.google.rs/search?q={query}&source=lnms&tbm=isch")
                    .WithDescription(res.Link)
                    .WithImageUrl(res.Link)
                    .WithTitle(ctx.User.ToString());
                await ctx.Channel.EmbedAsync(embed).ConfigureAwait(false);
            }
            catch
            {
                Log.Warning("Falling back to Imgur");

                var fullQueryLink = $"http://imgur.com/search?q={ query }";
                var config = Configuration.Default.WithDefaultLoader();
                using (var document = await BrowsingContext.New(config).OpenAsync(fullQueryLink).ConfigureAwait(false))
                {
                    var elems = document.QuerySelectorAll("a.image-list-link").ToList();

                    if (!elems.Any())
                        return;

                    var img = (elems.ElementAtOrDefault(new NadekoRandom().Next(0, elems.Count))?.Children?.FirstOrDefault() as IHtmlImageElement);

                    if (img?.Source is null)
                        return;

                    var source = img.Source.Replace("b.", ".", StringComparison.InvariantCulture);

                    var embed = _eb.Create()
                        .WithOkColor()
                        .WithAuthor(GetText(strs.image_search_for) + " " + oterms.TrimTo(50),
                            "http://s.imgur.com/images/logo-1200-630.jpg?",
                            fullQueryLink)
                        .WithDescription(source)
                        .WithImageUrl(source)
                        .WithTitle(ctx.User.ToString());
                    await ctx.Channel.EmbedAsync(embed).ConfigureAwait(false);
                }
            }
        }

        [NadekoCommand, Aliases]
        public async Task Lmgtfy([Leftover] string ffs = null)
        {
            if (!await ValidateQuery(ctx.Channel, ffs).ConfigureAwait(false))
                return;

            var shortenedUrl = await _google.ShortenUrl($"http://lmgtfy.com/?q={Uri.EscapeUriString(ffs)}");
            await SendConfirmAsync($"<{shortenedUrl}>");
        }

        public class ShortenData
        {
            [JsonProperty("result_url")]
            public string ResultUrl { get; set; }
        }

        private static readonly ConcurrentDictionary<string, string> cachedShortenedLinks = new ConcurrentDictionary<string, string>();

        [NadekoCommand, Aliases]
        public async Task Shorten([Leftover] string query)
        {
            if (!await ValidateQuery(ctx.Channel, query).ConfigureAwait(false))
                return;

            query = query.Trim();
            if (!cachedShortenedLinks.TryGetValue(query, out var shortLink))
            {
                try
                {
                    using (var _http = _httpFactory.CreateClient())
                    using (var req = new HttpRequestMessage(HttpMethod.Post, "https://goolnk.com/api/v1/shorten"))
                    {
                        var formData = new MultipartFormDataContent
                    {
                        { new StringContent(query), "url" }
                    };
                        req.Content = formData;

                        using (var res = await _http.SendAsync(req).ConfigureAwait(false))
                        {
                            var content = await res.Content.ReadAsStringAsync();
                            var data = JsonConvert.DeserializeObject<ShortenData>(content);

                            if (!string.IsNullOrWhiteSpace(data?.ResultUrl))
                                cachedShortenedLinks.TryAdd(query, data.ResultUrl);
                            else
                                return;

                            shortLink = data.ResultUrl;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error shortening a link: {Message}", ex.Message);
                    return;
                }
            }

            await ctx.Channel.EmbedAsync(_eb.Create()
                .WithOkColor()
                .AddField(GetText(strs.original_url), $"<{query}>")
                .AddField(GetText(strs.short_url), $"<{shortLink}>"));
        }

        [NadekoCommand, Aliases]
        public async Task Google([Leftover] string query = null)
        {
            query = query?.Trim();
            if (!await ValidateQuery(ctx.Channel, query).ConfigureAwait(false))
                return;

            _ = ctx.Channel.TriggerTypingAsync();
            
            var data = await _service.GoogleSearchAsync(query);
            if (data is null)
            {
                await ReplyErrorLocalizedAsync(strs.no_results);
                return;
            }
            
            var desc = data.Results.Take(5).Select(res =>
                $@"[**{res.Title}**]({res.Link})
{res.Text.TrimTo(400 - res.Title.Length - res.Link.Length)}");

            var descStr = string.Join("\n\n", desc);

            var embed = _eb.Create()
                .WithAuthor(ctx.User.ToString(),
                    iconUrl: "http://i.imgur.com/G46fm8J.png")
                .WithTitle(ctx.User.ToString())
                .WithFooter(data.TotalResults)
                .WithDescription($"{GetText(strs.search_for)} **{query}**\n\n" +descStr)
                .WithOkColor();

            await ctx.Channel.EmbedAsync(embed).ConfigureAwait(false);
        }
        
        [NadekoCommand, Aliases]
        public async Task DuckDuckGo([Leftover] string query = null)
        {
            query = query?.Trim();
            if (!await ValidateQuery(ctx.Channel, query).ConfigureAwait(false))
                return;

            _ = ctx.Channel.TriggerTypingAsync();
            
            var data = await _service.DuckDuckGoSearchAsync(query);
            if (data is null)
            {
                await ReplyErrorLocalizedAsync(strs.no_results);
                return;
            }
            
            var desc = data.Results.Take(5).Select(res =>
                $@"[**{res.Title}**]({res.Link})
{res.Text.TrimTo(380 - res.Title.Length - res.Link.Length)}");

            var descStr = string.Join("\n\n", desc);
            
            var embed = _eb.Create()
                .WithAuthor(ctx.User.ToString(),
                    iconUrl: "https://upload.wikimedia.org/wikipedia/en/9/90/The_DuckDuckGo_Duck.png")
                .WithDescription($"{GetText(strs.search_for)} **{query}**\n\n" + descStr)
                .WithOkColor();

            await ctx.Channel.EmbedAsync(embed).ConfigureAwait(false);
        }

        [NadekoCommand, Aliases]
        public async Task MagicTheGathering([Leftover] string search)
        {
            if (!await ValidateQuery(ctx.Channel, search))
                return;

            await ctx.Channel.TriggerTypingAsync().ConfigureAwait(false);
            var card = await _service.GetMtgCardAsync(search).ConfigureAwait(false);

            if (card is null)
            {
                await ReplyErrorLocalizedAsync(strs.card_not_found).ConfigureAwait(false);
                return;
            }

            var embed = _eb.Create().WithOkColor()
                .WithTitle(card.Name)
                .WithDescription(card.Description)
                .WithImageUrl(card.ImageUrl)
                .AddField(GetText(strs.store_url), card.StoreUrl, true)
                .AddField(GetText(strs.cost), card.ManaCost, true)
                .AddField(GetText(strs.types), card.Types, true);

            await ctx.Channel.EmbedAsync(embed).ConfigureAwait(false);
        }

        [NadekoCommand, Aliases]
        public async Task Hearthstone([Leftover] string name)
        {
            var arg = name;
            if (!await ValidateQuery(ctx.Channel, name).ConfigureAwait(false))
                return;

            if (string.IsNullOrWhiteSpace(_creds.RapidApiKey))
            {
                await ReplyErrorLocalizedAsync(strs.mashape_api_missing).ConfigureAwait(false);
                return;
            }

            await ctx.Channel.TriggerTypingAsync().ConfigureAwait(false);
            var card = await _service.GetHearthstoneCardDataAsync(name).ConfigureAwait(false);

            if (card is null)
            {
                await ReplyErrorLocalizedAsync(strs.card_not_found).ConfigureAwait(false);
                return;
            }
            var embed = _eb.Create().WithOkColor()
                .WithImageUrl(card.Img);

            if (!string.IsNullOrWhiteSpace(card.Flavor))
                embed.WithDescription(card.Flavor);

            await ctx.Channel.EmbedAsync(embed).ConfigureAwait(false);
        }

        [NadekoCommand, Aliases]
        public async Task UrbanDict([Leftover] string query = null)
        {
            if (!await ValidateQuery(ctx.Channel, query).ConfigureAwait(false))
                return;

            await ctx.Channel.TriggerTypingAsync().ConfigureAwait(false);
            using (var http = _httpFactory.CreateClient())
            {
                var res = await http.GetStringAsync($"http://api.urbandictionary.com/v0/define?term={Uri.EscapeUriString(query)}").ConfigureAwait(false);
                try
                {
                    var items = JsonConvert.DeserializeObject<UrbanResponse>(res).List;
                    if (items.Any())
                    {

                        await ctx.SendPaginatedConfirmAsync(0, (p) =>
                        {
                            var item = items[p];
                            return _eb.Create().WithOkColor()
                                         .WithUrl(item.Permalink)
                                         .WithAuthor(item.Word)
                                         .WithDescription(item.Definition);
                        }, items.Length, 1).ConfigureAwait(false);
                        return;
                    }
                }
                catch
                {
                }
            }
            await ReplyErrorLocalizedAsync(strs.ud_error).ConfigureAwait(false);

        }

        [NadekoCommand, Aliases]
        public async Task Define([Leftover] string word)
        {
            if (!await ValidateQuery(ctx.Channel, word).ConfigureAwait(false))
                return;

            using (var _http = _httpFactory.CreateClient())
            {
                string res;
                try
                {
                    res = await _cache.GetOrCreateAsync($"define_{word}", e =>
                     {
                         e.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12);
                         return _http.GetStringAsync("https://api.pearson.com/v2/dictionaries/entries?headword=" + WebUtility.UrlEncode(word));
                     }).ConfigureAwait(false);

                    var data = JsonConvert.DeserializeObject<DefineModel>(res);

                    var datas = data.Results
                        .Where(x => !(x.Senses is null) && x.Senses.Count > 0 && !(x.Senses[0].Definition is null))
                        .Select(x => (Sense: x.Senses[0], x.PartOfSpeech));

                    if (!datas.Any())
                    {
                        Log.Warning("Definition not found: {Word}", word);
                        await ReplyErrorLocalizedAsync(strs.define_unknown).ConfigureAwait(false);
                    }


                    var col = datas.Select(data => (
                        Definition: data.Sense.Definition is string
                            ? data.Sense.Definition.ToString()
                            : ((JArray)JToken.Parse(data.Sense.Definition.ToString())).First.ToString(),
                        Example: data.Sense.Examples is null || data.Sense.Examples.Count == 0
                            ? string.Empty
                            : data.Sense.Examples[0].Text,
                        Word: word,
                        WordType: string.IsNullOrWhiteSpace(data.PartOfSpeech) ? "-" : data.PartOfSpeech
                    )).ToList();

                    Log.Information($"Sending {col.Count} definition for: {word}");

                    await ctx.SendPaginatedConfirmAsync(0, page =>
                    {
                        var data = col.Skip(page).First();
                        var embed = _eb.Create()
                            .WithDescription(ctx.User.Mention)
                            .AddField(GetText(strs.word), data.Word, true)
                            .AddField(GetText(strs._class), data.WordType, true)
                            .AddField(GetText(strs.definition), data.Definition)
                            .WithOkColor();

                        if (!string.IsNullOrWhiteSpace(data.Example))
                            embed.AddField(GetText(strs.example), data.Example);

                        return embed;
                    }, col.Count, 1);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error retrieving definition data for: {Word}", word);
                }
            }
        }

        [NadekoCommand, Aliases]
        public async Task Catfact()
        {
            using (var http = _httpFactory.CreateClient())
            {
                var response = await http.GetStringAsync("https://catfact.ninja/fact").ConfigureAwait(false);
                if (response is null)
                    return;

                var fact = JObject.Parse(response)["fact"].ToString();
                await SendConfirmAsync("🐈" + GetText(strs.catfact), fact).ConfigureAwait(false);
            }
        }

        //done in 3.0
        [NadekoCommand, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Revav([Leftover] IGuildUser usr = null)
        {
            if (usr is null)
                usr = (IGuildUser)ctx.User;

            var av = usr.RealAvatarUrl();
            if (av is null)
                return;

            await SendConfirmAsync($"https://images.google.com/searchbyimage?image_url={av}").ConfigureAwait(false);
        }

        //done in 3.0
        [NadekoCommand, Aliases]
        public async Task Revimg([Leftover] string imageLink = null)
        {
            imageLink = imageLink?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(imageLink))
                return;
            await SendConfirmAsync($"https://images.google.com/searchbyimage?image_url={imageLink}").ConfigureAwait(false);
        }

        [NadekoCommand, Aliases]
        public async Task Wiki([Leftover] string query = null)
        {
            query = query?.Trim();

            if (!await ValidateQuery(ctx.Channel, query).ConfigureAwait(false))
                return;

            using (var http = _httpFactory.CreateClient())
            {
                var result = await http.GetStringAsync("https://en.wikipedia.org//w/api.php?action=query&format=json&prop=info&redirects=1&formatversion=2&inprop=url&titles=" + Uri.EscapeDataString(query)).ConfigureAwait(false);
                var data = JsonConvert.DeserializeObject<WikipediaApiModel>(result);
                if (data.Query.Pages[0].Missing || string.IsNullOrWhiteSpace(data.Query.Pages[0].FullUrl))
                    await ReplyErrorLocalizedAsync(strs.wiki_page_not_found).ConfigureAwait(false);
                else
                    await ctx.Channel.SendMessageAsync(data.Query.Pages[0].FullUrl).ConfigureAwait(false);
            }
        }

        [NadekoCommand, Aliases]
        public async Task Color(params SixLabors.ImageSharp.Color[] colors)
        {
            if (!colors.Any())
                return;

            var colorObjects = colors.Take(10)
                .ToArray();

            using (var img = new Image<Rgba32>(colorObjects.Length * 50, 50))
            {
                for (int i = 0; i < colorObjects.Length; i++)
                {
                    var x = i * 50;
                    img.Mutate(m => m.FillPolygon(colorObjects[i], new PointF[] {
                        new PointF(x, 0),
                        new PointF(x + 50, 0),
                        new PointF(x + 50, 50),
                        new PointF(x, 50)
                    }));
                }
                using (var ms = img.ToStream())
                {
                    await ctx.Channel.SendFileAsync(ms, $"colors.png").ConfigureAwait(false);
                }
            }
        }

        [NadekoCommand, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Avatar([Leftover] IGuildUser usr = null)
        {
            if (usr is null)
                usr = (IGuildUser)ctx.User;

            var avatarUrl = usr.RealAvatarUrl(2048);

            if (avatarUrl is null)
            {
                await ReplyErrorLocalizedAsync(strs.avatar_none(usr.ToString())).ConfigureAwait(false);
                return;
            }

            await ctx.Channel.EmbedAsync(_eb.Create().WithOkColor()
                .AddField("Username", usr.ToString(), false)
                .AddField("Avatar Url", avatarUrl, false)
                .WithThumbnailUrl(avatarUrl.ToString()), ctx.User.Mention).ConfigureAwait(false);
        }
        
        [NadekoCommand, Aliases]
        public async Task Wikia(string target, [Leftover] string query)
        {
            if (string.IsNullOrWhiteSpace(target) || string.IsNullOrWhiteSpace(query))
            {
                await ReplyErrorLocalizedAsync(strs.wikia_input_error).ConfigureAwait(false);
                return;
            }
            await ctx.Channel.TriggerTypingAsync().ConfigureAwait(false);
            using (var http = _httpFactory.CreateClient())
            {
                http.DefaultRequestHeaders.Clear();
                try
                {
                    var res = await http.GetStringAsync($"https://{Uri.EscapeUriString(target)}.fandom.com/api.php" +
                                                        $"?action=query" +
                                                        $"&format=json" +
                                                        $"&list=search" +
                                                        $"&srsearch={Uri.EscapeUriString(query)}" +
                                                        $"&srlimit=1").ConfigureAwait(false);
                    var items = JObject.Parse(res);
                    var title = items["query"]?["search"]?.FirstOrDefault()?["title"]?.ToString();
                    
                    if (string.IsNullOrWhiteSpace(title))
                    {
                        await ReplyErrorLocalizedAsync(strs.wikia_error).ConfigureAwait(false);
                        return;
                    }

                    var url = Uri.EscapeUriString($"https://{target}.fandom.com/wiki/{title}");
                    var response = $@"`{GetText(strs.title)}` {title?.SanitizeMentions()}
`{GetText(strs.url)}:` {url}";
                    await ctx.Channel.SendMessageAsync(response).ConfigureAwait(false);
                }
                catch
                {
                    await ReplyErrorLocalizedAsync(strs.wikia_error).ConfigureAwait(false);
                }
            }
        }

        [NadekoCommand, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Bible(string book, string chapterAndVerse)
        {
            var obj = new BibleVerses();
            try
            {
                using (var http = _httpFactory.CreateClient())
                {
                    var res = await http
                        .GetStringAsync("https://bible-api.com/" + book + " " + chapterAndVerse).ConfigureAwait(false);

                    obj = JsonConvert.DeserializeObject<BibleVerses>(res);
                }
            }
            catch
            {
            }
            if (obj.Error != null || obj.Verses is null || obj.Verses.Length == 0)
                await SendErrorAsync(obj.Error ?? "No verse found.").ConfigureAwait(false);
            else
            {
                var v = obj.Verses[0];
                await ctx.Channel.EmbedAsync(_eb.Create()
                    .WithOkColor()
                    .WithTitle($"{v.BookName} {v.Chapter}:{v.Verse}")
                    .WithDescription(v.Text)).ConfigureAwait(false);
            }
        }

        [NadekoCommand, Aliases]
        public async Task Steam([Leftover] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return;

            await ctx.Channel.TriggerTypingAsync().ConfigureAwait(false);

            var appId = await _service.GetSteamAppIdByName(query).ConfigureAwait(false);
            if (appId == -1)
            {
                await ReplyErrorLocalizedAsync(strs.not_found).ConfigureAwait(false);
                return;
            }

            //var embed = _eb.Create()
            //    .WithOkColor()
            //    .WithDescription(gameData.ShortDescription)
            //    .WithTitle(gameData.Name)
            //    .WithUrl(gameData.Link)
            //    .WithImageUrl(gameData.HeaderImage)
            //    .AddField(GetText(strs.genres), gameData.TotalEpisodes.ToString(), true)
            //    .AddField(GetText(strs.price), gameData.IsFree ? GetText(strs.FREE) : game, true)
            //    .AddField(GetText(strs.links), gameData.GetGenresString(), true)
            //    .WithFooter(GetText(strs.recommendations(gameData.TotalRecommendations)));
            await ctx.Channel.SendMessageAsync($"https://store.steampowered.com/app/{appId}").ConfigureAwait(false);
        }

        public async Task<bool> ValidateQuery(IMessageChannel ch, string query)
        {
            if (!string.IsNullOrWhiteSpace(query))
            {
                return true;
            }

            await ErrorLocalizedAsync(strs.specify_search_params).ConfigureAwait(false);
            return false;
        }
    }
}
