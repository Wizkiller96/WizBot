namespace NadekoBot.Modules.Searches.Youtube;

public sealed class YtdlYoutubeSearchService : YoutubedlxServiceBase, INService
{
    public override async Task<VideoInfo?> SearchAsync(string query)
        => await InternalGetInfoAsync(query, false);
}