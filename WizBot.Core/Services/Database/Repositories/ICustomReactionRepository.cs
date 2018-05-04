using System.Collections.Generic;
using WizBot.Core.Services.Database.Models;

namespace WizBot.Core.Services.Database.Repositories
{
    public interface ICustomReactionRepository : IRepository<CustomReaction>
    {
        CustomReaction[] GetGlobalAndFor(IEnumerable<long> ids);
        CustomReaction[] For(ulong id);
        int ClearFromGuild(ulong id);
    }
}
