using WizBot.Core.Services.Database.Models;
using System.Collections.Generic;
using System.Linq;

namespace WizBot.Core.Services.Database.Repositories
{
    public interface ISelfAssignedRolesRepository : IRepository<SelfAssignedRole>
    {
        bool DeleteByGuildAndRoleId(ulong guildId, ulong roleId);
        IGrouping<int, SelfAssignedRole>[] GetFromGuild(ulong guildId);
    }
}
