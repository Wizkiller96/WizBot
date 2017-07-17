using WizBot.Modules.Music.Common.SongResolver.Strategies;
using WizBot.Services.Database.Models;
using System.Threading.Tasks;

namespace WizBot.Modules.Music.Common.SongResolver
{
    public interface ISongResolverFactory
    {
        Task<IResolveStrategy> GetResolveStrategy(string query, MusicType? musicType);
    }
}
