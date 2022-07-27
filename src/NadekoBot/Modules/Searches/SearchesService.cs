#nullable disable
using Html2Markdown;
using Nadeko.Common;
using NadekoBot.Modules.Searches.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
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

    public List<WoWJoke> WowJokes { get; } = new();
    public List<MagicItem> MagicItems { get; } = new();
    private readonly IHttpClientFactory _httpFactory;
    private readonly IGoogleApiService _google;
    private readonly IImageCache _imgs;
    private readonly IBotCache _c;
    private readonly FontProvider _fonts;
    private readonly IBotCredsProvider _creds;
    private readonly NadekoRandom _rng;
    private readonly List<string> _yomamaJokes;

    private readonly object _yomamaLock = new();
    private int yomamaJokeIndex;

    public SearchesService(
        IGoogleApiService google,
        IImageCache images,
        IBotCache c,
        IHttpClientFactory factory,
        FontProvider fonts,
        IBotCredsProvider creds)
    {
        _httpFactory = factory;
        _google = google;
        _imgs = images;
        _c = c;
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
        => (await GetRipPictureFactory(text, imgUrl)).ToStream();

    private void DrawAvatar(Image bg, Image avatarImage)
        => bg.Mutate(x => x.Grayscale().DrawImage(avatarImage, new(83, 139), new GraphicsOptions()));

    public async Task<byte[]> GetRipPictureFactory(string text, Uri avatarUrl)
    {
        using var bg = Image.Load<Rgba32>(await _imgs.GetRipBgAsync());
        var result = await _c.GetImageDataAsync(avatarUrl);
        if (!result.TryPickT0(out var data, out _))
        {
            using var http = _httpFactory.CreateClient();
            data = await http.GetByteArrayAsync(avatarUrl);
            using (var avatarImg = Image.Load<Rgba32>(data))
            {
                avatarImg.Mutate(x => x.Resize(85, 85).ApplyRoundedCorners(42));
                await using var avStream = await avatarImg.ToStreamAsync();
                data = avStream.ToArray();
                DrawAvatar(bg, avatarImg);
            }

            await _c.SetImageDataAsync(avatarUrl, data);
        }
        else
        {
            using var avatarImg = Image.Load<Rgba32>(data);
            DrawAvatar(bg, avatarImg);
        }

        bg.Mutate(x => x.DrawText(
            new TextOptions(_fonts.RipFont)
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                FallbackFontFamilies = _fonts.FallBackFonts,
                Origin = new(bg.Width / 2, 225),
            },
            text,
            Color.Black));

        //flowa
        using (var flowers = Image.Load(await _imgs.GetRipOverlayAsync()))
        {
            bg.Mutate(x => x.DrawImage(flowers, new(0, 0), new GraphicsOptions()));
        }

        await using var stream = bg.ToStream();
        return stream.ToArray();
    }

    public async Task<WeatherData> GetWeatherDataAsync(string query)
    {
        query = query.Trim().ToLowerInvariant();

        return await _c.GetOrAddAsync(new($"nadeko_weather_{query}"),
            async () => await GetWeatherDataFactory(query),
            TimeSpan.FromHours(3));
    }

    private async Task<WeatherData> GetWeatherDataFactory(string query)
    {
        using var http = _httpFactory.CreateClient();
        try
        {
            var data = await http.GetStringAsync("https://api.openweathermap.org/data/2.5/weather?"
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


        var locIqKey = _creds.GetCreds().LocationIqApiKey;
        var tzDbKey = _creds.GetCreds().TimezoneDbApiKey;
        if (string.IsNullOrWhiteSpace(locIqKey) || string.IsNullOrWhiteSpace(tzDbKey))
            return (default, TimeErrors.ApiKeyMissing);

        try
        {
            using var http = _httpFactory.CreateClient();
            var res = await _c.GetOrAddAsync(new($"searches:geo:{query}"),
                async () =>
                {
                    var url = "https://eu1.locationiq.com/v1/search.php?"
                              + (string.IsNullOrWhiteSpace(locIqKey)
                                  ? "key="
                                  : $"key={locIqKey}&")
                              + $"q={Uri.EscapeDataString(query)}&"
                              + "format=json";

                    var res = await http.GetStringAsync(url);
                    return res;
                },
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
                + $"key={tzDbKey}"
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
        var data = await _c.GetOrAddAsync(new($"mtg:{search}"),
            async () => await GetMtgCardFactory(search),
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

    public async Task<HearthstoneCardData> GetHearthstoneCardDataAsync(string name)
    {
        name = name.ToLowerInvariant();
        return await _c.GetOrAddAsync($"hearthstone:{name}",
            () => HearthstoneCardDataFactory(name),
            TimeSpan.FromDays(1));
    }

    private async Task<HearthstoneCardData> HearthstoneCardDataFactory(string name)
    {
        using var http = _httpFactory.CreateClient();
        http.DefaultRequestHeaders.Clear();
        http.DefaultRequestHeaders.Add("x-rapidapi-key", _creds.GetCreds().RapidApiKey);
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

    public async Task<OmdbMovie> GetMovieDataAsync(string name)
    {
        name = name.Trim().ToLowerInvariant();
        return await _c.GetOrAddAsync(new($"movie:{name}"),
            () => GetMovieDataFactory(name),
            TimeSpan.FromDays(1));
    }

    private async Task<OmdbMovie> GetMovieDataFactory(string name)
    {
        using var http = _httpFactory.CreateClient();
        var res = await http.GetStringAsync(string.Format("https://omdbapi.nadeko.bot/"
                                                          + "?t={0}"
                                                          + "&y="
                                                          + "&plot=full"
                                                          + "&r=json",
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

        var gamesMap = await _c.GetOrAddAsync(new(steamGameIdsKey),
            async () =>
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
                               })!
                           .applist.apps;

                return apps.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                           .GroupBy(x => x.Name)
                           .ToDictionary(x => x.Key, x => x.First().AppId);
            },
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
    }
}