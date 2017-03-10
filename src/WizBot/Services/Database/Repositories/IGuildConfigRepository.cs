using Microsoft.EntityFrameworkCore;
using WizBot.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WizBot.Services.Database.Repositories
{
    public interface IGuildConfigRepository : IRepository<GuildConfig>
    {
        GuildConfig For(ulong guildId, Func<DbSet<GuildConfig>, IQueryable<GuildConfig>> includes = null);
        GuildConfig LogSettingsFor(ulong guildId);
        IEnumerable<GuildConfig> OldPermissionsForAll();
        IEnumerable<GuildConfig> GetAllGuildConfigs();
        IEnumerable<FollowedStream> GetAllFollowedStreams();
        void SetCleverbotEnabled(ulong id, bool cleverbotEnabled);
        IEnumerable<GuildConfig> Permissionsv2ForAll();
    }
}
