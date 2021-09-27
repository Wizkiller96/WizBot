#nullable enable
using System.Threading.Tasks;

namespace WizBot.Modules.Music
{
    public interface IPlatformQueryResolver
    {
        Task<ITrackInfo?> ResolveByQueryAsync(string query);
    }
}