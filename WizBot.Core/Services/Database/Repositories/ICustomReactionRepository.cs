using System.Collections.Generic;
using System.Threading.Tasks;
using WizBot.Core.Services.Database.Models;

namespace WizBot.Core.Services.Database.Repositories
{
    public interface ICustomReactionRepository : IRepository<CustomReaction>
    {
        IEnumerable<CustomReaction> GetGlobal();
        Task<List<CustomReaction>> GetFor(IEnumerable<ulong> ids);
        IEnumerable<CustomReaction> ForId(ulong id);
        int ClearFromGuild(ulong id);
        CustomReaction GetByGuildIdAndInput(ulong? guildId, string input);
    }
}
