using NadekoBot.Modules.Searches.GoogleScrape;
using NadekoBot.Modules.Searches.Youtube;

namespace NadekoBot.Modules.Searches;

public sealed class DefaultSearchServiceFactory : ISearchServiceFactory, INService
{
    private readonly SearchesConfigService _scs;
    private readonly SearxSearchService _sss;
    private readonly GoogleSearchService _gss;

    private readonly YtdlpYoutubeSearchService _ytdlp;
    private readonly YtdlYoutubeSearchService _ytdl;
    private readonly YoutubeDataApiSearchService _ytdata;
    private readonly InvidiousYtSearchService _iYtSs;
    private readonly GoogleScrapeService _gscs;

    public DefaultSearchServiceFactory(
        SearchesConfigService scs,
        GoogleSearchService gss,
        GoogleScrapeService gscs,
        SearxSearchService sss,
        YtdlpYoutubeSearchService ytdlp,
        YtdlYoutubeSearchService ytdl,
        YoutubeDataApiSearchService ytdata,
        InvidiousYtSearchService iYtSs)
    {
        _scs = scs;
        _sss = sss;
        _gss = gss;
        _gscs = gscs;
        _iYtSs = iYtSs;

        _ytdlp = ytdlp;
        _ytdl = ytdl;
        _ytdata = ytdata;
    }

    public ISearchService GetSearchService(string? hint = null)
        => _scs.Data.WebSearchEngine switch
        {
            WebSearchEngine.Google => _gss,
            WebSearchEngine.Google_Scrape => _gscs,
            WebSearchEngine.Searx => _sss,
            _ => _gss
        };

    public ISearchService GetImageSearchService(string? hint = null)
        => _scs.Data.ImgSearchEngine switch
        {
            ImgSearchEngine.Google => _gss,
            ImgSearchEngine.Searx => _sss,
            _ => _gss
        };

    public IYoutubeSearchService GetYoutubeSearchService(string? hint = null)
        => _scs.Data.YtProvider switch
        {
            YoutubeSearcher.YtDataApiv3 => _ytdata,
            YoutubeSearcher.Ytdlp => _ytdlp,
            YoutubeSearcher.Ytdl => _ytdl,
            YoutubeSearcher.Invidious => _iYtSs,
            _ => _ytdl
        };
}