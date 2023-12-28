﻿#nullable disable
using Microsoft.Extensions.Caching.Memory;
using WizBot.Modules.Administration.Services;
using WizBot.Modules.Searches.Common;
using WizBot.Modules.Searches.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using Wiz.Common;
using Color = SixLabors.ImageSharp.Color;

namespace WizBot.Modules.Searches;

public partial class Searches : WizBotModule<SearchesService>
{
    private static readonly ConcurrentDictionary<string, string> _cachedShortenedLinks = new();
    private readonly IBotCredentials _creds;
    private readonly IGoogleApiService _google;
    private readonly IHttpClientFactory _httpFactory;
    private readonly IMemoryCache _cache;
    private readonly GuildTimezoneService _tzSvc;

    public Searches(
        IBotCredentials creds,
        IGoogleApiService google,
        IHttpClientFactory factory,
        IMemoryCache cache,
        GuildTimezoneService tzSvc)
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

        var embed = _eb.Create();
        var data = await _service.GetWeatherDataAsync(query);

        if (data is null)
            embed.WithDescription(GetText(strs.city_not_found)).WithErrorColor();
        else
        {
            var f = StandardConversions.CelsiusToFahrenheit;

            var tz = ctx.Guild is null ? TimeZoneInfo.Utc : _tzSvc.GetTimeZoneOrUtc(ctx.Guild.Id);
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

        await ctx.Channel.EmbedAsync(embed);
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

            await ReplyErrorLocalizedAsync(errorKey);
            return;
        }

        if (string.IsNullOrWhiteSpace(data.TimeZoneName))
        {
            await ReplyErrorLocalizedAsync(strs.timezone_db_api_key);
            return;
        }

        var eb = _eb.Create()
                    .WithOkColor()
                    .WithTitle(GetText(strs.time_new))
                    .WithDescription(Format.Code(data.Time.ToString(Culture)))
                    .AddField(GetText(strs.location), string.Join('\n', data.Address.Split(", ")), true)
                    .AddField(GetText(strs.timezone), data.TimeZoneName, true);

        await ctx.Channel.SendMessageAsync(embed: eb.Build());
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
            await ReplyErrorLocalizedAsync(strs.imdb_fail);
            return;
        }

        await ctx.Channel.EmbedAsync(_eb.Create()
                                        .WithOkColor()
                                        .WithTitle(movie.Title)
                                        .WithUrl($"https://www.imdb.com/title/{movie.ImdbId}/")
                                        .WithDescription(movie.Plot.TrimTo(1000))
                                        .AddField("Rating", movie.ImdbRating, true)
                                        .AddField("Genre", movie.Genre, true)
                                        .AddField("Year", movie.Year, true)
                                        .WithImageUrl(movie.Poster));
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
        return ctx.Channel.EmbedAsync(_eb.Create().WithOkColor().WithImageUrl(url));
    }

    [Cmd]
    public async Task Lmgtfy([Leftover] string ffs = null)
    {
        if (!await ValidateQuery(ffs))
            return;

        var shortenedUrl = await _google.ShortenUrl($"https://letmegooglethat.com/?q={Uri.EscapeDataString(ffs)}");
        await SendConfirmAsync($"<{shortenedUrl}>");
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

        await ctx.Channel.EmbedAsync(_eb.Create()
                                        .WithOkColor()
                                        .AddField(GetText(strs.original_url), $"<{query}>")
                                        .AddField(GetText(strs.short_url), $"<{shortLink}>"));
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
            await ReplyErrorLocalizedAsync(strs.card_not_found);
            return;
        }

        var embed = _eb.Create()
                       .WithOkColor()
                       .WithTitle(card.Name)
                       .WithDescription(card.Description)
                       .WithImageUrl(card.ImageUrl)
                       .AddField(GetText(strs.store_url), card.StoreUrl, true)
                       .AddField(GetText(strs.cost), card.ManaCost, true)
                       .AddField(GetText(strs.types), card.Types, true);

        await ctx.Channel.EmbedAsync(embed);
    }

    [Cmd]
    public async Task Hearthstone([Leftover] string name)
    {
        if (!await ValidateQuery(name))
            return;

        if (string.IsNullOrWhiteSpace(_creds.RapidApiKey))
        {
            await ReplyErrorLocalizedAsync(strs.mashape_api_missing);
            return;
        }

        await ctx.Channel.TriggerTypingAsync();
        var card = await _service.GetHearthstoneCardDataAsync(name);

        if (card is null)
        {
            await ReplyErrorLocalizedAsync(strs.card_not_found);
            return;
        }

        var embed = _eb.Create().WithOkColor().WithImageUrl(card.Img);

        if (!string.IsNullOrWhiteSpace(card.Flavor))
            embed.WithDescription(card.Flavor);

        await ctx.Channel.EmbedAsync(embed);
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
                var items = JsonConvert.DeserializeObject<UrbanResponse>(res).List;
                if (items.Any())
                {
                    await ctx.SendPaginatedConfirmAsync(0,
                        p =>
                        {
                            var item = items[p];
                            return _eb.Create()
                                      .WithOkColor()
                                      .WithUrl(item.Permalink)
                                      .WithTitle(item.Word)
                                      .WithDescription(item.Definition);
                        },
                        items.Length,
                        1);
                    return;
                }
            }
            catch
            {
            }
        }

        await ReplyErrorLocalizedAsync(strs.ud_error);
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

            var data = JsonConvert.DeserializeObject<DefineModel>(res);

            var datas = data.Results
                            .Where(x => x.Senses is not null
                                        && x.Senses.Count > 0
                                        && x.Senses[0].Definition is not null)
                            .Select(x => (Sense: x.Senses[0], x.PartOfSpeech))
                            .ToList();

            if (!datas.Any())
            {
                Log.Warning("Definition not found: {Word}", word);
                await ReplyErrorLocalizedAsync(strs.define_unknown);
            }


            var col = datas.Select(x => (
                               Definition: x.Sense.Definition is string
                                   ? x.Sense.Definition.ToString()
                                   : ((JArray)JToken.Parse(x.Sense.Definition.ToString())).First.ToString(),
                               Example: x.Sense.Examples is null || x.Sense.Examples.Count == 0
                                   ? string.Empty
                                   : x.Sense.Examples[0].Text, Word: word,
                               WordType: string.IsNullOrWhiteSpace(x.PartOfSpeech) ? "-" : x.PartOfSpeech))
                           .ToList();

            Log.Information("Sending {Count} definition for: {Word}", col.Count, word);

            await ctx.SendPaginatedConfirmAsync(0,
                page =>
                {
                    var model = col.Skip(page).First();
                    var embed = _eb.Create()
                                   .WithDescription(ctx.User.Mention)
                                   .AddField(GetText(strs.word), model.Word, true)
                                   .AddField(GetText(strs._class), model.WordType, true)
                                   .AddField(GetText(strs.definition), model.Definition)
                                   .WithOkColor();

                    if (!string.IsNullOrWhiteSpace(model.Example))
                        embed.AddField(GetText(strs.example), model.Example);

                    return embed;
                },
                col.Count,
                1);
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
        await SendConfirmAsync("🐈" + GetText(strs.catfact), fact);
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
            await ReplyErrorLocalizedAsync(strs.wiki_page_not_found);
        else
            await ctx.Channel.SendMessageAsync(data.Query.Pages[0].FullUrl);
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

        await ctx.Channel.EmbedAsync(
            _eb.Create()
               .WithOkColor()
               .AddField("Username", usr.ToString())
               .AddField("Avatar Url", avatarUrl)
               .WithThumbnailUrl(avatarUrl.ToString()),
            ctx.User.Mention);
    }

    [Cmd]
    public async Task Wikia(string target, [Leftover] string query)
    {
        if (string.IsNullOrWhiteSpace(target) || string.IsNullOrWhiteSpace(query))
        {
            await ReplyErrorLocalizedAsync(strs.wikia_input_error);
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
                await ReplyErrorLocalizedAsync(strs.wikia_error);
                return;
            }

            var url = Uri.EscapeDataString($"https://{target}.fandom.com/wiki/{title}");
            var response = $@"`{GetText(strs.title)}` {title.SanitizeMentions()}
`{GetText(strs.url)}:` {url}";
            await ctx.Channel.SendMessageAsync(response);
        }
        catch
        {
            await ReplyErrorLocalizedAsync(strs.wikia_error);
        }
    }

    [Cmd]
    public async Task Nya(string format = "img", [Remainder] string category = "neko")
    {
        // List if category to pull an image from.
        string[] img_cat =
        {
            "lizard", "cat", "neko", "no_tag_avatar", "neko_avatars_avatar", "wallpaper", "kitsune", "kiminonawa",
            "waifu", "keta_avatar", "gecg", "holo_avatar", "smug", "holo"
        };

        string[] gif_cat = { "baka", "tickle", "feed", "neko", "poke", "pat", "kiss", "hug", "cuddle", "slap", "smug" };

        // Check to see if the command is calling for a normal image or a gif.
        string[] img_format = { "img", "gif" };

        if (string.IsNullOrWhiteSpace(category))
            return;

        if (string.IsNullOrWhiteSpace(format))
            return;

        try
        {
            JToken nekotitle;
            JToken nekoimg;
            using (var http = _httpFactory.CreateClient())
            {
                nekotitle = JObject.Parse(await http.GetStringAsync($"https://api.nekos.dev/api/v3/text/cat_emote/")
                                                    .ConfigureAwait(false));
                nekoimg = JObject.Parse(await http
                                              .GetStringAsync(
                                                  $"https://api.nekos.dev/api/v3/images/sfw/{format}/{category}/")
                                              .ConfigureAwait(false));
            }

            if (img_format.Contains("img") && img_cat.Contains(category))
                await ctx.Channel.EmbedAsync(_eb.Create()
                                                .WithOkColor()
                                                .WithAuthor(
                                                    $"Nekos Life - SFW IMG Database {nekotitle["data"]["response"]["text"]}",
                                                    "https://i.imgur.com/a36AMkG.png",
                                                    "http://nekos.life/")
                                                .WithImageUrl($"{nekoimg["data"]["response"]["url"]}"))
                         .ConfigureAwait(false);
            else if (img_format.Contains("gif") && gif_cat.Contains(category))
                await ctx.Channel.EmbedAsync(_eb.Create()
                                                .WithOkColor()
                                                .WithAuthor(
                                                    $"Nekos Life - SFW GIF Database {nekotitle["data"]["response"]["text"]}",
                                                    "https://i.imgur.com/a36AMkG.png",
                                                    "http://nekos.life/")
                                                .WithImageUrl($"{nekoimg["data"]["response"]["url"]}"))
                         .ConfigureAwait(false);
            else if (img_format.Contains("img") && gif_cat.Contains(category))
                await ctx.Channel.EmbedAsync(_eb.Create()
                                                .WithErrorColor()
                                                .WithAuthor("Nekos Life - Invalid SFW IMG Category",
                                                    "https://i.imgur.com/a36AMkG.png",
                                                    "http://nekos.life/")
                                                .WithDescription(
                                                    "Seems the image category you was looking for could not be found. Please use the categories listed below.")
                                                .AddField("SFW IMG Categories",
                                                    "`kitsune`, `keta_avatar`, `no_tag_avatar`, `holo_avatar`, `neko_avatars_avatar`, `lizard`, `cat`, `gecg`, `smug`, `holo`, `wallpaper`, `kiminonawa`, `shinobu`, `neko`, `waifu`",
                                                    false))
                         .ConfigureAwait(false);
            else if (img_format.Contains("gif") && img_cat.Contains(category))
                await ctx.Channel.EmbedAsync(_eb.Create()
                                                .WithErrorColor()
                                                .WithAuthor("Nekos Life - Invalid SFW GIF Category",
                                                    "https://i.imgur.com/a36AMkG.png",
                                                    "http://nekos.life/")
                                                .WithDescription(
                                                    "Seems the gif category you was looking for could not be found. Please use the categories listed below.")
                                                .AddField("SFW GIF Categories",
                                                    "`tickle`, `poke`, `kiss`, `slap`, `cuddle`, `hug`, `pat`, `smug`, `baka`, `feed`, `neko`",
                                                    false))
                         .ConfigureAwait(false);
            else
                await ctx.Channel.EmbedAsync(_eb.Create()
                                                .WithErrorColor()
                                                .WithAuthor("Nekos Life - Invalid SFW Image Type or Category",
                                                    "https://i.imgur.com/a36AMkG.png",
                                                    "http://nekos.life/")
                                                .WithDescription(
                                                    "Seems the image type or category you was looking for could not be found. Please use the image type or categories listed below.")
                                                .AddField("SFW IMG Types", "`img`, `gif`", false)
                                                .AddField("SFW IMG Categories",
                                                    "`kitsune`, `keta_avatar`, `no_tag_avatar`, `holo_avatar`, `neko_avatars_avatar`, `lizard`, `cat`, `gecg`, `smug`, `holo`, `wallpaper`, `kiminonawa`, `shinobu`, `neko`, `waifu`",
                                                    false)
                                                .AddField("SFW GIF Categories",
                                                    "`tickle`, `poke`, `kiss`, `slap`, `cuddle`, `hug`, `pat`, `smug`, `baka`, `feed`, `neko`",
                                                    false))
                         .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await SendErrorAsync(ex.Message).ConfigureAwait(false);
        }
    }

    // Waifu Gen Command

    [Cmd]
    public async Task GWaifu()
    {
        try
        {
            using (var http = _httpFactory.CreateClient())
            {
                //var waifutxt = await http.GetStringAsync($"https://www.thiswaifudoesnotexist.net/snippet-{new WizBotRandom().Next(0, 100000)}.txt").ConfigureAwait(false);
                await ctx.Channel.EmbedAsync(_eb.Create()
                                                .WithOkColor()
                                                .WithAuthor("This Waifu Does Not Exist",
                                                    null,
                                                    "https://www.thiswaifudoesnotexist.net")
                                                .WithImageUrl(
                                                    $"https://www.thiswaifudoesnotexist.net/example-{new WizBotRandom().Next(0, 100000)}.jpg"))
                         //.WithDescription($"{waifutxt}".TrimTo(1000)))
                         .ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            await SendErrorAsync(ex.Message).ConfigureAwait(false);
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
            var res = await http.GetStringAsync($"https://bible-api.com/{book} {chapterAndVerse}");

            obj = JsonConvert.DeserializeObject<BibleVerses>(res);
        }
        catch
        {
        }

        if (obj.Error is not null || obj.Verses is null || obj.Verses.Length == 0)
            await SendErrorAsync(obj.Error ?? "No verse found.");
        else
        {
            var v = obj.Verses[0];
            await ctx.Channel.EmbedAsync(_eb.Create()
                                            .WithOkColor()
                                            .WithTitle($"{v.BookName} {v.Chapter}:{v.Verse}")
                                            .WithDescription(v.Text));
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
            await ReplyErrorLocalizedAsync(strs.not_found);
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
        await ctx.Channel.SendMessageAsync($"https://store.steampowered.com/app/{appId}");
    }

    private async Task<bool> ValidateQuery([MaybeNullWhen(false)] string query)
    {
        if (!string.IsNullOrWhiteSpace(query))
            return true;

        await ErrorLocalizedAsync(strs.specify_search_params);
        return false;
    }

    public class ShortenData
    {
        [JsonProperty("result_url")]
        public string ResultUrl { get; set; }
    }
}