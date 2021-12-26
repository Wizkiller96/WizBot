using Discord;
using NadekoBot.Db.Models;

namespace NadekoBot.Extensions;

public static class IUserExtensions
{
    public static async Task<IUserMessage> EmbedAsync(this IUser user, IEmbedBuilder embed, string msg = "")
    {
        var ch = await user.CreateDMChannelAsync();
        return await ch.EmbedAsync(embed, msg);
    }
    
    public static async Task<IUserMessage> SendAsync(this IUser user, string plainText, Embed embed, bool sanitizeAll = false)
    {
        var ch = await user.CreateDMChannelAsync();
        return await ch.SendAsync(plainText, embed, sanitizeAll);
    }

    public static async Task<IUserMessage> SendAsync(this IUser user, SmartText text, bool sanitizeAll = false)
    {
        var ch = await user.CreateDMChannelAsync();
        return await ch.SendAsync(text, sanitizeAll);
    }
    
    public static async Task<IUserMessage> SendConfirmAsync(this IUser user, IEmbedBuilderService eb, string text)
        => await user.SendMessageAsync("", embed: eb.Create()
            .WithOkColor()
            .WithDescription(text)
            .Build());

    public static async Task<IUserMessage> SendConfirmAsync(this IUser user, IEmbedBuilderService eb, string title, string text, string url = null)
    {
        var embed = eb.Create().WithOkColor().WithDescription(text).WithTitle(title);
        if (url != null && Uri.IsWellFormedUriString(url, UriKind.Absolute))
            embed.WithUrl(url);
            
        return await user.SendMessageAsync("", embed: embed.Build());
    }

    public static async Task<IUserMessage> SendErrorAsync(this IUser user, IEmbedBuilderService eb, string title, string error, string url = null)
    {
        var embed = eb.Create().WithErrorColor().WithDescription(error).WithTitle(title);
        if (url != null && Uri.IsWellFormedUriString(url, UriKind.Absolute))
            embed.WithUrl(url);

        return await user.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
    }

    public static async Task<IUserMessage> SendErrorAsync(this IUser user, IEmbedBuilderService eb, string error)
        => await user
            .SendMessageAsync("", embed: eb.Create()
                .WithErrorColor()
                .WithDescription(error)
                .Build());

    public static async Task<IUserMessage> SendFileAsync(this IUser user, string filePath, string caption = null, string text = null, bool isTTS = false)
    {
        await using var file = File.Open(filePath, FileMode.Open);
        return await UserExtensions.SendFileAsync(user, file, caption ?? "x", text, isTTS).ConfigureAwait(false);
    }

    public static async Task<IUserMessage> SendFileAsync(this IUser user, Stream fileStream, string fileName, string caption = null, bool isTTS = false) =>
        await UserExtensions.SendFileAsync(user, fileStream, fileName, caption, isTTS).ConfigureAwait(false);

    // This method is used by everything that fetches the avatar from a user
    public static Uri RealAvatarUrl(this IUser usr, ushort size = 128)
    {
        return usr.AvatarId is null
            ? new(usr.GetDefaultAvatarUrl())
            : new Uri(usr.GetAvatarUrl(ImageFormat.Auto, size));
    }

    // This method is only used for the xp card
    public static Uri RealAvatarUrl(this DiscordUser usr)
    {
        return usr.AvatarId is null
            ? null
            : new Uri(usr.AvatarId.StartsWith("a_", StringComparison.InvariantCulture)
                ? $"{DiscordConfig.CDNUrl}avatars/{usr.UserId}/{usr.AvatarId}.gif"
                : $"{DiscordConfig.CDNUrl}avatars/{usr.UserId}/{usr.AvatarId}.png");
    }
}