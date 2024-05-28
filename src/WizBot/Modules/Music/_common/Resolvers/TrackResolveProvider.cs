﻿namespace WizBot.Modules.Music;

public sealed class TrackResolveProvider : ITrackResolveProvider
{
    private readonly IYoutubeResolver _ytResolver;
    private readonly ILocalTrackResolver _localResolver;
    private readonly IRadioResolver _radioResolver;

    public TrackResolveProvider(
        IYoutubeResolver ytResolver,
        ILocalTrackResolver localResolver,
        IRadioResolver radioResolver)
    {
        _ytResolver = ytResolver;
        _localResolver = localResolver;
        _radioResolver = radioResolver;
    }

    public Task<ITrackInfo?> QuerySongAsync(string query, MusicPlatform? forcePlatform)
    {
        switch (forcePlatform)
        {
            case MusicPlatform.Radio:
                return _radioResolver.ResolveByQueryAsync(query);
            case MusicPlatform.Youtube:
                return _ytResolver.ResolveByQueryAsync(query);
            case MusicPlatform.Local:
                return _localResolver.ResolveByQueryAsync(query);
            case null:
                var match = _ytResolver.YtVideoIdRegex.Match(query);
                if (match.Success)
                    return _ytResolver.ResolveByIdAsync(match.Groups["id"].Value);
                else if (Uri.TryCreate(query, UriKind.Absolute, out var uri) && uri.IsFile)
                    return _localResolver.ResolveByQueryAsync(uri.AbsolutePath);
                else if (IsRadioLink(query))
                    return _radioResolver.ResolveByQueryAsync(query);
                else
                    return _ytResolver.ResolveByQueryAsync(query, false);
            default:
                Log.Error("Unsupported platform: {MusicPlatform}", forcePlatform);
                return Task.FromResult<ITrackInfo?>(null);
        }
    }

    public static bool IsRadioLink(string query)
        => (query.StartsWith("http", StringComparison.InvariantCulture)
            || query.StartsWith("ww", StringComparison.InvariantCulture))
           && (query.Contains(".pls") || query.Contains(".m3u") || query.Contains(".asx") || query.Contains(".xspf"));
}