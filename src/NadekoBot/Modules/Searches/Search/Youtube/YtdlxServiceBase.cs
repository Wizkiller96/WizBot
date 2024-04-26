namespace NadekoBot.Modules.Searches.Youtube;

public abstract class YoutubedlxServiceBase : IYoutubeSearchService
{
    private YtdlOperation CreateYtdlOp(bool isYtDlp)
        => new YtdlOperation("-4 "
                             + "--geo-bypass "
                             + "--encoding UTF8 "
                             + "--get-id "
                             + "--no-check-certificate "
                             + "--default-search "
                             + "\"ytsearch:\" -- \"{0}\"",
            isYtDlp: isYtDlp);

    protected async Task<VideoInfo?> InternalGetInfoAsync(string query, bool isYtDlp)
    {
        var op = CreateYtdlOp(isYtDlp);
        var data = await op.GetDataAsync(query);
        var items = data?.Split('\n');
        if (items is null or { Length: 0 })
            return null;

        var id = items.FirstOrDefault(x => x.Length is > 5 and < 15);
        if (id is null)
            return null;

        return new VideoInfo()
        {
            Url = $"https://youtube.com/watch?v={id}"
        };
    }

    public abstract Task<VideoInfo?> SearchAsync(string query);
}