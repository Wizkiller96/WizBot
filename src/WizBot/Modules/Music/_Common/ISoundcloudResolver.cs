#nullable disable
namespace WizBot.Modules.Music;

public interface ISoundcloudResolver : IPlatformQueryResolver
{
    bool IsSoundCloudLink(string url);
    IAsyncEnumerable<ITrackInfo> ResolvePlaylistAsync(string playlist);
}