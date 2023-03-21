#nullable disable warnings
using LinqToDB;
using Wiz.Common;
using WizBot.Modules.Nsfw.Common;
using WizBot.Modules.Searches.Common;
using Newtonsoft.Json.Linq;

namespace WizBot.Modules.Nsfw;

public class SearchImagesService : ISearchImagesService, INService
{
    private ConcurrentDictionary<ulong, HashSet<string>> BlacklistedTags { get; }

    // public ConcurrentDictionary<ulong, Timer> AutoHentaiTimers { get; } = new();
    public ConcurrentDictionary<ulong, Timer> AutoBoobTimers { get; } = new();
    public ConcurrentDictionary<ulong, Timer> AutoButtTimers { get; } = new();
    
    private readonly Random _rng;
    private readonly SearchImageCacher _cache;
    private readonly IHttpClientFactory _httpFactory;
    private readonly DbService _db;

    private readonly object _taglock = new();

    public SearchImagesService(
        DbService db,
        SearchImageCacher cacher,
        IHttpClientFactory httpFactory
        )
    {
        _db = db;
        _rng = new WizBotRandom();
        _cache = cacher;
        _httpFactory = httpFactory;

        using var uow = db.GetDbContext();
        BlacklistedTags = new(uow.NsfwBlacklistedTags.AsEnumerable()
                                 .GroupBy(x => x.GuildId)
                                 .ToDictionary(x => x.Key, x => new HashSet<string>(x.Select(y => y.Tag))));
    }

    private Task<UrlReply> GetNsfwImageAsync(
        ulong? guildId,
        bool forceExplicit,
        string[] tags,
        Booru dapi,
        CancellationToken cancel = default)
        => GetNsfwImageAsync(guildId ?? 0, tags ?? Array.Empty<string>(), forceExplicit, dapi, cancel);

    private bool IsValidTag(string tag)
        => tag.All(x => x != '+' && x != '?' && x != '/'); // tags mustn't contain + or ? or /

    private async Task<UrlReply> GetNsfwImageAsync(
        ulong guildId,
        string[] tags,
        bool forceExplicit,
        Booru dapi,
        CancellationToken cancel)
    {
        if (!tags.All(x => IsValidTag(x)))
        {
            return new()
            {
                Error = "One or more tags are invalid.",
                Url = ""
            };
        }

        Log.Information("Getting {V} image for Guild: {GuildId}...", dapi.ToString(), guildId);
        try
        {
            BlacklistedTags.TryGetValue(guildId, out var blTags);

            if (dapi == Booru.E621)
            {
                for (var i = 0; i < tags.Length; ++i)
                {
                    if (tags[i] == "yuri")
                        tags[i] = "female/female";
                }
            }

            if (dapi == Booru.Derpibooru)
            {
                for (var i = 0; i < tags.Length; ++i)
                {
                    if (tags[i] == "yuri")
                        tags[i] = "lesbian";
                }
            }

            var result = await _cache.GetImageNew(tags, forceExplicit, dapi, blTags ?? new HashSet<string>(), cancel);

            if (result is null)
            {
                return new()
                {
                    Error = "Image not found.",
                    Url = ""
                };
            }

            var reply = new UrlReply
            {
                Error = "",
                Url = result.FileUrl,
                Rating = result.Rating,
                Provider = result.SearchType.ToString()
            };

            reply.Tags.AddRange(result.Tags);

            return reply;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed getting {Dapi} image: {Message}", dapi, ex.Message);
            return new()
            {
                Error = ex.Message,
                Url = ""
            };
        }
    }

    public Task<UrlReply> Gelbooru(ulong? guildId, bool forceExplicit, string[] tags)
        => GetNsfwImageAsync(guildId, forceExplicit, tags, Booru.Gelbooru);

    public Task<UrlReply> Danbooru(ulong? guildId, bool forceExplicit, string[] tags)
        => GetNsfwImageAsync(guildId, forceExplicit, tags, Booru.Danbooru);

    public Task<UrlReply> Konachan(ulong? guildId, bool forceExplicit, string[] tags)
        => GetNsfwImageAsync(guildId, forceExplicit, tags, Booru.Konachan);

    public Task<UrlReply> Yandere(ulong? guildId, bool forceExplicit, string[] tags)
        => GetNsfwImageAsync(guildId, forceExplicit, tags, Booru.Yandere);

    public Task<UrlReply> Rule34(ulong? guildId, bool forceExplicit, string[] tags)
        => GetNsfwImageAsync(guildId, forceExplicit, tags, Booru.Rule34);

    public Task<UrlReply> E621(ulong? guildId, bool forceExplicit, string[] tags)
        => GetNsfwImageAsync(guildId, forceExplicit, tags, Booru.E621);

    public Task<UrlReply> DerpiBooru(ulong? guildId, bool forceExplicit, string[] tags)
        => GetNsfwImageAsync(guildId, forceExplicit, tags, Booru.Derpibooru);

    public Task<UrlReply> SafeBooru(ulong? guildId, bool forceExplicit, string[] tags)
        => GetNsfwImageAsync(guildId, forceExplicit, tags, Booru.Safebooru);

    public Task<UrlReply> Sankaku(ulong? guildId, bool forceExplicit, string[] tags)
        => GetNsfwImageAsync(guildId, forceExplicit, tags, Booru.Sankaku);

    public async Task<UrlReply> Hentai(ulong? guildId, bool forceExplicit, string[] tags)
    {
        var providers = new[] { Booru.Danbooru, Booru.Konachan, Booru.Gelbooru, Booru.Yandere };

        using var cancelSource = new CancellationTokenSource();

        // create a task for each type
        var tasks = providers.Select(type => GetNsfwImageAsync(guildId, forceExplicit, tags, type)).ToList();
        do
        {
            // wait for any of the tasks to complete
            var task = await Task.WhenAny(tasks);

            // get its result
            var result = task.GetAwaiter().GetResult();
            if (result.Error == "")
            {
                // if we have a non-error result, cancel other searches and return the result
                cancelSource.Cancel();
                return result;
            }

            // if the result is an error, remove that task from the waiting list,
            // and wait for another task to complete
            tasks.Remove(task);
        } while (tasks.Count > 0); // keep looping as long as there is any task remaining to be attempted

        // if we ran out of tasks, that means all tasks failed - return an error
        return new()
        {
            Error = "No hentai image found."
        };
    }

    public async Task<UrlReply> Boobs()
    {
        try
        {
            using var http = _httpFactory.CreateClient();
            http.AddFakeHeaders();
            JToken obj;
            obj = JArray.Parse(await http.GetStringAsync($"http://api.oboobs.ru/boobs/{_rng.Next(0, 12000)}"))[0];
            return new()
            {
                Error = "",
                Url = $"http://media.oboobs.ru/{obj["preview"]}"
            };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retreiving boob image: {Message}", ex.Message);
            return new()
            {
                Error = ex.Message,
                Url = ""
            };
        }
    }

    public ValueTask<bool> ToggleBlacklistTag(ulong guildId, string tag)
    {
        lock (_taglock)
        {
            tag = tag.Trim().ToLowerInvariant();
            var blacklistedTags = BlacklistedTags.GetOrAdd(guildId, new HashSet<string>());
            var isAdded = blacklistedTags.Add(tag);

            using var uow = _db.GetDbContext();
            if (!isAdded)
            {
                blacklistedTags.Remove(tag);
                uow.NsfwBlacklistedTags.DeleteAsync(x => x.GuildId == guildId && x.Tag == tag);
                uow.SaveChanges();
            }
            else
            {
                uow.NsfwBlacklistedTags.Add(new()
                {
                    Tag = tag,
                    GuildId = guildId
                });

                uow.SaveChanges();
            }

            return new(isAdded);
        }
    }

    public ValueTask<string[]> GetBlacklistedTags(ulong guildId)
    {
        lock (_taglock)
        {
            if (BlacklistedTags.TryGetValue(guildId, out var tags))
                return new(tags.ToArray());

            return new(Array.Empty<string>());
        }
    }

    public async Task<UrlReply> Butts()
    {
        try
        {
            using var http = _httpFactory.CreateClient();
            http.AddFakeHeaders();
            JToken obj;
            obj = JArray.Parse(await http.GetStringAsync($"http://api.obutts.ru/butts/{_rng.Next(0, 6100)}"))[0];
            return new()
            {
                Error = "",
                Url = $"http://media.obutts.ru/{obj["preview"]}"
            };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retreiving butt image: {Message}", ex.Message);
            return new()
            {
                Error = ex.Message,
                Url = ""
            };
        }
    }

    /*
    #region Nhentai

    public Task<Gallery?> GetNhentaiByIdAsync(uint id)
        => _nh.GetAsync(id);

    public async Task<Gallery?> GetNhentaiBySearchAsync(string search)
    {
        var ids = await _nh.GetIdsBySearchAsync(search);

        if (ids.Count == 0)
            return null;
        
        var id = ids[_rng.Next(0, ids.Count)];
        return await _nh.GetAsync(id);
    }

    #endregion
    */
}