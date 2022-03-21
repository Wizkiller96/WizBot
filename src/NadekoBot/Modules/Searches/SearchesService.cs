#nullable disable
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Html2Markdown;
using NadekoBot.Modules.Searches.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Net;
using Color = SixLabors.ImageSharp.Color;
using Image = SixLabors.ImageSharp.Image;

namespace NadekoBot.Modules.Searches.Services;

public class SearchesService : INService
{
    public enum ImageTag
    {
        Food,
        Dogs,
        Cats,
        Birds
    }

    private static readonly HtmlParser _googleParser = new(new()
    {
        IsScripting = false,
        IsEmbedded = false,
        IsSupportingProcessingInstructions = false,
        IsKeepingSourceReferences = false,
        IsNotSupportingFrames = true
    });

    public List<WoWJoke> WowJokes { get; } = new();
    public List<MagicItem> MagicItems { get; } = new();
    private readonly IHttpClientFactory _httpFactory;
    private readonly IGoogleApiService _google;
    private readonly IImageCache _imgs;
    private readonly IDataCache _cache;
    private readonly FontProvider _fonts;
    private readonly IBotCredentials _creds;
    private readonly NadekoRandom _rng;
    private readonly List<string> _yomamaJokes;

    private readonly object _yomamaLock = new();
    private int yomamaJokeIndex;

    public SearchesService(
        IGoogleApiService google,
        IDataCache cache,
        IHttpClientFactory factory,
        FontProvider fonts,
        IBotCredentials creds)
    {
        _httpFactory = factory;
        _google = google;
        _imgs = cache.LocalImages;
        _cache = cache;
        _fonts = fonts;
        _creds = creds;
        _rng = new();

        //joke commands
        if (File.Exists("data/wowjokes.json"))
            WowJokes = JsonConvert.DeserializeObject<List<WoWJoke>>(File.ReadAllText("data/wowjokes.json"));
        else
            Log.Warning("data/wowjokes.json is missing. WOW Jokes are not loaded");

        if (File.Exists("data/magicitems.json"))
            MagicItems = JsonConvert.DeserializeObject<List<MagicItem>>(File.ReadAllText("data/magicitems.json"));
        else
            Log.Warning("data/magicitems.json is missing. Magic items are not loaded");

        if (File.Exists("data/yomama.txt"))
            _yomamaJokes = File.ReadAllLines("data/yomama.txt").Shuffle().ToList();
        else
        {
            _yomamaJokes = new();
            Log.Warning("data/yomama.txt is missing. .yomama command won't work");
        }
    }

    public async Task<Stream> GetRipPictureAsync(string text, Uri imgUrl)
    {
        var data = await _cache.GetOrAddCachedDataAsync($"nadeko_rip_{text}_{imgUrl}",
            GetRipPictureFactory,
            (text, imgUrl),
            TimeSpan.FromDays(1));

        return data.ToStream();
    }

    private void DrawAvatar(Image bg, Image avatarImage)
        => bg.Mutate(x => x.Grayscale().DrawImage(avatarImage, new(83, 139), new GraphicsOptions()));

    public async Task<byte[]> GetRipPictureFactory((string text, Uri avatarUrl) arg)
    {
        var (text, avatarUrl) = arg;
        using var bg = Image.Load<Rgba32>(_imgs.Rip.ToArray());
        var (succ, data) = (false, (byte[])null); //await _cache.TryGetImageDataAsync(avatarUrl);
        if (!succ)
        {
            using var http = _httpFactory.CreateClient();
            data = await http.GetByteArrayAsync(avatarUrl);
            using (var avatarImg = Image.Load<Rgba32>(data))
            {
                avatarImg.Mutate(x => x.Resize(85, 85).ApplyRoundedCorners(42));
                await using var avStream = avatarImg.ToStream();
                data = avStream.ToArray();
                DrawAvatar(bg, avatarImg);
            }

            await _cache.SetImageDataAsync(avatarUrl, data);
        }
        else
        {
            using var avatarImg = Image.Load<Rgba32>(data);
            DrawAvatar(bg, avatarImg);
        }

        bg.Mutate(x => x.DrawText(
            new()
            {
                TextOptions = new TextOptions
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    WrapTextWidth = 190
                }.WithFallbackFonts(_fonts.FallBackFonts)
            },
            text,
            _fonts.RipFont,
            Color.Black,
            new(25, 225)));

        //flowa
        using (var flowers = Image.Load(_imgs.RipOverlay.ToArray()))
        {
            bg.Mutate(x => x.DrawImage(flowers, new(0, 0), new GraphicsOptions()));
        }

        await using var stream = bg.ToStream();
        return stream.ToArray();
    }

    public Task<WeatherData> GetWeatherDataAsync(string query)
    {
        query = query.Trim().ToLowerInvariant();

        return _cache.GetOrAddCachedDataAsync($"nadeko_weather_{query}",
            GetWeatherDataFactory,
            query,
            TimeSpan.FromHours(3));
    }

    private async Task<WeatherData> GetWeatherDataFactory(string query)
    {
        using var http = _httpFactory.CreateClient();
        try
        {
            var data = await http.GetStringAsync("http://api.openweathermap.org/data/2.5/weather?"
                                                 + $"q={query}&"
                                                 + "appid=42cd627dd60debf25a5739e50a217d74&"
                                                 + "units=metric");

            if (string.IsNullOrWhiteSpace(data))
                return null;

            return JsonConvert.DeserializeObject<WeatherData>(data);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error getting weather data");
            return null;
        }
    }

    public Task<((string Address, DateTime Time, string TimeZoneName), TimeErrors?)> GetTimeDataAsync(string arg)
        => GetTimeDataFactory(arg);

    //return _cache.GetOrAddCachedDataAsync($"nadeko_time_{arg}",
    //    GetTimeDataFactory,
    //    arg,
    //    TimeSpan.FromMinutes(1));
    private async Task<((string Address, DateTime Time, string TimeZoneName), TimeErrors?)> GetTimeDataFactory(
        string query)
    {
        query = query.Trim();

        if (string.IsNullOrEmpty(query))
            return (default, TimeErrors.InvalidInput);

        if (string.IsNullOrWhiteSpace(_creds.LocationIqApiKey) || string.IsNullOrWhiteSpace(_creds.TimezoneDbApiKey))
            return (default, TimeErrors.ApiKeyMissing);

        try
        {
            using var http = _httpFactory.CreateClient();
            var res = await _cache.GetOrAddCachedDataAsync($"geo_{query}",
                _ =>
                {
                    var url = "https://eu1.locationiq.com/v1/search.php?"
                              + (string.IsNullOrWhiteSpace(_creds.LocationIqApiKey)
                                  ? "key="
                                  : $"key={_creds.LocationIqApiKey}&")
                              + $"q={Uri.EscapeDataString(query)}&"
                              + "format=json";

                    var res = http.GetStringAsync(url);
                    return res;
                },
                "",
                TimeSpan.FromHours(1));

            var responses = JsonConvert.DeserializeObject<LocationIqResponse[]>(res);
            if (responses is null || responses.Length == 0)
            {
                Log.Warning("Geocode lookup failed for: {Query}", query);
                return (default, TimeErrors.NotFound);
            }

            var geoData = responses[0];

            using var req = new HttpRequestMessage(HttpMethod.Get,
                "http://api.timezonedb.com/v2.1/get-time-zone?"
                + $"key={_creds.TimezoneDbApiKey}"
                + $"&format=json"
                + $"&by=position"
                + $"&lat={geoData.Lat}"
                + $"&lng={geoData.Lon}");

            using var geoRes = await http.SendAsync(req);
            var resString = await geoRes.Content.ReadAsStringAsync();
            var timeObj = JsonConvert.DeserializeObject<TimeZoneResult>(resString);

            var time = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(timeObj.Timestamp);

            return ((Address: responses[0].DisplayName, Time: time, TimeZoneName: timeObj.TimezoneName), default);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Weather error: {Message}", ex.Message);
            return (default, TimeErrors.NotFound);
        }
    }

    public string GetRandomImageUrl(ImageTag tag)
    {
        var subpath = tag.ToString().ToLowerInvariant();

        int max;
        switch (tag)
        {
            case ImageTag.Food:
                max = 773;
                break;
            case ImageTag.Dogs:
                max = 750;
                break;
            case ImageTag.Cats:
                max = 773;
                break;
            case ImageTag.Birds:
                max = 578;
                break;
            default:
                max = 100;
                break;
        }

        return $"https://nadeko-pictures.nyc3.digitaloceanspaces.com/{subpath}/"
               + _rng.Next(1, max).ToString("000")
               + ".png";
    }

    public Task<string> GetYomamaJoke()
    {
        string joke;
        lock (_yomamaLock)
        {
            if (yomamaJokeIndex >= _yomamaJokes.Count)
            {
                yomamaJokeIndex = 0;
                var newList = _yomamaJokes.ToList();
                _yomamaJokes.Clear();
                _yomamaJokes.AddRange(newList.Shuffle());
            }

            joke = _yomamaJokes[yomamaJokeIndex++];
        }

        return Task.FromResult(joke);

        // using (var http = _httpFactory.CreateClient())
        // {
        //     var response = await http.GetStringAsync(new Uri("http://api.yomomma.info/"));
        //     return JObject.Parse(response)["joke"].ToString() + " 😆";
        // }
    }

    public async Task<(string Setup, string Punchline)> GetRandomJoke()
    {
        using var http = _httpFactory.CreateClient();
        var res = await http.GetStringAsync("https://official-joke-api.appspot.com/random_joke");
        var resObj = JsonConvert.DeserializeAnonymousType(res,
            new
            {
                setup = "",
                punchline = ""
            });
        return (resObj.setup, resObj.punchline);
    }

    public async Task<string> GetChuckNorrisJoke()
    {
        using var http = _httpFactory.CreateClient();
        var response = await http.GetStringAsync(new Uri("http://api.icndb.com/jokes/random/"));
        return JObject.Parse(response)["value"]["joke"] + " 😆";
    }

    public async Task<MtgData> GetMtgCardAsync(string search)
    {
        search = search.Trim().ToLowerInvariant();
        var data = await _cache.GetOrAddCachedDataAsync($"nadeko_mtg_{search}",
            GetMtgCardFactory,
            search,
            TimeSpan.FromDays(1));

        if (data is null || data.Length == 0)
            return null;

        return data[_rng.Next(0, data.Length)];
    }

    private async Task<MtgData[]> GetMtgCardFactory(string search)
    {
        async Task<MtgData> GetMtgDataAsync(MtgResponse.Data card)
        {
            string storeUrl;
            try
            {
                storeUrl = await _google.ShortenUrl("https://shop.tcgplayer.com/productcatalog/product/show?"
                                                    + "newSearch=false&"
                                                    + "ProductType=All&"
                                                    + "IsProductNameExact=false&"
                                                    + $"ProductName={Uri.EscapeDataString(card.Name)}");
            }
            catch { storeUrl = "<url can't be found>"; }

            return new()
            {
                Description = card.Text,
                Name = card.Name,
                ImageUrl = card.ImageUrl,
                StoreUrl = storeUrl,
                Types = string.Join(",\n", card.Types),
                ManaCost = card.ManaCost
            };
        }

        using var http = _httpFactory.CreateClient();
        http.DefaultRequestHeaders.Clear();
        var response =
            await http.GetStringAsync($"https://api.magicthegathering.io/v1/cards?name={Uri.EscapeDataString(search)}");

        var responseObject = JsonConvert.DeserializeObject<MtgResponse>(response);
        if (responseObject is null)
            return Array.Empty<MtgData>();

        var cards = responseObject.Cards.Take(5).ToArray();
        if (cards.Length == 0)
            return Array.Empty<MtgData>();

        return await cards.Select(GetMtgDataAsync).WhenAll();
    }

    public Task<HearthstoneCardData> GetHearthstoneCardDataAsync(string name)
    {
        name = name.ToLowerInvariant();
        return _cache.GetOrAddCachedDataAsync($"nadeko_hearthstone_{name}",
            HearthstoneCardDataFactory,
            name,
            TimeSpan.FromDays(1));
    }

    private async Task<HearthstoneCardData> HearthstoneCardDataFactory(string name)
    {
        using var http = _httpFactory.CreateClient();
        http.DefaultRequestHeaders.Clear();
        http.DefaultRequestHeaders.Add("x-rapidapi-key", _creds.RapidApiKey);
        try
        {
            var response = await http.GetStringAsync("https://omgvamp-hearthstone-v1.p.rapidapi.com/"
                                                     + $"cards/search/{Uri.EscapeDataString(name)}");
            var objs = JsonConvert.DeserializeObject<HearthstoneCardData[]>(response);
            if (objs is null || objs.Length == 0)
                return null;
            var data = objs.FirstOrDefault(x => x.Collectible)
                       ?? objs.FirstOrDefault(x => !string.IsNullOrEmpty(x.PlayerClass)) ?? objs.FirstOrDefault();
            if (data is null)
                return null;
            if (!string.IsNullOrWhiteSpace(data.Img))
                data.Img = await _google.ShortenUrl(data.Img);
            if (!string.IsNullOrWhiteSpace(data.Text))
            {
                var converter = new Converter();
                data.Text = converter.Convert(data.Text);
            }

            return data;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting Hearthstone Card: {ErrorMessage}", ex.Message);
            return null;
        }
    }

    public Task<OmdbMovie> GetMovieDataAsync(string name)
    {
        name = name.Trim().ToLowerInvariant();
        return _cache.GetOrAddCachedDataAsync($"nadeko_movie_{name}", GetMovieDataFactory, name, TimeSpan.FromDays(1));
    }

    private async Task<OmdbMovie> GetMovieDataFactory(string name)
    {
        using var http = _httpFactory.CreateClient();
        var res = await http.GetStringAsync(string.Format("https://omdbapi.nadeko.bot/?t={0}&y=&plot=full&r=json",
            name.Trim().Replace(' ', '+')));
        var movie = JsonConvert.DeserializeObject<OmdbMovie>(res);
        if (movie?.Title is null)
            return null;
        movie.Poster = await _google.ShortenUrl(movie.Poster);
        return movie;
    }

    public async Task<int> GetSteamAppIdByName(string query)
    {
        const string steamGameIdsKey = "steam_names_to_appid";
        // var exists = await db.KeyExistsAsync(steamGameIdsKey);

        // if we didn't get steam name to id map already, get it
        //if (!exists)
        //{
        //    using (var http = _httpFactory.CreateClient())
        //    {
        //        // https://api.steampowered.com/ISteamApps/GetAppList/v2/
        //        var gamesStr = await http.GetStringAsync("https://api.steampowered.com/ISteamApps/GetAppList/v2/");
        //        var apps = JsonConvert.DeserializeAnonymousType(gamesStr, new { applist = new { apps = new List<SteamGameId>() } }).applist.apps;

        //        //await db.HashSetAsync("steam_game_ids", apps.Select(app => new HashEntry(app.Name.Trim().ToLowerInvariant(), app.AppId)).ToArray());
        //        await db.StringSetAsync("steam_game_ids", gamesStr, TimeSpan.FromHours(24));
        //        //await db.KeyExpireAsync("steam_game_ids", TimeSpan.FromHours(24), CommandFlags.FireAndForget);
        //    }
        //}

        var gamesMap = await _cache.GetOrAddCachedDataAsync(steamGameIdsKey,
            async _ =>
            {
                using var http = _httpFactory.CreateClient();
                // https://api.steampowered.com/ISteamApps/GetAppList/v2/
                var gamesStr = await http.GetStringAsync("https://api.steampowered.com/ISteamApps/GetAppList/v2/");
                var apps = JsonConvert
                           .DeserializeAnonymousType(gamesStr,
                               new
                               {
                                   applist = new
                                   {
                                       apps = new List<SteamGameId>()
                                   }
                               })
                           .applist.apps;

                return apps.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                           .GroupBy(x => x.Name)
                           .ToDictionary(x => x.Key, x => x.First().AppId);
                //await db.HashSetAsync("steam_game_ids", apps.Select(app => new HashEntry(app.Name.Trim().ToLowerInvariant(), app.AppId)).ToArray());
                //await db.StringSetAsync("steam_game_ids", gamesStr, TimeSpan.FromHours(24));
                //await db.KeyExpireAsync("steam_game_ids", TimeSpan.FromHours(24), CommandFlags.FireAndForget);
            },
            default(string),
            TimeSpan.FromHours(24));

        if (gamesMap is null)
            return -1;


        query = query.Trim();

        var keyList = gamesMap.Keys.ToList();

        var key = keyList.FirstOrDefault(x => x.Equals(query, StringComparison.OrdinalIgnoreCase));

        if (key == default)
        {
            key = keyList.FirstOrDefault(x => x.StartsWith(query, StringComparison.OrdinalIgnoreCase));
            if (key == default)
                return -1;
        }

        return gamesMap[key];


        //// try finding the game id
        //var val = db.HashGet(STEAM_GAME_IDS_KEY, query);
        //if (val == default)
        //    return -1; // not found

        //var appid = (int)val;
        //return appid;

        // now that we have appid, get the game info with that appid
        //var gameData = await _cache.GetOrAddCachedDataAsync($"steam_game:{appid}", SteamGameDataFactory, appid, TimeSpan.FromHours(12))
        //;

        //return gameData;
    }

    public async Task<GoogleSearchResultData> GoogleSearchAsync(string query)
    {
        query = WebUtility.UrlEncode(query)?.Replace(' ', '+');

        var fullQueryLink = $"https://www.google.ca/search?q={query}&safe=on&lr=lang_eng&hl=en&ie=utf-8&oe=utf-8";

        using var msg = new HttpRequestMessage(HttpMethod.Get, fullQueryLink);
        msg.Headers.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.77 Safari/537.36");
        msg.Headers.Add("Cookie", "CONSENT=YES+shp.gws-20210601-0-RC2.en+FX+423;");

        using var http = _httpFactory.CreateClient();
        http.DefaultRequestHeaders.Clear();

        using var response = await http.SendAsync(msg);
        await using var content = await response.Content.ReadAsStreamAsync();

        using var document = await _googleParser.ParseDocumentAsync(content);
        var elems = document.QuerySelectorAll("div.g > div > div");

        var resultsElem = document.QuerySelectorAll("#resultStats").FirstOrDefault();
        var totalResults = resultsElem?.TextContent;
        //var time = resultsElem.Children.FirstOrDefault()?.TextContent
        //^ this doesn't work for some reason, <nobr> is completely missing in parsed collection
        if (!elems.Any())
            return default;

        var results = elems.Select(elem =>
                           {
                               var children = elem.Children.ToList();
                               if (children.Count < 2)
                                   return null;

                               var href = (children[0].QuerySelector("a") as IHtmlAnchorElement)?.Href;
                               var name = children[0].QuerySelector("h3")?.TextContent;

                               if (href is null || name is null)
                                   return null;

                               var txt = children[1].TextContent;

                               if (string.IsNullOrWhiteSpace(txt))
                                   return null;

                               return new GoogleSearchResult(name, href, txt);
                           })
                           .Where(x => x is not null)
                           .ToList();

        return new(results.AsReadOnly(), fullQueryLink, totalResults);
    }

    public async Task<GoogleSearchResultData> DuckDuckGoSearchAsync(string query)
    {
        query = WebUtility.UrlEncode(query)?.Replace(' ', '+');

        var fullQueryLink = "https://html.duckduckgo.com/html";

        using var http = _httpFactory.CreateClient();
        http.DefaultRequestHeaders.Clear();
        http.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.77 Safari/537.36");

        using var formData = new MultipartFormDataContent();
        formData.Add(new StringContent(query), "q");
        using var response = await http.PostAsync(fullQueryLink, formData);
        var content = await response.Content.ReadAsStringAsync();

        using var document = await _googleParser.ParseDocumentAsync(content);
        var searchResults = document.QuerySelector(".results");
        var elems = searchResults.QuerySelectorAll(".result");

        if (!elems.Any())
            return default;

        var results = elems.Select(elem =>
                           {
                               if (elem.QuerySelector(".result__a") is not IHtmlAnchorElement anchor)
                                   return null;

                               var href = anchor.Href;
                               var name = anchor.TextContent;

                               if (string.IsNullOrWhiteSpace(href) || string.IsNullOrWhiteSpace(name))
                                   return null;

                               var txt = elem.QuerySelector(".result__snippet")?.TextContent;

                               if (string.IsNullOrWhiteSpace(txt))
                                   return null;

                               return new GoogleSearchResult(name, href, txt);
                           })
                           .Where(x => x is not null)
                           .ToList();

        return new(results.AsReadOnly(), fullQueryLink, "0");
    }

    //private async Task<SteamGameData> SteamGameDataFactory(int appid)
    //{
    //    using (var http = _httpFactory.CreateClient())
    //    {
    //        //  https://store.steampowered.com/api/appdetails?appids=
    //        var responseStr = await http.GetStringAsync($"https://store.steampowered.com/api/appdetails?appids={appid}");
    //        var data = JsonConvert.DeserializeObject<Dictionary<int, SteamGameData.Container>>(responseStr);
    //        if (!data.ContainsKey(appid) || !data[appid].Success)
    //            return null; // for some reason we can't get the game with valid appid. SHould never happen

    //        return data[appid].Data;
    //    }
    //}

    public class GoogleSearchResultData
    {
        public IReadOnlyList<GoogleSearchResult> Results { get; }
        public string FullQueryLink { get; }
        public string TotalResults { get; }

        public GoogleSearchResultData(
            IReadOnlyList<GoogleSearchResult> results,
            string fullQueryLink,
            string totalResults)
        {
            Results = results;
            FullQueryLink = fullQueryLink;
            TotalResults = totalResults;
        }
    }
}