using NadekoBot.Db.Models;

namespace NadekoBot.Extensions;

public static class UserExtensions
{
    // This method is used by everything that fetches the avatar from a user
    public static Uri RealAvatarUrl(this IUser usr, ushort size = 256)
        => usr.AvatarId is null ? new(usr.GetDefaultAvatarUrl()) : new Uri(usr.GetAvatarUrl(ImageFormat.Auto, size));

    // This method is only used for the xp card
    public static Uri RealAvatarUrl(this DiscordUser usr)
        => usr.AvatarId is null
            ? new(CDN.GetDefaultUserAvatarUrl(ushort.Parse(usr.Discriminator)))
            : new Uri(usr.AvatarId.StartsWith("a_", StringComparison.InvariantCulture)
                ? $"{DiscordConfig.CDNUrl}avatars/{usr.UserId}/{usr.AvatarId}.gif"
                : $"{DiscordConfig.CDNUrl}avatars/{usr.UserId}/{usr.AvatarId}.png");
}