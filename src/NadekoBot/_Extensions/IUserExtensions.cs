using Discord;
using NadekoBot.Services.Database.Models;
using System;
using System.IO;
using System.Threading.Tasks;
using NadekoBot.Db.Models;
using NadekoBot.Services;

namespace NadekoBot.Extensions
{
    public static class IUserExtensions
    {
        public static async Task<IUserMessage> SendConfirmAsync(this IUser user, IEmbedBuilderService eb, string text)
             => await (await user.GetOrCreateDMChannelAsync()).SendMessageAsync("", embed: eb.Create()
                 .WithOkColor()
                 .WithDescription(text)
                 .Build());

        public static async Task<IUserMessage> SendConfirmAsync(this IUser user, IEmbedBuilderService eb, string title, string text, string url = null)
        {
            var embed = eb.Create().WithOkColor().WithDescription(text).WithTitle(title);
            if (url != null && Uri.IsWellFormedUriString(url, UriKind.Absolute))
                embed.WithUrl(url);
            
            return await (await user.GetOrCreateDMChannelAsync()).SendMessageAsync("", embed: embed.Build());
        }

        public static async Task<IUserMessage> SendErrorAsync(this IUser user, IEmbedBuilderService eb, string title, string error, string url = null)
        {
            var embed = eb.Create().WithErrorColor().WithDescription(error).WithTitle(title);
            if (url != null && Uri.IsWellFormedUriString(url, UriKind.Absolute))
                embed.WithUrl(url);

            return await (await user.GetOrCreateDMChannelAsync().ConfigureAwait(false)).SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
        }

        public static async Task<IUserMessage> SendErrorAsync(this IUser user, IEmbedBuilderService eb, string error)
            => await (await user.GetOrCreateDMChannelAsync())
                .SendMessageAsync("", embed: eb.Create()
                    .WithErrorColor()
                    .WithDescription(error)
                    .Build());

        public static async Task<IUserMessage> SendFileAsync(this IUser user, string filePath, string caption = null, string text = null, bool isTTS = false)
        {
            using (var file = File.Open(filePath, FileMode.Open))
            {
                return await (await user.GetOrCreateDMChannelAsync().ConfigureAwait(false)).SendFileAsync(file, caption ?? "x", text, isTTS).ConfigureAwait(false);
            }
        }

        public static async Task<IUserMessage> SendFileAsync(this IUser user, Stream fileStream, string fileName, string caption = null, bool isTTS = false) =>
            await (await user.GetOrCreateDMChannelAsync().ConfigureAwait(false)).SendFileAsync(fileStream, fileName, caption, isTTS).ConfigureAwait(false);

        // This method is used by everything that fetches the avatar from a user
        public static Uri RealAvatarUrl(this IUser usr, ushort size = 128)
        {
            return usr.AvatarId is null
                ? new Uri(usr.GetDefaultAvatarUrl())
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
}