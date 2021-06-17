#nullable enable
using System.Threading.Tasks;

namespace NadekoBot.Core.Modules.Music
{
    public interface IPlatformQueryResolver
    {
        Task<ITrackInfo?> ResolveByQueryAsync(string query);
    }
}