using NadekoBot.Services.Database.Models;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace NadekoBot.Db
{
    public static class SelfAssignableRolesExtensions
    {
        public static bool DeleteByGuildAndRoleId(this DbSet<SelfAssignedRole> roles, ulong guildId, ulong roleId)
        {
            var role = roles.FirstOrDefault(s => s.GuildId == guildId && s.RoleId == roleId);

            if (role is null)
                return false;

            roles.Remove(role);
            return true;
        }

        public static IEnumerable<SelfAssignedRole> GetFromGuild(this DbSet<SelfAssignedRole> roles, ulong guildId) 
            =>  roles.AsQueryable()
                    .Where(s => s.GuildId == guildId)
                    .ToArray();
    }
}
