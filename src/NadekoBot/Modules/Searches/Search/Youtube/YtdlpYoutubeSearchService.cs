namespace NadekoBot.Modules.Searches.Youtube;

public sealed class YtdlpYoutubeSearchService : YoutubedlxServiceBase, INService
{
    public override async Task<VideoInfo?> SearchAsync(string query)
        => await InternalGetInfoAsync(query, true);
}