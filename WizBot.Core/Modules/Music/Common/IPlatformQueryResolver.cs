#nullable enable
using System.Threading.Tasks;

namespace WizBot.Core.Modules.Music
{
    public interface IPlatformQueryResolver
    {
        Task<ITrackInfo?> ResolveByQueryAsync(string query);
    }
}