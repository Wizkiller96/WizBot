using NadekoBot.Db.Models;

namespace NadekoBot.Extensions;

public static class UserExtensions
{
    public static async Task<IUserMessage> EmbedAsync(this IUser user, IEmbedBuilder embed, string msg = "")
    {
        var ch = await user.CreateDMChannelAsync();
        return await ch.EmbedAsync(embed, msg);
    }

    public static async Task<IUserMessage> SendAsync(this IUser user, SmartText text, bool sanitizeAll = false)
    {
        var ch = await user.CreateDMChannelAsync();
        return await ch.SendAsync(text, sanitizeAll);
    }

    public static async Task<IUserMessage> SendConfirmAsync(this IUser user, IEmbedBuilderService eb, string text)
        => await user.SendMessageAsync("", embed: eb.Create().WithOkColor().WithDescription(text).Build());

    public static async Task<IUserMessage> SendErrorAsync(this IUser user, IEmbedBuilderService eb, string error)
        => await user.SendMessageAsync("", embed: eb.Create().WithErrorColor().WithDescription(error).Build());

    public static async Task<IUserMessage> SendPendingAsync(this IUser user, IEmbedBuilderService eb, string message)
        => await user.SendMessageAsync("", embed: eb.Create().WithPendingColor().WithDescription(message).Build());

    // This method is used by everything that fetches the avatar from a user
    public static Uri RealAvatarUrl(this IUser usr, ushort size = 256)
        => usr.AvatarId is null ? new(usr.GetDefaultAvatarUrl()) : new Uri(usr.GetAvatarUrl(ImageFormat.Auto, size));

    // This method is only used for the xp card
    public static Uri? RealAvatarUrl(this DiscordUser usr)
        => usr.AvatarId is null
            ? null
            : new Uri(usr.AvatarId.StartsWith("a_", StringComparison.InvariantCulture)
                ? $"{DiscordConfig.CDNUrl}avatars/{usr.UserId}/{usr.AvatarId}.gif"
                : $"{DiscordConfig.CDNUrl}avatars/{usr.UserId}/{usr.AvatarId}.png");
}