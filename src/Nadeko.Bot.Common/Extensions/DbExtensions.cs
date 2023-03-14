using NadekoBot.Db;
using NadekoBot.Db.Models;
// todo fix these namespaces. It should only be Nadeko.Bot.Db
using NadekoBot.Services.Database;

namespace NadekoBot.Extensions;

public static class DbExtensions
{
    public static DiscordUser GetOrCreateUser(this NadekoContext ctx, IUser original, Func<IQueryable<DiscordUser>, IQueryable<DiscordUser>> includes = null)
        => ctx.GetOrCreateUser(original.Id, original.Username, original.Discriminator, original.AvatarId, includes);
}