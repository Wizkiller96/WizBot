#nullable disable
using Microsoft.Extensions.Caching.Memory;
using NadekoBot.Modules.Searches.Common;
using NadekoBot.Modules.Searches.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Json;
using Color = SixLabors.ImageSharp.Color;

namespace NadekoBot.Modules.Searches;

public partial class Searches : NadekoModule<SearchesService>
{
    private static readonly ConcurrentDictionary<string, string> _cachedShortenedLinks = new();
    private readonly IBotCredentials _creds;
    private readonly IGoogleApiService _google;
    private readonly IHttpClientFactory _httpFactory;
    private readonly IMemoryCache _cache;
    private readonly ITimezoneService _tzSvc;

    public Searches(
        IBotCredentials creds,
        IGoogleApiService google,
        IHttpClientFactory factory,
        IMemoryCache cache,
        ITimezoneService tzSvc)
    {
        _creds = creds;
        _google = google;
        _httpFactory = factory;
        _cache = cache;
        _tzSvc = tzSvc;
    }

    [Cmd]
    public async Task Rip([Leftover] IGuildUser usr)
    {
        var av = usr.RealAvatarUrl();
        await using var picStream = await _service.GetRipPictureAsync(usr.Nickname ?? usr.Username, av);
        await ctx.Channel.SendFileAsync(picStream,
            "rip.png",
            $"Rip {Format.Bold(usr.ToString())} \n\t- " + Format.Italics(ctx.User.ToString()));
    }

    [Cmd]
    public async Task Weather([Leftover] string query)
    {
        if (!await ValidateQuery(query))
            return;

        var embed = _sender.CreateEmbed();
        var data = await _service.GetWeatherDataAsync(query);

        if (data is null)
            embed.WithDescription(GetText(strs.city_not_found)).WithErrorColor();
        else
        {
            var f = StandardConversions.CelsiusToFahrenheit;

            var tz = _tzSvc.GetTimeZoneOrUtc(ctx.Guild?.Id);
            var sunrise = data.Sys.Sunrise.ToUnixTimestamp();
            var sunset = data.Sys.Sunset.ToUnixTimestamp();
            sunrise = sunrise.ToOffset(tz.GetUtcOffset(sunrise));
            sunset = sunset.ToOffset(tz.GetUtcOffset(sunset));
            var timezone = $"UTC{sunrise:zzz}";

            embed
                .AddField("🌍 " + Format.Bold(GetText(strs.location)),
                    $"[{data.Name + ", " + data.Sys.Country}](https://openweathermap.org/city/{data.Id})",
                    true)
                .AddField("📏 " + Format.Bold(GetText(strs.latlong)), $"{data.Coord.Lat}, {data.Coord.Lon}", true)
                .AddField("☁ " + Format.Bold(GetText(strs.condition)),
                    string.Join(", ", data.Weather.Select(w => w.Main)),
                    true)
                .AddField("😓 " + Format.Bold(GetText(strs.humidity)), $"{data.Main.Humidity}%", true)
                .AddField("💨 " + Format.Bold(GetText(strs.wind_speed)), data.Wind.Speed + " m/s", true)
                .AddField("🌡 " + Format.Bold(GetText(strs.temperature)),
                    $"{data.Main.Temp:F1}°C / {f(data.Main.Temp):F1}°F",
                    true)
                .AddField("🔆 " + Format.Bold(GetText(strs.min_max)),
                    $"{data.Main.TempMin:F1}°C - {data.Main.TempMax:F1}°C\n{f(data.Main.TempMin):F1}°F - {f(data.Main.TempMax):F1}°F",
                    true)
                .AddField("🌄 " + Format.Bold(GetText(strs.sunrise)), $"{sunrise:HH:mm} {timezone}", true)
                .AddField("🌇 " + Format.Bold(GetText(strs.sunset)), $"{sunset:HH:mm} {timezone}", true)
                .WithOkColor()
                .WithFooter("Powered by openweathermap.org",
                    $"https://openweathermap.org/img/w/{data.Weather[0].Icon}.png");
        }

        await Response().Embed(embed).SendAsync();
    }

    [Cmd]
    public async Task Time([Leftover] string query)
    {
        if (!await ValidateQuery(query))
            return;

        await ctx.Channel.TriggerTypingAsync();

        var (data, err) = await _service.GetTimeDataAsync(query);
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

            await Response().Error(errorKey).SendAsync();
            return;
        }

        if (string.IsNullOrWhiteSpace(data.TimeZoneName))
        {
            await Response().Error(strs.timezone_db_api_key).SendAsync();
            return;
        }

        var eb = _sender.CreateEmbed()
                 .WithOkColor()
                 .WithTitle(GetText(strs.time_new))
                 .WithDescription(Format.Code(data.Time.ToString(Culture)))
                 .AddField(GetText(strs.location), string.Join('\n', data.Address.Split(", ")), true)
                 .AddField(GetText(strs.timezone), data.TimeZoneName, true);

        await Response().Embed(eb).SendAsync();
    }

    [Cmd]
    public async Task Movie([Leftover] string query = null)
    {
        if (!await ValidateQuery(query))
            return;

        await ctx.Channel.TriggerTypingAsync();

        var movie = await _service.GetMovieDataAsync(query);
        if (movie is null)
        {
            await Response().Error(strs.imdb_fail).SendAsync();
            return;
        }

        await Response()
              .Embed(_sender.CreateEmbed()
                     .WithOkColor()
                     .WithTitle(movie.Title)
                     .WithUrl($"https://www.imdb.com/title/{movie.ImdbId}/")
                     .WithDescription(movie.Plot.TrimTo(1000))
                     .AddField("Rating", movie.ImdbRating, true)
                     .AddField("Genre", movie.Genre, true)
                     .AddField("Year", movie.Year, true)
                     .WithImageUrl(movie.Poster))
              .SendAsync();
    }

    [Cmd]
    public Task RandomCat()
        => InternalRandomImage(SearchesService.ImageTag.Cats);

    [Cmd]
    public Task RandomDog()
        => InternalRandomImage(SearchesService.ImageTag.Dogs);

    [Cmd]
    public Task RandomFood()
        => InternalRandomImage(SearchesService.ImageTag.Food);

    [Cmd]
    public Task RandomBird()
        => InternalRandomImage(SearchesService.ImageTag.Birds);

    private Task InternalRandomImage(SearchesService.ImageTag tag)
    {
        var url = _service.GetRandomImageUrl(tag);
        return Response().Embed(_sender.CreateEmbed().WithOkColor().WithImageUrl(url)).SendAsync();
    }

    [Cmd]
    public async Task Lmgtfy([Leftover] string ffs = null)
    {
        if (!await ValidateQuery(ffs))
            return;

        var shortenedUrl = await _google.ShortenUrl($"https://letmegooglethat.com/?q={Uri.EscapeDataString(ffs)}");
        await Response().Confirm($"<{shortenedUrl}>").SendAsync();
    }

    [Cmd]
    public async Task Shorten([Leftover] string query)
    {
        if (!await ValidateQuery(query))
            return;

        query = query.Trim();
        if (!_cachedShortenedLinks.TryGetValue(query, out var shortLink))
        {
            try
            {
                using var http = _httpFactory.CreateClient();
                using var req = new HttpRequestMessage(HttpMethod.Post, "https://goolnk.com/api/v1/shorten");
                var formData = new MultipartFormDataContent
                {
                    { new StringContent(query), "url" }
                };
                req.Content = formData;

                using var res = await http.SendAsync(req);
                var content = await res.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<ShortenData>(content);

                if (!string.IsNullOrWhiteSpace(data?.ResultUrl))
                    _cachedShortenedLinks.TryAdd(query, data.ResultUrl);
                else
                    return;

                shortLink = data.ResultUrl;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error shortening a link: {Message}", ex.Message);
                return;
            }
        }

        await Response()
              .Embed(_sender.CreateEmbed()
                     .WithOkColor()
                     .AddField(GetText(strs.original_url), $"<{query}>")
                     .AddField(GetText(strs.short_url), $"<{shortLink}>"))
              .SendAsync();
    }

    [Cmd]
    public async Task MagicTheGathering([Leftover] string search)
    {
        if (!await ValidateQuery(search))
            return;

        await ctx.Channel.TriggerTypingAsync();
        var card = await _service.GetMtgCardAsync(search);

        if (card is null)
        {
            await Response().Error(strs.card_not_found).SendAsync();
            return;
        }

        var embed = _sender.CreateEmbed()
                    .WithOkColor()
                    .WithTitle(card.Name)
                    .WithDescription(card.Description)
                    .WithImageUrl(card.ImageUrl)
                    .AddField(GetText(strs.store_url), card.StoreUrl, true)
                    .AddField(GetText(strs.cost), card.ManaCost, true)
                    .AddField(GetText(strs.types), card.Types, true);

        await Response().Embed(embed).SendAsync();
    }

    [Cmd]
    public async Task Hearthstone([Leftover] string name)
    {
        if (!await ValidateQuery(name))
            return;

        if (string.IsNullOrWhiteSpace(_creds.RapidApiKey))
        {
            await Response().Error(strs.mashape_api_missing).SendAsync();
            return;
        }

        await ctx.Channel.TriggerTypingAsync();
        var card = await _service.GetHearthstoneCardDataAsync(name);

        if (card is null)
        {
            await Response().Error(strs.card_not_found).SendAsync();
            return;
        }

        var embed = _sender.CreateEmbed().WithOkColor().WithImageUrl(card.Img);

        if (!string.IsNullOrWhiteSpace(card.Flavor))
            embed.WithDescription(card.Flavor);

        await Response().Embed(embed).SendAsync();
    }

    [Cmd]
    public async Task UrbanDict([Leftover] string query = null)
    {
        if (!await ValidateQuery(query))
            return;

        await ctx.Channel.TriggerTypingAsync();
        using (var http = _httpFactory.CreateClient())
        {
            var res = await http.GetStringAsync(
                $"https://api.urbandictionary.com/v0/define?term={Uri.EscapeDataString(query)}");
            try
            {
                var allItems = JsonConvert.DeserializeObject<UrbanResponse>(res).List;
                if (allItems.Any())
                {
                    await Response()
                          .Paginated()
                          .Items(allItems)
                          .PageSize(1)
                          .CurrentPage(0)
                          .Page((items, _) =>
                          {
                              var item = items[0];
                              return _sender.CreateEmbed()
                                     .WithOkColor()
                                     .WithUrl(item.Permalink)
                                     .WithTitle(item.Word)
                                     .WithDescription(item.Definition);
                          })
                          .SendAsync();
                    return;
                }
            }
            catch
            {
            }
        }

        await Response().Error(strs.ud_error).SendAsync();
    }

    [Cmd]
    public async Task Define([Leftover] string word)
    {
        if (!await ValidateQuery(word))
            return;

        using var http = _httpFactory.CreateClient();
        string res;
        try
        {
            res = await _cache.GetOrCreateAsync($"define_{word}",
                e =>
                {
                    e.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12);
                    return http.GetStringAsync("https://api.pearson.com/v2/dictionaries/entries?headword="
                                               + WebUtility.UrlEncode(word));
                });

            var responseModel = JsonConvert.DeserializeObject<DefineModel>(res);

            var data = responseModel.Results
                                    .Where(x => x.Senses is not null
                                                && x.Senses.Count > 0
                                                && x.Senses[0].Definition is not null)
                                    .Select(x => (Sense: x.Senses[0], x.PartOfSpeech))
                                    .ToList();

            if (!data.Any())
            {
                Log.Warning("Definition not found: {Word}", word);
                await Response().Error(strs.define_unknown).SendAsync();
            }


            var col = data.Select(x => (
                              Definition: x.Sense.Definition is string
                                  ? x.Sense.Definition.ToString()
                                  : ((JArray)JToken.Parse(x.Sense.Definition.ToString())).First.ToString(),
                              Example: x.Sense.Examples is null || x.Sense.Examples.Count == 0
                                  ? string.Empty
                                  : x.Sense.Examples[0].Text, Word: word,
                              WordType: string.IsNullOrWhiteSpace(x.PartOfSpeech) ? "-" : x.PartOfSpeech))
                          .ToList();

            Log.Information("Sending {Count} definition for: {Word}", col.Count, word);

            await Response()
                  .Paginated()
                  .Items(col)
                  .PageSize(1)
                  .Page((items, _) =>
                  {
                      var model = items.First();
                      var embed = _sender.CreateEmbed()
                                  .WithDescription(ctx.User.Mention)
                                  .AddField(GetText(strs.word), model.Word, true)
                                  .AddField(GetText(strs._class), model.WordType, true)
                                  .AddField(GetText(strs.definition), model.Definition)
                                  .WithOkColor();

                      if (!string.IsNullOrWhiteSpace(model.Example))
                          embed.AddField(GetText(strs.example), model.Example);

                      return embed;
                  })
                  .SendAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving definition data for: {Word}", word);
        }
    }

    [Cmd]
    public async Task Catfact()
    {
        using var http = _httpFactory.CreateClient();
        var response = await http.GetStringAsync("https://catfact.ninja/fact");

        var fact = JObject.Parse(response)["fact"].ToString();
        await Response().Confirm("🐈" + GetText(strs.catfact), fact).SendAsync();
    }

    [Cmd]
    public async Task Wiki([Leftover] string query = null)
    {
        query = query?.Trim();

        if (!await ValidateQuery(query))
            return;

        using var http = _httpFactory.CreateClient();
        var result = await http.GetStringAsync(
            "https://en.wikipedia.org//w/api.php?action=query&format=json&prop=info&redirects=1&formatversion=2&inprop=url&titles="
            + Uri.EscapeDataString(query));
        var data = JsonConvert.DeserializeObject<WikipediaApiModel>(result);
        if (data.Query.Pages[0].Missing || string.IsNullOrWhiteSpace(data.Query.Pages[0].FullUrl))
            await Response().Error(strs.wiki_page_not_found).SendAsync();
        else
            await Response().Text(data.Query.Pages[0].FullUrl).SendAsync();
    }

    [Cmd]
    public async Task Color(params Color[] colors)
    {
        if (!colors.Any())
            return;

        var colorObjects = colors.Take(10).ToArray();

        using var img = new Image<Rgba32>(colorObjects.Length * 50, 50);
        for (var i = 0; i < colorObjects.Length; i++)
        {
            var x = i * 50;
            img.Mutate(m => m.FillPolygon(colorObjects[i], new(x, 0), new(x + 50, 0), new(x + 50, 50), new(x, 50)));
        }

        await using var ms = img.ToStream();
        await ctx.Channel.SendFileAsync(ms, "colors.png");
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    public async Task Avatar([Leftover] IGuildUser usr = null)
    {
        if (usr is null)
            usr = (IGuildUser)ctx.User;

        var avatarUrl = usr.RealAvatarUrl(2048);

        await Response()
              .Embed(
                  _sender.CreateEmbed()
                      .WithOkColor()
                      .AddField("Username", usr.ToString())
                      .AddField("Avatar Url", avatarUrl)
                      .WithThumbnailUrl(avatarUrl.ToString()))
              .SendAsync();
    }

    [Cmd]
    public async Task Wikia(string target, [Leftover] string query)
    {
        if (string.IsNullOrWhiteSpace(target) || string.IsNullOrWhiteSpace(query))
        {
            await Response().Error(strs.wikia_input_error).SendAsync();
            return;
        }

        await ctx.Channel.TriggerTypingAsync();
        using var http = _httpFactory.CreateClient();
        http.DefaultRequestHeaders.Clear();
        try
        {
            var res = await http.GetStringAsync($"https://{Uri.EscapeDataString(target)}.fandom.com/api.php"
                                                + "?action=query"
                                                + "&format=json"
                                                + "&list=search"
                                                + $"&srsearch={Uri.EscapeDataString(query)}"
                                                + "&srlimit=1");
            var items = JObject.Parse(res);
            var title = items["query"]?["search"]?.FirstOrDefault()?["title"]?.ToString();

            if (string.IsNullOrWhiteSpace(title))
            {
                await Response().Error(strs.wikia_error).SendAsync();
                return;
            }

            var url = Uri.EscapeDataString($"https://{target}.fandom.com/wiki/{title}");
            var response = $@"`{GetText(strs.title)}` {title.SanitizeMentions()}
`{GetText(strs.url)}:` {url}";
            await Response().Text(response).SendAsync();
        }
        catch
        {
            await Response().Error(strs.wikia_error).SendAsync();
        }
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    public async Task Bible(string book, string chapterAndVerse)
    {
        var obj = new BibleVerses();
        try
        {
            using var http = _httpFactory.CreateClient();
            obj = await http.GetFromJsonAsync<BibleVerses>($"https://bible-api.com/{book} {chapterAndVerse}");
        }
        catch
        {
        }

        if (obj.Error is not null || obj.Verses is null || obj.Verses.Length == 0)
            await Response().Error(obj.Error ?? "No verse found.").SendAsync();
        else
        {
            var v = obj.Verses[0];
            await Response()
                  .Embed(_sender.CreateEmbed()
                         .WithOkColor()
                         .WithTitle($"{v.BookName} {v.Chapter}:{v.Verse}")
                         .WithDescription(v.Text))
                  .SendAsync();
        }
    }

    [Cmd]
    public async Task Steam([Leftover] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return;

        await ctx.Channel.TriggerTypingAsync();

        var appId = await _service.GetSteamAppIdByName(query);
        if (appId == -1)
        {
            await Response().Error(strs.not_found).SendAsync();
            return;
        }

        //var embed = _sender.CreateEmbed()
        //    .WithOkColor()
        //    .WithDescription(gameData.ShortDescription)
        //    .WithTitle(gameData.Name)
        //    .WithUrl(gameData.Link)
        //    .WithImageUrl(gameData.HeaderImage)
        //    .AddField(GetText(strs.genres), gameData.TotalEpisodes.ToString(), true)
        //    .AddField(GetText(strs.price), gameData.IsFree ? GetText(strs.FREE) : game, true)
        //    .AddField(GetText(strs.links), gameData.GetGenresString(), true)
        //    .WithFooter(GetText(strs.recommendations(gameData.TotalRecommendations)));
        await Response().Text($"https://store.steampowered.com/app/{appId}").SendAsync();
    }

    private async Task<bool> ValidateQuery([MaybeNullWhen(false)] string query)
    {
        if (!string.IsNullOrWhiteSpace(query))
            return true;

        await Response().Error(strs.specify_search_params).SendAsync();
        return false;
    }

    public class ShortenData
    {
        [JsonProperty("result_url")]
        public string ResultUrl { get; set; }
    }
}