#nullable disable
using NadekoBot.Db.Models;
using System.Text.Json.Serialization;

namespace NadekoBot.Modules.Searches.Common;

public readonly struct StreamDataKey
{
    public FollowedStream.FType Type { get; }
    public string Name { get; }

    [JsonConstructor]
    public StreamDataKey(FollowedStream.FType type, string name)
    {
        Type = type;
        Name = name;
    }
}