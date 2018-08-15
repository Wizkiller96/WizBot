using WizBot.Core.Services.Database.Models;
using System.Collections.Generic;

namespace WizBot.Core.Services.Database.Repositories
{
    public interface ISelfAssignedRolesRepository : IRepository<SelfAssignedRole>
    {
        bool DeleteByGuildAndRoleId(ulong guildId, ulong roleId);
        IEnumerable<SelfAssignedRole> GetFromGuild(ulong guildId);
    }
}
