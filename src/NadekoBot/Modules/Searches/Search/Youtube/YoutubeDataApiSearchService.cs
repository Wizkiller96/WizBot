namespace NadekoBot.Modules.Searches.Youtube;

public sealed class YoutubeDataApiSearchService : IYoutubeSearchService, INService
{
    private readonly IGoogleApiService _gapi;

    public YoutubeDataApiSearchService(IGoogleApiService gapi)
    {
        _gapi = gapi;
    }

    public async Task<VideoInfo?> SearchAsync(string query)
    {
        ArgumentNullException.ThrowIfNull(query);

        var results = await _gapi.GetVideoLinksByKeywordAsync(query);
        var first = results.FirstOrDefault();
        if (first is null)
            return null;

        return new()
        {
            Url = first
        };
    }
}