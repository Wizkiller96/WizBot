﻿using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using WizBot.Common;
using WizBot.Core.Modules.Searches.Common;
using WizBot.Core.Services;
using WizBot.Core.Services.Database.Models;
using WizBot.Core.Services.Impl;
using WizBot.Extensions;
using WizBot.Modules.Searches.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Serilog;
using HorizontalAlignment = SixLabors.Fonts.HorizontalAlignment;
using Image = SixLabors.ImageSharp.Image;

namespace WizBot.Modules.Searches.Services
{
    public class SearchesService : INService, IUnloadableService
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly DiscordSocketClient _client;
        private readonly IGoogleApiService _google;
        private readonly DbService _db;
        private readonly IImageCache _imgs;
        private readonly IDataCache _cache;
        private readonly FontProvider _fonts;
        private readonly IBotCredentials _creds;
        private readonly WizBotRandom _rng;

        public ConcurrentDictionary<ulong, bool> TranslatedChannels { get; } = new ConcurrentDictionary<ulong, bool>();
        // (userId, channelId)
        public ConcurrentDictionary<(ulong UserId, ulong ChannelId), string> UserLanguages { get; } = new ConcurrentDictionary<(ulong, ulong), string>();

        public List<WoWJoke> WowJokes { get; } = new List<WoWJoke>();
        public List<MagicItem> MagicItems { get; } = new List<MagicItem>();

        private readonly ConcurrentDictionary<ulong, SearchImageCacher> _imageCacher = new ConcurrentDictionary<ulong, SearchImageCacher>();

        public ConcurrentDictionary<ulong, Timer> AutoHentaiTimers { get; } = new ConcurrentDictionary<ulong, Timer>();
        public ConcurrentDictionary<ulong, Timer> AutoBoobTimers { get; } = new ConcurrentDictionary<ulong, Timer>();
        public ConcurrentDictionary<ulong, Timer> AutoButtTimers { get; } = new ConcurrentDictionary<ulong, Timer>();

        private readonly ConcurrentDictionary<ulong, HashSet<string>> _blacklistedTags = new ConcurrentDictionary<ulong, HashSet<string>>();
        private readonly List<string> _yomamaJokes;

        public SearchesService(DiscordSocketClient client, IGoogleApiService google,
            DbService db, WizBot bot, IDataCache cache, IHttpClientFactory factory,
            FontProvider fonts, IBotCredentials creds)
        {
            _httpFactory = factory;
            _client = client;
            _google = google;
            _db = db;
            _imgs = cache.LocalImages;
            _cache = cache;
            _fonts = fonts;
            _creds = creds;
            _rng = new WizBotRandom();

            _blacklistedTags = new ConcurrentDictionary<ulong, HashSet<string>>(
                bot.AllGuildConfigs.ToDictionary(
                    x => x.GuildId,
                    x => new HashSet<string>(x.NsfwBlacklistedTags.Select(y => y.Tag))));

            //translate commands
            _client.MessageReceived += (msg) =>
            {
                var _ = Task.Run(async () =>
                {
                    try
                    {
                        if (!(msg is SocketUserMessage umsg))
                            return;

                        if (!TranslatedChannels.TryGetValue(umsg.Channel.Id, out var autoDelete))
                            return;

                        var key = (umsg.Author.Id, umsg.Channel.Id);

                        if (!UserLanguages.TryGetValue(key, out string langs))
                            return;

                        var text = await Translate(langs, umsg.Resolve(TagHandling.Ignore))
                                            .ConfigureAwait(false);
                        if (autoDelete)
                            try { await umsg.DeleteAsync().ConfigureAwait(false); } catch { }
                        await umsg.Channel.SendConfirmAsync($"{umsg.Author.Mention} `:` "
                            + text.Replace("<@ ", "<@", StringComparison.InvariantCulture)
                                  .Replace("<@! ", "<@!", StringComparison.InvariantCulture)).ConfigureAwait(false);
                    }
                    catch { }
                });
                return Task.CompletedTask;
            };

            //joke commands
            if (File.Exists("data/wowjokes.json"))
            {
                WowJokes = JsonConvert.DeserializeObject<List<WoWJoke>>(File.ReadAllText("data/wowjokes.json"));
            }
            else
                Log.Warning("data/wowjokes.json is missing. WOW Jokes are not loaded.");

            if (File.Exists("data/magicitems.json"))
            {
                MagicItems = JsonConvert.DeserializeObject<List<MagicItem>>(File.ReadAllText("data/magicitems.json"));
            }
            else
                Log.Warning("data/magicitems.json is missing. Magic items are not loaded.");

            if (File.Exists("data/yomama.txt"))
            {
                _yomamaJokes = File.ReadAllLines("data/yomama.txt")
                    .Shuffle()
                    .ToList();
            }
            else
            {
                _yomamaJokes = new List<string>();
                Log.Warning("data/yomama.txt is missing. .yomama command won't work");
            }

        }

        public async Task<Stream> GetRipPictureAsync(string text, Uri imgUrl)
        {
            byte[] data = await _cache.GetOrAddCachedDataAsync($"wizbot_rip_{text}_{imgUrl}",
                GetRipPictureFactory,
                (text, imgUrl),
                TimeSpan.FromDays(1)).ConfigureAwait(false);

            return data.ToStream();
        }

        private void DrawAvatar(Image bg, Image avatarImage)
            => bg.Mutate(x => x.Grayscale().DrawImage(avatarImage, new Point(83, 139), new GraphicsOptions()));

        public async Task<byte[]> GetRipPictureFactory((string text, Uri avatarUrl) arg)
        {
            var (text, avatarUrl) = arg;
            using (var bg = Image.Load<Rgba32>(_imgs.Rip.ToArray()))
            {
                var (succ, data) = (false, (byte[])null); //await _cache.TryGetImageDataAsync(avatarUrl);
                if (!succ)
                {
                    using (var http = _httpFactory.CreateClient())
                    {
                        data = await http.GetByteArrayAsync(avatarUrl);
                        using (var avatarImg = Image.Load<Rgba32>(data))

                        {
                            avatarImg.Mutate(x => x
                                .Resize(85, 85)
                                .ApplyRoundedCorners(42));
                            data = avatarImg.ToStream().ToArray();
                            DrawAvatar(bg, avatarImg);
                        }
                        await _cache.SetImageDataAsync(avatarUrl, data);
                    }
                }
                else
                {
                    using (var avatarImg = Image.Load<Rgba32>(data))
                    {
                        DrawAvatar(bg, avatarImg);
                    }
                }

                bg.Mutate(x => x.DrawText(
                    new TextGraphicsOptions()
                    {
                        TextOptions = new TextOptions
                        {
                            HorizontalAlignment = HorizontalAlignment.Center,
                            WrapTextWidth = 190,
                        }.WithFallbackFonts(_fonts.FallBackFonts)
                    },
                    text,
                    _fonts.RipFont,
                    SixLabors.ImageSharp.Color.Black,
                    new PointF(25, 225)));

                //flowa
                using (var flowers = Image.Load(_imgs.RipOverlay.ToArray()))
                {
                    bg.Mutate(x => x.DrawImage(flowers, new Point(0, 0), new GraphicsOptions()));
                }

                return bg.ToStream().ToArray();
            }
        }

        public Task<WeatherData> GetWeatherDataAsync(string query)
        {
            query = query.Trim().ToLowerInvariant();

            return _cache.GetOrAddCachedDataAsync($"wizbot_weather_{query}",
                GetWeatherDataFactory,
                query,
                expiry: TimeSpan.FromHours(3));
        }

        private async Task<WeatherData> GetWeatherDataFactory(string query)
        {
            using (var http = _httpFactory.CreateClient())
            {
                try
                {
                    var data = await http.GetStringAsync($"http://api.openweathermap.org/data/2.5/weather?" +
                        $"q={query}&" +
                        $"appid=42cd627dd60debf25a5739e50a217d74&" +
                        $"units=metric").ConfigureAwait(false);

                    if (data == null)
                        return null;

                    return JsonConvert.DeserializeObject<WeatherData>(data);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex.Message);
                    return null;
                }
            }
        }

        public Task<((string Address, DateTime Time, string TimeZoneName), TimeErrors?)> GetTimeDataAsync(string arg)
        {
            return GetTimeDataFactory(arg);
            //return _cache.GetOrAddCachedDataAsync($"wizbot_time_{arg}",
            //    GetTimeDataFactory,
            //    arg,
            //    TimeSpan.FromMinutes(1));
        }
        private async Task<((string Address, DateTime Time, string TimeZoneName), TimeErrors?)> GetTimeDataFactory(string query)
        {
            query = query.Trim();

            if (string.IsNullOrEmpty(query))
            {
                return (default, TimeErrors.InvalidInput);
            }

            if (string.IsNullOrWhiteSpace(_creds.LocationIqApiKey)
                || string.IsNullOrWhiteSpace(_creds.TimezoneDbApiKey))
            {
                return (default, TimeErrors.ApiKeyMissing);
            }

            try
            {
                using (var _http = _httpFactory.CreateClient())
                {
                    var res = await _cache.GetOrAddCachedDataAsync($"geo_{query}", _ =>
                    {
                        var url = "https://eu1.locationiq.com/v1/search.php?" +
                            (string.IsNullOrWhiteSpace(_creds.LocationIqApiKey) ? "key=" : $"key={_creds.LocationIqApiKey}&") +
                            $"q={Uri.EscapeDataString(query)}&" +
                            $"format=json";

                        var res = _http.GetStringAsync(url);
                        return res;
                    }, "", TimeSpan.FromHours(1));

                    var responses = JsonConvert.DeserializeObject<LocationIqResponse[]>(res);
                    if (responses is null || responses.Length == 0)
                    {
                        Log.Warning("Geocode lookup failed for: {Query}", query);
                        return (default, TimeErrors.NotFound);
                    }

                    var geoData = responses[0];

                    using (var req = new HttpRequestMessage(HttpMethod.Get, "http://api.timezonedb.com/v2.1/get-time-zone?" +
                        $"key={_creds.TimezoneDbApiKey}&format=json&" +
                        "by=position&" +
                        $"lat={geoData.Lat}&lng={geoData.Lon}"))
                    {

                        using (var geoRes = await _http.SendAsync(req))
                        {
                            var resString = await geoRes.Content.ReadAsStringAsync();
                            var timeObj = JsonConvert.DeserializeObject<TimeZoneResult>(resString);

                            var time = new DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc).AddSeconds(timeObj.Timestamp);

                            return ((
                                Address: responses[0].DisplayName,
                                Time: time,
                                TimeZoneName: timeObj.TimezoneName
                                ), default);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Weather error: {Message}", ex.Message);
                return (default, TimeErrors.NotFound);
            }
        }

        public enum ImageTag
        {
            Food,
            Dogs,
            Cats,
            Birds
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

            return $"https://nadeko-pictures.nyc3.digitaloceanspaces.com/{subpath}/" +
                _rng.Next(1, max).ToString("000") + ".png";
        }

        public async Task<string> Translate(string langs, string text = null)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("Text is empty or null", nameof(text));
            var langarr = langs.ToLowerInvariant().Split('>');
            if (langarr.Length != 2)
                throw new ArgumentException("Langs does not have 2 parts separated by a >", nameof(langs));
            var from = langarr[0];
            var to = langarr[1];
            text = text?.Trim();
            return (await _google.Translate(text, from, to).ConfigureAwait(false)).SanitizeMentions(true);
        }

        public Task<ImageCacherObject> DapiSearch(string tag, DapiSearchType type, ulong? guild, bool isExplicit = false)
        {
            tag = tag ?? "";
            if (string.IsNullOrWhiteSpace(tag)
                && (tag.Contains("loli") || tag.Contains("shota")))
            {
                return null;
            }

            var tags = tag
                .Split('+')
                .Select(x => x.ToLowerInvariant().Replace(' ', '_'))
                .ToArray();

            if (guild.HasValue)
            {
                var blacklistedTags = GetBlacklistedTags(guild.Value);

                var cacher = _imageCacher.GetOrAdd(guild.Value, (key) => new SearchImageCacher(_httpFactory));

                return cacher.GetImage(tags, isExplicit, type, blacklistedTags);
            }
            else
            {
                var cacher = _imageCacher.GetOrAdd(guild ?? 0, (key) => new SearchImageCacher(_httpFactory));

                return cacher.GetImage(tags, isExplicit, type);
            }
        }

        public HashSet<string> GetBlacklistedTags(ulong guildId)
        {
            if (_blacklistedTags.TryGetValue(guildId, out var tags))
                return tags;
            return new HashSet<string>();
        }

        public bool ToggleBlacklistedTag(ulong guildId, string tag)
        {
            var tagObj = new NsfwBlacklitedTag
            {
                Tag = tag
            };

            bool added;
            using (var uow = _db.GetDbContext())
            {
                var gc = uow.GuildConfigs.ForId(guildId, set => set.Include(y => y.NsfwBlacklistedTags));
                if (gc.NsfwBlacklistedTags.Add(tagObj))
                    added = true;
                else
                {
                    gc.NsfwBlacklistedTags.Remove(tagObj);
                    var toRemove = gc.NsfwBlacklistedTags.FirstOrDefault(x => x.Equals(tagObj));
                    if (toRemove != null)
                        uow._context.Remove(toRemove);
                    added = false;
                }
                var newTags = new HashSet<string>(gc.NsfwBlacklistedTags.Select(x => x.Tag));
                _blacklistedTags.AddOrUpdate(guildId, newTags, delegate { return newTags; });

                uow.SaveChanges();
            }
            return added;
        }

        public void ClearCache()
        {
            foreach (var c in _imageCacher)
            {
                c.Value?.Clear();
            }
        }

        private readonly object yomamaLock = new object();
        private int yomamaJokeIndex = 0;
        public Task<string> GetYomamaJoke()
        {
            string joke;
            lock (yomamaLock)
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
            //     var response = await http.GetStringAsync(new Uri("http://api.yomomma.info/")).ConfigureAwait(false);
            //     return JObject.Parse(response)["joke"].ToString() + " 😆";
            // }
        }

        public async Task<(string Setup, string Punchline)> GetRandomJoke()
        {
            using (var http = _httpFactory.CreateClient())
            {
                var res = await http.GetStringAsync("https://official-joke-api.appspot.com/random_joke");
                var resObj = JsonConvert.DeserializeAnonymousType(res, new { setup = "", punchline = "" });
                return (resObj.setup, resObj.punchline);
            }
        }

        public async Task<string> GetChuckNorrisJoke()
        {
            using (var http = _httpFactory.CreateClient())
            {
                var response = await http.GetStringAsync(new Uri("http://api.icndb.com/jokes/random/")).ConfigureAwait(false);
                return JObject.Parse(response)["value"]["joke"].ToString() + " 😆";
            }
        }

        public Task Unload()
        {
            AutoBoobTimers.ForEach(x => x.Value.Change(Timeout.Infinite, Timeout.Infinite));
            AutoBoobTimers.Clear();
            AutoButtTimers.ForEach(x => x.Value.Change(Timeout.Infinite, Timeout.Infinite));
            AutoButtTimers.Clear();
            AutoHentaiTimers.ForEach(x => x.Value.Change(Timeout.Infinite, Timeout.Infinite));
            AutoHentaiTimers.Clear();

            _imageCacher.Clear();
            return Task.CompletedTask;
        }

        public async Task<MtgData> GetMtgCardAsync(string search)
        {
            search = search.Trim().ToLowerInvariant();
            var data = await _cache.GetOrAddCachedDataAsync($"wizbot_mtg_{search}",
                GetMtgCardFactory,
                search,
                TimeSpan.FromDays(1)).ConfigureAwait(false);

            if (data == null || data.Length == 0)
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
                    storeUrl = await _google.ShortenUrl($"https://shop.tcgplayer.com/productcatalog/product/show?" +
                        $"newSearch=false&" +
                        $"ProductType=All&" +
                        $"IsProductNameExact=false&" +
                        $"ProductName={Uri.EscapeUriString(card.Name)}").ConfigureAwait(false);
                }
                catch { storeUrl = "<url can't be found>"; }

                return new MtgData
                {
                    Description = card.Text,
                    Name = card.Name,
                    ImageUrl = card.ImageUrl,
                    StoreUrl = storeUrl,
                    Types = string.Join(",\n", card.Types),
                    ManaCost = card.ManaCost,
                };
            }

            using (var http = _httpFactory.CreateClient())
            {
                http.DefaultRequestHeaders.Clear();
                var response = await http.GetStringAsync($"https://api.magicthegathering.io/v1/cards?name={Uri.EscapeUriString(search)}")
                    .ConfigureAwait(false);

                var responseObject = JsonConvert.DeserializeObject<MtgResponse>(response);
                if (responseObject == null)
                    return new MtgData[0];

                var cards = responseObject.Cards.Take(5).ToArray();
                if (cards.Length == 0)
                    return new MtgData[0];

                var tasks = new List<Task<MtgData>>(cards.Length);
                for (int i = 0; i < cards.Length; i++)
                {
                    var card = cards[i];

                    tasks.Add(GetMtgDataAsync(card));
                }

                return await Task.WhenAll(tasks).ConfigureAwait(false);
            }
        }

        public Task<HearthstoneCardData> GetHearthstoneCardDataAsync(string name)
        {
            name = name.ToLowerInvariant();
            return _cache.GetOrAddCachedDataAsync($"wizbot_hearthstone_{name}",
                HearthstoneCardDataFactory,
                name,
                TimeSpan.FromDays(1));
        }

        private async Task<HearthstoneCardData> HearthstoneCardDataFactory(string name)
        {
            using (var http = _httpFactory.CreateClient())
            {
                http.DefaultRequestHeaders.Clear();
                http.DefaultRequestHeaders.Add("x-rapidapi-key", _creds.MashapeKey);
                try
                {
                    var response = await http.GetStringAsync($"https://omgvamp-hearthstone-v1.p.rapidapi.com/" +
                        $"cards/search/{Uri.EscapeUriString(name)}").ConfigureAwait(false);
                    var objs = JsonConvert.DeserializeObject<HearthstoneCardData[]>(response);
                    if (objs == null || objs.Length == 0)
                        return null;
                    var data = objs.FirstOrDefault(x => x.Collectible)
                        ?? objs.FirstOrDefault(x => !string.IsNullOrEmpty(x.PlayerClass))
                        ?? objs.FirstOrDefault();
                    if (data == null)
                        return null;
                    if (!string.IsNullOrWhiteSpace(data.Img))
                    {
                        data.Img = await _google.ShortenUrl(data.Img).ConfigureAwait(false);
                    }
                    if (!string.IsNullOrWhiteSpace(data.Text))
                    {
                        var converter = new Html2Markdown.Converter();
                        data.Text = converter.Convert(data.Text);
                    }
                    return data;
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Message);
                    return null;
                }
            }
        }

        public Task<OmdbMovie> GetMovieDataAsync(string name)
        {
            name = name.Trim().ToLowerInvariant();
            return _cache.GetOrAddCachedDataAsync($"wizbot_movie_{name}",
                GetMovieDataFactory,
                name,
                TimeSpan.FromDays(1));
        }

        private async Task<OmdbMovie> GetMovieDataFactory(string name)
        {
            using (var http = _httpFactory.CreateClient())
            {
                var res = await http.GetStringAsync(string.Format("https://omdbapi.nadeko.bot/?t={0}&y=&plot=full&r=json",
                    name.Trim().Replace(' ', '+'))).ConfigureAwait(false);
                var movie = JsonConvert.DeserializeObject<OmdbMovie>(res);
                if (movie?.Title == null)
                    return null;
                movie.Poster = await _google.ShortenUrl(movie.Poster).ConfigureAwait(false);
                return movie;
            }
        }

        public async Task<int> GetSteamAppIdByName(string query)
        {
            var redis = _cache.Redis;
            var db = redis.GetDatabase();
            const string STEAM_GAME_IDS_KEY = "steam_names_to_appid";
            var exists = await db.KeyExistsAsync(STEAM_GAME_IDS_KEY).ConfigureAwait(false);

            // if we didn't get steam name to id map already, get it
            //if (!exists)
            //{
            //    using (var http = _httpFactory.CreateClient())
            //    {
            //        // https://api.steampowered.com/ISteamApps/GetAppList/v2/
            //        var gamesStr = await http.GetStringAsync("https://api.steampowered.com/ISteamApps/GetAppList/v2/").ConfigureAwait(false);
            //        var apps = JsonConvert.DeserializeAnonymousType(gamesStr, new { applist = new { apps = new List<SteamGameId>() } }).applist.apps;

            //        //await db.HashSetAsync("steam_game_ids", apps.Select(app => new HashEntry(app.Name.Trim().ToLowerInvariant(), app.AppId)).ToArray()).ConfigureAwait(false);
            //        await db.StringSetAsync("steam_game_ids", gamesStr, TimeSpan.FromHours(24));
            //        //await db.KeyExpireAsync("steam_game_ids", TimeSpan.FromHours(24), CommandFlags.FireAndForget).ConfigureAwait(false);
            //    }
            //}

            var gamesMap = await _cache.GetOrAddCachedDataAsync(STEAM_GAME_IDS_KEY, async _ =>
            {
                using (var http = _httpFactory.CreateClient())
                {
                    // https://api.steampowered.com/ISteamApps/GetAppList/v2/
                    var gamesStr = await http.GetStringAsync("https://api.steampowered.com/ISteamApps/GetAppList/v2/").ConfigureAwait(false);
                    var apps = JsonConvert.DeserializeAnonymousType(gamesStr, new { applist = new { apps = new List<SteamGameId>() } }).applist.apps;

                    return apps
                        .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                        .GroupBy(x => x.Name)
                        .ToDictionary(x => x.Key, x => x.First().AppId);
                    //await db.HashSetAsync("steam_game_ids", apps.Select(app => new HashEntry(app.Name.Trim().ToLowerInvariant(), app.AppId)).ToArray()).ConfigureAwait(false);
                    //await db.StringSetAsync("steam_game_ids", gamesStr, TimeSpan.FromHours(24));
                    //await db.KeyExpireAsync("steam_game_ids", TimeSpan.FromHours(24), CommandFlags.FireAndForget).ConfigureAwait(false);
                }
            }, default(string), TimeSpan.FromHours(24));

            if (gamesMap == null)
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
            //    .ConfigureAwait(false);

            //return gameData;
        }

        //private async Task<SteamGameData> SteamGameDataFactory(int appid)
        //{
        //    using (var http = _httpFactory.CreateClient())
        //    {
        //        //  https://store.steampowered.com/api/appdetails?appids=
        //        var responseStr = await http.GetStringAsync($"https://store.steampowered.com/api/appdetails?appids={appid}").ConfigureAwait(false);
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

            public GoogleSearchResultData(IReadOnlyList<GoogleSearchResult> results, string fullQueryLink,
                string totalResults)
            {
                Results = results;
                FullQueryLink = fullQueryLink;
                TotalResults = totalResults;
            }
        }

        private static readonly HtmlParser _googleParser = new HtmlParser(new HtmlParserOptions()
        {
            IsScripting = false,
            IsEmbedded = false,
            IsSupportingProcessingInstructions = false,
            IsKeepingSourceReferences = false,
            IsNotSupportingFrames = true,
        });
        
        public async Task<GoogleSearchResultData> GoogleSearchAsync(string query)
        {
            query = WebUtility.UrlEncode(query)?.Replace(' ', '+');

            var fullQueryLink = $"https://www.google.ca/search?q={ query }&safe=on&lr=lang_eng&hl=en&ie=utf-8&oe=utf-8";

            using var msg = new HttpRequestMessage(HttpMethod.Get, fullQueryLink);
            msg.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.77 Safari/537.36");
            msg.Headers.Add("Cookie", "CONSENT=YES+shp.gws-20210601-0-RC2.en+FX+423;");
                
            using var http = _httpFactory.CreateClient();
            http.DefaultRequestHeaders.Clear();

            using var response = await http.SendAsync(msg);
            var content = await response.Content.ReadAsStreamAsync();

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
                    
                    if (href == null || name == null)
                        return null;

                    var txt = children[1].TextContent;

                    if (string.IsNullOrWhiteSpace(txt))
                        return null;

                    return new GoogleSearchResult(name, href, txt);
                })
                .Where(x => x != null)
                .ToList();

            return new GoogleSearchResultData(
                results.AsReadOnly(),
                fullQueryLink,
                totalResults);
        }
        
        public async Task<GoogleSearchResultData> DuckDuckGoSearchAsync(string query)
        {
            query = WebUtility.UrlEncode(query)?.Replace(' ', '+');

            var fullQueryLink = $"https://html.duckduckgo.com/html";

            using var http = _httpFactory.CreateClient();
            http.DefaultRequestHeaders.Clear();
            http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.77 Safari/537.36");

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
                    var anchor = elem.QuerySelector(".result__a") as IHtmlAnchorElement;

                    if (anchor is null)
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
                .Where(x => x != null)
                .ToList();

            return new GoogleSearchResultData(
                results.AsReadOnly(),
                fullQueryLink,
                "0");
        }
    }

    public class SteamGameId
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("appid")]
        public int AppId { get; set; }
    }

    public class SteamGameData
    {
        public string ShortDescription { get; set; }

        public class Container
        {
            [JsonProperty("success")]
            public bool Success { get; set; }

            [JsonProperty("data")]
            public SteamGameData Data { get; set; }
        }
    }


    public enum TimeErrors
    {
        InvalidInput,
        ApiKeyMissing,
        NotFound,
        Unknown
    }
}
