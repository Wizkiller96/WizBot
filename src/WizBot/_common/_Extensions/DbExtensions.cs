using Microsoft.EntityFrameworkCore;
using WizBot.Db;
using WizBot.Db.Models;

namespace WizBot.Extensions;

public static class DbExtensions
{
    public static DiscordUser GetOrCreateUser(this DbContext ctx, IUser original, Func<IQueryable<DiscordUser>, IQueryable<DiscordUser>>? includes = null)
        => ctx.GetOrCreateUser(original.Id, original.Username, original.Discriminator, original.AvatarId, includes);
}