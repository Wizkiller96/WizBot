using System;
using System.Collections.Generic;
using System.Linq;

namespace NadekoBot.Modules.Nsfw.Common
{
    public static class ImageDownloaderHelper
    {
        public static string GetTagString(IEnumerable<string> tags, bool isExplicit = false)
        {
            if (isExplicit)
                tags = tags.Append("rating:explicit");
            
            return string.Join('+', tags.Select(x => x.ToLowerInvariant()));
        }
    }
}