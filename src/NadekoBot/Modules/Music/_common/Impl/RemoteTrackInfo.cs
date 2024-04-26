namespace NadekoBot.Modules.Music;

public sealed record RemoteTrackInfo(
    string Id,
    string Title,
    string Url,
    string Thumbnail,
    TimeSpan Duration,
    MusicPlatform Platform,
    Func<Task<string?>> _streamFactory) : ITrackInfo
{
    private readonly Func<Task<string?>> _streamFactory = _streamFactory;

    public async ValueTask<string?> GetStreamUrl()
        => await _streamFactory();
}