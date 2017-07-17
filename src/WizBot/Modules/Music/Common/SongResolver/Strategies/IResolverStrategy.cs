using System.Threading.Tasks;

namespace WizBot.Modules.Music.Common.SongResolver.Strategies
{
    public interface IResolveStrategy
    {
        Task<SongInfo> ResolveSong(string query);
    }
}
