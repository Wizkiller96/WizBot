using System.Collections.Generic;

namespace NadekoBot.Core.Modules.Music
{
    public interface ILocalTrackResolver : IPlatformQueryResolver
    {
        IAsyncEnumerable<ITrackInfo> ResolveDirectoryAsync(string dirPath);
    }
}