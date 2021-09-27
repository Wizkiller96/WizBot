using System.Collections.Generic;

namespace WizBot.Modules.Music
{
    public interface ILocalTrackResolver : IPlatformQueryResolver
    {
        IAsyncEnumerable<ITrackInfo> ResolveDirectoryAsync(string dirPath);
    }
}