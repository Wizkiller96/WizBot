using NadekoBot.Db.Models;
using NadekoBot.Modules.Searches.Common;

namespace NadekoBot.Modules.Searches._Common;

public static class Extensions
{
    public static StreamDataKey CreateKey(this FollowedStream fs)
        => new(fs.Type, fs.Username.ToLower());
}