using Discord;

namespace WizBot.Snake;

[AttributeUsage(AttributeTargets.Method)]
public sealed class user_permAttribute : Attribute
{
    public GuildPermission? GuildPerm { get; }
    public ChannelPermission? ChannelPerm { get; }
    

    public user_permAttribute(GuildPermission perm)
    {
        GuildPerm = perm;
        ChannelPerm = null;
    }

    public user_permAttribute(ChannelPermission perm)
    {
        ChannelPerm = perm;
        GuildPerm = null;
    }
}