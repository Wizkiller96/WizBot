using System.Linq;
using System.Threading.Tasks;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Services.Database.Models;

namespace NadekoBot.Modules.Searches
{
    public static class AtlExtensions
    {
        public static Task<AutoTranslateChannel> GetByChannelId(this IQueryable<AutoTranslateChannel> set, ulong channelId)
            => set
                .Include(x => x.Users)
                .FirstOrDefaultAsyncEF(x => x.ChannelId == channelId);
    }
}