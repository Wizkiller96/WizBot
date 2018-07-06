using System.Collections.Generic;
using WizBot.Core.Services.Database.Models;

namespace WizBot.Core.Services.Database.Repositories
{
    public interface ICustomReactionRepository : IRepository<CustomReaction>
    {
        CustomReaction[] GetGlobalAndFor(IEnumerable<ulong> ids);
        CustomReaction[] ForId(ulong id);
        int ClearFromGuild(ulong id);
    }
}