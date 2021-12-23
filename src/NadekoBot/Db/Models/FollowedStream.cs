﻿using NadekoBot.Services.Database.Models;
using NadekoBot.Modules.Searches.Common;

namespace NadekoBot.Db.Models;

public class FollowedStream : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public string Username { get; set; }
    public FType Type { get; set; }
    public string Message { get; set; }

    public enum FType
    {
        Twitch = 0,
        Picarto = 3,
        Youtube = 4,
        Facebook = 5,
    }

    protected bool Equals(FollowedStream other)
    {
        return ChannelId == other.ChannelId 
               && Username.Trim().ToUpperInvariant() == other.Username.Trim().ToUpperInvariant() 
               && Type == other.Type;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ChannelId, Username, (int) Type);
    }

    public override bool Equals(object obj) 
        => obj is FollowedStream fs && Equals(fs);

    public StreamDataKey CreateKey() =>
        new StreamDataKey(Type, Username.ToLower());
}