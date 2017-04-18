using WizBot.Services.Database.Models;
using System.Threading.Tasks;

namespace WizBot.Services.Database.Repositories
{
    public interface IWarningsRepository : IRepository<Warning>
    {
        Warning[] For(ulong guildId, ulong userId);
        Task ForgiveAll(ulong guildId, ulong userId, string mod);
    }
}
