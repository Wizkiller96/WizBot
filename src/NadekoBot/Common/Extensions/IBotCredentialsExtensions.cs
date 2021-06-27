using Discord;
using NadekoBot.Common;

namespace NadekoBot.Extensions
{
    public static class BotCredentialsExtensions
    {
        public static bool IsOwner(this IBotCredentials creds, IUser user)
            => creds.OwnerIds.Contains(user.Id);
    }
}