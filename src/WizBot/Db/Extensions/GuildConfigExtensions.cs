#nullable disable
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WizBot.Db.Models;

namespace WizBot.Db;

public static class GuildConfigExtensions
{
    private static List<WarningPunishment> DefaultWarnPunishments
        =>
        [
            new()
            {
                Count = 3,
                Punishment = PunishmentAction.Kick
            },

            new()
            {
                Count = 5,
                Punishment = PunishmentAction.Ban
            }
        ];

    /// <summary>
    ///     Gets full stream role settings for the guild with the specified id.
    /// </summary>
    /// <param name="ctx">Db Context</param>
    /// <param name="guildId">Id of the guild to get stream role settings for.</param>
    /// <returns>Guild'p stream role settings</returns>
    public static StreamRoleSettings GetStreamRoleSettings(this DbContext ctx, ulong guildId)
    {
        var conf = ctx.GuildConfigsForId(guildId,
            set => set.Include(y => y.StreamRole)
                      .Include(y => y.StreamRole.Whitelist)
                      .Include(y => y.StreamRole.Blacklist));

        if (conf.StreamRole is null)
            conf.StreamRole = new();

        return conf.StreamRole;
    }

    private static IQueryable<GuildConfig> IncludeEverything(this DbSet<GuildConfig> configs)
        => configs
           .AsSplitQuery()
           .Include(gc => gc.CommandCooldowns)
           .Include(gc => gc.FollowedStreams)
           .Include(gc => gc.StreamRole)
           .Include(gc => gc.DelMsgOnCmdChannels)
           .Include(gc => gc.XpSettings)
           .ThenInclude(x => x.ExclusionList);

    public static async Task<GuildConfig[]> GetAllGuildConfigs(
        this DbSet<GuildConfig> configs,
        List<ulong> availableGuilds)
    {
        var result = await configs
                           .AsQueryable()
                           .Include(x => x.CommandCooldowns)
                           .Where(x => availableGuilds.Contains(x.GuildId))
                           .AsNoTracking()
                           .ToArrayAsync();

        return result;
    }

    /// <summary>
    ///     Gets and creates if it doesn't exist a config for a guild.
    /// </summary>
    /// <param name="ctx">Context</param>
    /// <param name="guildId">Id of the guide</param>
    /// <param name="includes">Use to manipulate the set however you want. Pass null to include everything</param>
    /// <returns>Config for the guild</returns>
    public static GuildConfig GuildConfigsForId(
        this DbContext ctx,
        ulong guildId,
        Func<DbSet<GuildConfig>, IQueryable<GuildConfig>> includes)
    {
        GuildConfig config;

        if (includes is null)
            config = ctx.Set<GuildConfig>().IncludeEverything().FirstOrDefault(c => c.GuildId == guildId);
        else
        {
            var set = includes(ctx.Set<GuildConfig>());
            config = set.FirstOrDefault(c => c.GuildId == guildId);
        }

        if (config is null)
        {
            ctx.Set<GuildConfig>()
               .Add(config = new()
               {
                   GuildId = guildId,
                   Permissions = Permissionv2.GetDefaultPermlist,
                   WarningsInitialized = true,
                   WarnPunishments = DefaultWarnPunishments
               });
            ctx.SaveChanges();
        }

        if (!config.WarningsInitialized)
        {
            config.WarningsInitialized = true;
            config.WarnPunishments = DefaultWarnPunishments;
        }

        return config;

        // ctx.GuildConfigs
        //    .ToLinqToDBTable()
        //    .InsertOrUpdate(() => new()
        //        {
        //            GuildId = guildId,
        //            Permissions = Permissionv2.GetDefaultPermlist,
        //            WarningsInitialized = true,
        //            WarnPunishments = DefaultWarnPunishments
        //        },
        //        _ => new(),
        //        () => new()
        //        {
        //            GuildId = guildId
        //        });
        //
        // if(includes is null)
        // return ctx.GuildConfigs
        //    .ToLinqToDBTable()
        //    .First(x => x.GuildId == guildId);
    }

    public static LogSetting LogSettingsFor(this DbContext ctx, ulong guildId)
    {
        var logSetting = ctx.Set<LogSetting>()
                            .AsQueryable()
                            .Include(x => x.LogIgnores)
                            .Where(x => x.GuildId == guildId)
                            .FirstOrDefault();

        if (logSetting is null)
        {
            ctx.Set<LogSetting>()
               .Add(logSetting = new()
               {
                   GuildId = guildId
               });
            ctx.SaveChanges();
        }

        return logSetting;
    }

    public static IEnumerable<GuildConfig> PermissionsForAll(this DbSet<GuildConfig> configs, List<ulong> include)
    {
        var query = configs.AsQueryable().Where(x => include.Contains(x.GuildId)).Include(gc => gc.Permissions);

        return query.ToList();
    }

    public static GuildConfig GcWithPermissionsFor(this DbContext ctx, ulong guildId)
    {
        var config = ctx.Set<GuildConfig>()
                        .AsQueryable()
                        .Where(gc => gc.GuildId == guildId)
                        .Include(gc => gc.Permissions)
                        .FirstOrDefault();

        if (config is null) // if there is no guildconfig, create new one
        {
            ctx.Set<GuildConfig>()
               .Add(config = new()
               {
                   GuildId = guildId,
                   Permissions = Permissionv2.GetDefaultPermlist
               });
            ctx.SaveChanges();
        }
        else if (config.Permissions is null || !config.Permissions.Any()) // if no perms, add default ones
        {
            config.Permissions = Permissionv2.GetDefaultPermlist;
            ctx.SaveChanges();
        }

        return config;
    }

    public static IEnumerable<FollowedStream> GetFollowedStreams(this DbSet<GuildConfig> configs)
        => configs.AsQueryable().Include(x => x.FollowedStreams).SelectMany(gc => gc.FollowedStreams).ToArray();

    public static IEnumerable<FollowedStream> GetFollowedStreams(this DbSet<GuildConfig> configs, List<ulong> included)
        => configs.AsQueryable()
                  .Where(gc => included.Contains(gc.GuildId))
                  .Include(gc => gc.FollowedStreams)
                  .SelectMany(gc => gc.FollowedStreams)
                  .ToList();


    public static XpSettings XpSettingsFor(this DbContext ctx, ulong guildId)
    {
        var gc = ctx.GuildConfigsForId(guildId,
            set => set.Include(x => x.XpSettings)
                      .ThenInclude(x => x.RoleRewards)
                      .Include(x => x.XpSettings)
                      .ThenInclude(x => x.CurrencyRewards)
                      .Include(x => x.XpSettings)
                      .ThenInclude(x => x.ExclusionList));

        if (gc.XpSettings is null)
            gc.XpSettings = new();

        return gc.XpSettings;
    }

    public static IEnumerable<GeneratingChannel> GetGeneratingChannels(this DbSet<GuildConfig> configs)
        => configs.AsQueryable()
                  .Include(x => x.GenerateCurrencyChannelIds)
                  .Where(x => x.GenerateCurrencyChannelIds.Any())
                  .SelectMany(x => x.GenerateCurrencyChannelIds)
                  .Select(x => new GeneratingChannel
                  {
                      ChannelId = x.ChannelId,
                      GuildId = x.GuildConfig.GuildId
                  })
                  .ToArray();

    public class GeneratingChannel
    {
        public ulong GuildId { get; set; }
        public ulong ChannelId { get; set; }
    }
}