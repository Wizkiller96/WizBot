using System.Threading.Tasks;

namespace WizBot.Services.Music.SongResolver.Strategies
{
    public interface IResolveStrategy
    {
        Task<SongInfo> ResolveSong(string query);
    }
}