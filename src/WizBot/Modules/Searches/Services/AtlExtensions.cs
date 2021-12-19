using System.Linq;
using System.Threading.Tasks;
using LinqToDB.EntityFrameworkCore;
using WizBot.Services.Database.Models;

namespace WizBot.Modules.Searches
{
    public static class AtlExtensions
    {
        public static Task<AutoTranslateChannel> GetByChannelId(this IQueryable<AutoTranslateChannel> set, ulong channelId)
            => set.FirstOrDefaultAsyncLinqToDB(x => x.ChannelId == channelId);
    }
}