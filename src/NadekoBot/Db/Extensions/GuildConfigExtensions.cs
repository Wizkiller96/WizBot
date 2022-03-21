#nullable disable
using Microsoft.EntityFrameworkCore;
using NadekoBot.Db.Models;
using NadekoBot.Services.Database;
using NadekoBot.Services.Database.Models;

namespace NadekoBot.Db;

public static class GuildConfigExtensions
{
    private static List<WarningPunishment> DefaultWarnPunishments
        => new()
        {
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
        };

    /// <summary>
    ///     Gets full stream role settings for the guild with the specified id.
    /// </summary>
    /// <param name="ctx">Db Context</param>
    /// <param name="guildId">Id of the guild to get stream role settings for.</param>
    /// <returns>Guild'p stream role settings</returns>
    public static StreamRoleSettings GetStreamRoleSettings(this NadekoContext ctx, ulong guildId)
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
        => configs.AsQueryable()
                  .AsSplitQuery()
                  .Include(gc => gc.CommandCooldowns)
                  .Include(gc => gc.FollowedStreams)
                  .Include(gc => gc.StreamRole)
                  .Include(gc => gc.XpSettings)
                  .ThenInclude(x => x.ExclusionList)
                  .Include(gc => gc.DelMsgOnCmdChannels)
                  .Include(gc => gc.ReactionRoleMessages)
                  .ThenInclude(x => x.ReactionRoles);

    public static IEnumerable<GuildConfig> GetAllGuildConfigs(
        this DbSet<GuildConfig> configs,
        List<ulong> availableGuilds)
        => configs.IncludeEverything().AsNoTracking().Where(x => availableGuilds.Contains(x.GuildId)).ToList();

    /// <summary>
    ///     Gets and creates if it doesn't exist a config for a guild.
    /// </summary>
    /// <param name="ctx">Context</param>
    /// <param name="guildId">Id of the guide</param>
    /// <param name="includes">Use to manipulate the set however you want. Pass null to include everything</param>
    /// <returns>Config for the guild</returns>
    public static GuildConfig GuildConfigsForId(
        this NadekoContext ctx,
        ulong guildId,
        Func<DbSet<GuildConfig>, IQueryable<GuildConfig>> includes)
    {
        GuildConfig config;

        // todo linq2db
        if (includes is null)
            config = ctx.GuildConfigs.IncludeEverything().FirstOrDefault(c => c.GuildId == guildId);
        else
        {
            var set = includes(ctx.GuildConfigs);
            config = set.FirstOrDefault(c => c.GuildId == guildId);
        }

        if (config is null)
        {
            ctx.GuildConfigs.Add(config = new()
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

    public static LogSetting LogSettingsFor(this NadekoContext ctx, ulong guildId)
    {
        var logSetting = ctx.LogSettings.AsQueryable()
                            .Include(x => x.LogIgnores)
                            .Where(x => x.GuildId == guildId)
                            .FirstOrDefault();

        if (logSetting is null)
        {
            ctx.LogSettings.Add(logSetting = new()
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

    public static GuildConfig GcWithPermissionsFor(this NadekoContext ctx, ulong guildId)
    {
        var config = ctx.GuildConfigs.AsQueryable()
                        .Where(gc => gc.GuildId == guildId)
                        .Include(gc => gc.Permissions)
                        .FirstOrDefault();

        if (config is null) // if there is no guildconfig, create new one
        {
            ctx.GuildConfigs.Add(config = new()
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

    public static void SetCleverbotEnabled(this DbSet<GuildConfig> configs, ulong id, bool cleverbotEnabled)
    {
        var conf = configs.FirstOrDefault(gc => gc.GuildId == id);

        if (conf is null)
            return;

        conf.CleverbotEnabled = cleverbotEnabled;
    }

    public static XpSettings XpSettingsFor(this NadekoContext ctx, ulong guildId)
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