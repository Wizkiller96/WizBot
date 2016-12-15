using WizBot.Services.Database.Models;
using System.Collections.Generic;

namespace WizBot.Services.Database.Repositories
{
    public interface ISelfAssignedRolesRepository : IRepository<SelfAssignedRole>
    {
        bool DeleteByGuildAndRoleId(ulong guildId, ulong roleId);
        IEnumerable<SelfAssignedRole> GetFromGuild(ulong guildId);
    }
}
