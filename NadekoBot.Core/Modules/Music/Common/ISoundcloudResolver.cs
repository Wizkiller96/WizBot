using System.Collections.Generic;

namespace NadekoBot.Core.Modules.Music
{
    public interface ISoundcloudResolver : IPlatformQueryResolver
    {
        bool IsSoundCloudLink(string url);
        IAsyncEnumerable<ITrackInfo> ResolvePlaylistAsync(string playlist);
    }
}