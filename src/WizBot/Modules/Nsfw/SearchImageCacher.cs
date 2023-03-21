﻿#nullable disable
using Microsoft.Extensions.Caching.Memory;
using Wiz.Common;

namespace WizBot.Modules.Nsfw.Common;

public class SearchImageCacher : INService
{
    private static readonly ISet<string> _defaultTagBlacklist = new HashSet<string>
    {
        "loli",
        "lolicon",
        "shota",
        "shotacon",
        "cub"
    };

    private readonly IHttpClientFactory _httpFactory;
    private readonly Random _rng;

    private readonly Dictionary<Booru, object> _typeLocks = new();
    private readonly Dictionary<Booru, HashSet<string>> _usedTags = new();
    private readonly IMemoryCache _cache;

    private readonly ConcurrentDictionary<(Booru, string), int> _maxPages = new();

    public SearchImageCacher(IHttpClientFactory httpFactory, IMemoryCache cache)
    {
        _httpFactory = httpFactory;
        _rng = new WizBotRandom();
        _cache = cache;

        // initialize new cache with empty values
        foreach (var type in Enum.GetValues<Booru>())
        {
            _typeLocks[type] = new();
            _usedTags[type] = new();
        }
    }

    private string Key(Booru boory, string tag)
        => $"booru:{boory}__tag:{tag}";

    /// <summary>
    ///     Download images of the specified type, and cache them.
    /// </summary>
    /// <param name="tags">Required tags</param>
    /// <param name="forceExplicit">Whether images will be forced to be explicit</param>
    /// <param name="type">Provider type</param>
    /// <param name="cancel">Cancellation token</param>
    /// <returns>Whether any image is found.</returns>
    private async Task<bool> UpdateImagesInternalAsync(
        string[] tags,
        bool forceExplicit,
        Booru type,
        CancellationToken cancel)
    {
        var images = await DownloadImagesAsync(tags, forceExplicit, type, cancel);
        if (images is null || images.Count == 0)
            // Log.Warning("Got no images for {0}, tags: {1}", type, string.Join(", ", tags));
            return false;

        Log.Information("Updating {Type}...", type);
        lock (_typeLocks[type])
        {
            var typeUsedTags = _usedTags[type];
            foreach (var tag in tags)
                typeUsedTags.Add(tag);

            // if user uses no tags for the hentai command and there are no used
            // tags atm, just select 50 random tags from downloaded images to seed
            if (typeUsedTags.Count == 0)
                images.SelectMany(x => x.Tags).Distinct().Shuffle().Take(50).ToList().ForEach(x => typeUsedTags.Add(x));

            foreach (var img in images)
            {
                // if any of the tags is a tag banned by discord
                // do not put that image in the cache
                if (_defaultTagBlacklist.Overlaps(img.Tags))
                    continue;

                // if image doesn't have a proper absolute uri, skip it
                if (!Uri.IsWellFormedUriString(img.FileUrl, UriKind.Absolute))
                    continue;

                // i'm appending current tags because of tag aliasing
                // this way, if user uses tag alias, for example 'kissing' -
                // both 'kiss' (real tag returned by the image) and 'kissing' will be populated with
                // retreived images
                foreach (var tag in img.Tags.Concat(tags).Distinct())
                {
                    if (typeUsedTags.Contains(tag))
                    {
                        var set = _cache.GetOrCreate<HashSet<ImageData>>(Key(type, tag),
                            e =>
                            {
                                e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
                                return new();
                            });

                        if (set.Count < 100)
                            set.Add(img);
                    }
                }
            }
        }

        return true;
    }

    private ImageData QueryLocal(
        string[] tags,
        Booru type,
        HashSet<string> blacklistedTags)
    {
        var setList = new List<HashSet<ImageData>>();

        // ofc make sure no changes are happening while we're getting a random one
        lock (_typeLocks[type])
        {
            // if no tags are provided, get a random tag
            if (tags.Length == 0)
            {
                // get all tags in the cache
                if (_usedTags.TryGetValue(type, out var allTags) && allTags.Count > 0)
                    tags = new[] { allTags.ToList()[_rng.Next(0, allTags.Count)] };
                else
                    return null;
            }

            foreach (var tag in tags)
                // if any tag is missing from cache, that means there is no result
            {
                if (_cache.TryGetValue<HashSet<ImageData>>(Key(type, tag), out var set))
                    setList.Add(set);
                else
                    return null;
            }

            if (setList.Count == 0)
                return null;


            List<ImageData> resultList;
            // if multiple tags, we need to interesect sets
            if (setList.Count > 1)
            {
                // now that we have sets, interesect them to find eligible items
                // make a copy of the 1st set
                var resultSet = new HashSet<ImageData>(setList[0]);

                // go through all other sets, and
                for (var i = 1; i < setList.Count; ++i)
                    // if any of the elements in result set are not present in the current set
                    // remove it from the result set
                    resultSet.IntersectWith(setList[i]);

                resultList = resultSet.ToList();
            }
            else
            {
                // if only one tag, use that set
                resultList = setList[0].ToList();
            }

            // return a random one which doesn't have blacklisted tags in it
            resultList = resultList.Where(x => !blacklistedTags.Overlaps(x.Tags)).ToList();

            // if no items in the set -> not found
            if (resultList.Count == 0)
                return null;

            var toReturn = resultList[_rng.Next(0, resultList.Count)];

            // remove from cache
            foreach (var tag in tags)
            {
                if (_cache.TryGetValue<HashSet<ImageData>>(Key(type, tag), out var items))
                    items.Remove(toReturn);
            }

            return toReturn;
        }
    }

    public async Task<ImageData> GetImageNew(
        string[] tags,
        bool forceExplicit,
        Booru type,
        HashSet<string> blacklistedTags,
        CancellationToken cancel)
    {
        // make sure tags are proper
        tags = tags.Where(x => x is not null).Select(tag => tag.ToLowerInvariant().Trim()).Distinct().ToArray();

        if (tags.Length > 2 && type == Booru.Danbooru)
            tags = tags[..2];

        // use both tags banned by discord and tags banned on the server 
        if (blacklistedTags.Overlaps(tags) || _defaultTagBlacklist.Overlaps(tags))
            return default;

        // query for an image
        var image = QueryLocal(tags, type, blacklistedTags);
        if (image is not null)
            return image;

        var success = false;
        try
        {
            // if image is not found, update the cache and query again
            success = await UpdateImagesInternalAsync(tags, forceExplicit, type, cancel);
        }
        catch (HttpRequestException)
        {
        }

        if (!success)
            return default;

        image = QueryLocal(tags, type, blacklistedTags);

        return image;
    }

    public async Task<List<ImageData>> DownloadImagesAsync(
        string[] tags,
        bool isExplicit,
        Booru type,
        CancellationToken cancel)
    {
        var tagStr = string.Join(' ', tags.OrderByDescending(x => x));

        var attempt = 0;
        while (attempt++ <= 10)
        {
            int page;
            if (_maxPages.TryGetValue((type, tagStr), out var maxPage))
            {
                if (maxPage == 0)
                {
                    Log.Information("Tag {Tags} yields no result on {Type}, skipping", tagStr, type);
                    return new();
                }

                page = _rng.Next(0, maxPage);
            }
            else
                page = _rng.Next(0, 11);

            var result = await DownloadImagesAsync(tags, isExplicit, type, page, cancel);

            if (result is null or { Count: 0 })
            {
                Log.Information("Tag {Tags}, page {Page} has no result on {Type}",
                    string.Join(", ", tags),
                    page,
                    type.ToString());
                continue;
            }

            return result;
        }

        return new();
    }

    private IImageDownloader GetImageDownloader(Booru booru)
        => booru switch
        {
            // Booru.Danbooru => new DanbooruImageDownloader(_httpFactory),
            Booru.Yandere => new YandereImageDownloader(_httpFactory),
            Booru.Konachan => new KonachanImageDownloader(_httpFactory),
            Booru.Safebooru => new SafebooruImageDownloader(_httpFactory),
            Booru.E621 => new E621ImageDownloader(_httpFactory),
            Booru.Derpibooru => new DerpibooruImageDownloader(_httpFactory),
            Booru.Gelbooru => new GelbooruImageDownloader(_httpFactory),
            Booru.Rule34 => new Rule34ImageDownloader(_httpFactory),
            Booru.Sankaku => new SankakuImageDownloader(_httpFactory),
            _ => throw new NotImplementedException($"{booru} downloader not implemented.")
        };

    private async Task<List<ImageData>> DownloadImagesAsync(
        string[] tags,
        bool isExplicit,
        Booru type,
        int page,
        CancellationToken cancel)
    {
        try
        {
            Log.Information("Downloading from {Type} (page {Page})...", type, page);

            var downloader = GetImageDownloader(type);

            var images = await downloader.DownloadImageDataAsync(tags, page, isExplicit, cancel);
            if (images.Count == 0)
            {
                var tagStr = string.Join(' ', tags.OrderByDescending(x => x));
                _maxPages[(type, tagStr)] = page;
            }

            return images;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Log.Error(ex,
                "Error downloading an image:\nTags: {Tags}\nType: {Type}\nPage: {Page}\nMessage: {Message}",
                string.Join(", ", tags),
                type,
                page,
                ex.Message);
            return new();
        }
    }
}