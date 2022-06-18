﻿using WizBot.Modules.Searches.Youtube;

namespace WizBot.Modules.Searches;

public sealed class DefaultSearchServiceFactory : ISearchServiceFactory, INService
{
    private readonly SearchesConfigService _scs;
    private readonly SearxSearchService _sss;
    private readonly GoogleSearchService _gss;

    private readonly YtdlpYoutubeSearchService _ytdlp;
    private readonly YtdlYoutubeSearchService _ytdl;
    private readonly YoutubeDataApiSearchService _ytdata;
    private readonly InvidiousYtSearchService _iYtSs;

    public DefaultSearchServiceFactory(
        SearchesConfigService scs,
        GoogleSearchService gss,
        SearxSearchService sss,
        YtdlpYoutubeSearchService ytdlp,
        YtdlYoutubeSearchService ytdl,
        YoutubeDataApiSearchService ytdata,
        InvidiousYtSearchService iYtSs)
    {
        _scs = scs;
        _sss = sss;
        _gss = gss;
        _iYtSs = iYtSs;

        _ytdlp = ytdlp;
        _ytdl = ytdl;
        _ytdata = ytdata;
    }

    public ISearchService GetSearchService(string? hint = null)
        => _scs.Data.WebSearchEngine switch
        {
            WebSearchEngine.Google => _gss,
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