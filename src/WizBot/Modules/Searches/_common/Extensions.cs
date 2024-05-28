using WizBot.Db.Models;

namespace WizBot.Modules.Searches.Common;

public static class Extensions
{
    public static StreamDataKey CreateKey(this FollowedStream fs)
        => new(fs.Type, fs.Username.ToLower());
}