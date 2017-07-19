using Microsoft.EntityFrameworkCore;
using WizBot.Services.Database.Models;
using WizBot.Services.Database.Repositories;

namespace WizBot.Modules.Utility.Extensions
{
    public static class StreamRoleExtensions
    {
        /// <summary>
        /// Gets full stream role settings for the guild with the specified id.
        /// </summary>
        /// <param name="gc"></param>
        /// <param name="guildId">Id of the guild to get stream role settings for.</param>
        /// <returns></returns>
        public static StreamRoleSettings GetStreamRoleSettings(this IGuildConfigRepository gc, ulong guildId)
        {
            var conf = gc.For(guildId, x => x.Include(y => y.StreamRole)
                .Include(y => y.StreamRole.Whitelist)
                .Include(y => y.StreamRole.Blacklist));

            if (conf.StreamRole == null)
                conf.StreamRole = new StreamRoleSettings();

            return conf.StreamRole;
        }
    }
}