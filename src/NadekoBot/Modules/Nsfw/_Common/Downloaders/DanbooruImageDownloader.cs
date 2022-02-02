#nullable disable
using System.Net.Http.Json;

namespace NadekoBot.Modules.Nsfw.Common;

public sealed class DanbooruImageDownloader : DapiImageDownloader
{
    // using them as concurrent hashsets, value doesn't matter
    private static readonly ConcurrentDictionary<string, bool> _existentTags = new();
    private static readonly ConcurrentDictionary<string, bool> _nonexistentTags = new();

    public DanbooruImageDownloader(HttpClient http)
        : base(Booru.Danbooru, http, "http://danbooru.donmai.us")
    {
    }

    public override async Task<bool> IsTagValid(string tag, CancellationToken cancel = default)
    {
        if (_existentTags.ContainsKey(tag))
            return true;

        if (_nonexistentTags.ContainsKey(tag))
            return false;

        var tags = await _http.GetFromJsonAsync<DapiTag[]>(
            _baseUrl + "/tags.json" + $"?search[name_or_alias_matches]={tag}",
            _serializerOptions,
            cancel);
        if (tags is { Length: > 0 })
            return _existentTags[tag] = true;

        return _nonexistentTags[tag] = false;
    }
}