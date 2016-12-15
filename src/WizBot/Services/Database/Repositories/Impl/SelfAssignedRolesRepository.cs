﻿using WizBot.Services.Database.Models;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace WizBot.Services.Database.Repositories.Impl
{
    public class SelfAssignedRolesRepository : Repository<SelfAssignedRole>, ISelfAssignedRolesRepository
    {
        public SelfAssignedRolesRepository(DbContext context) : base(context)
        {
        }

        public bool DeleteByGuildAndRoleId(ulong guildId, ulong roleId)
        {
            var role = _set.Where(s => s.GuildId == guildId && s.RoleId == roleId).FirstOrDefault();

            if (role == null)
                return false;

            _set.Remove(role);
            return true;
        }

        public IEnumerable<SelfAssignedRole> GetFromGuild(ulong guildId) => 
            _set.Where(s => s.GuildId == guildId).ToList();
    }
}
