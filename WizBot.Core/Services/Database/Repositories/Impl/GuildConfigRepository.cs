﻿using WizBot.Core.Services.Database.Models;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace WizBot.Core.Services.Database.Repositories.Impl
{
    public static class MusicPlayerSettingsExtensions
    {
        public static async Task<MusicPlayerSettings> ForGuildAsync(this DbSet<MusicPlayerSettings> settings, ulong guildId)
        {
            var toReturn = await settings
                .AsQueryable()
                .FirstOrDefaultAsync(x => x.GuildId == guildId);

            if (toReturn is null)
            {
                var newSettings = new MusicPlayerSettings()
                {
                    GuildId = guildId,
                    PlayerRepeat = PlayerRepeatType.Queue
                };

                await settings.AddAsync(newSettings);
                return newSettings;
            }

            return toReturn;
        }
    }
    
    public class GuildConfigRepository : Repository<GuildConfig>, IGuildConfigRepository
    {
        public GuildConfigRepository(DbContext context) : base(context)
        {
        }

        private List<WarningPunishment> DefaultWarnPunishments =>
            new List<WarningPunishment>() {
                new WarningPunishment() {
                    Count = 3,
                    Punishment = PunishmentAction.Kick
                },
                new WarningPunishment() {
                    Count = 5,
                    Punishment = PunishmentAction.Ban
                }
            };

        public IEnumerable<GuildConfig> GetAllGuildConfigs(List<ulong> availableGuilds) =>
            IncludeEverything()
                .AsNoTracking()
                .Where(x => availableGuilds.Contains(x.GuildId))
                .ToList();

        private IQueryable<GuildConfig> IncludeEverything()
        {
            return _set
                .AsQueryable()
                .Include(gc => gc.CommandCooldowns)
                .Include(gc => gc.FollowedStreams)
                .Include(gc => gc.StreamRole)
                .Include(gc => gc.NsfwBlacklistedTags)
                .Include(gc => gc.XpSettings)
                    .ThenInclude(x => x.ExclusionList)
                .Include(gc => gc.DelMsgOnCmdChannels)
                .Include(gc => gc.ReactionRoleMessages)
                    .ThenInclude(x => x.ReactionRoles)
                ;
        }

        /// <summary>
        /// Gets and creates if it doesn't exist a config for a guild.
        /// </summary>
        /// <param name="guildId">For which guild</param>
        /// <param name="includes">Use to manipulate the set however you want</param>
        /// <returns>Config for the guild</returns>
        public GuildConfig ForId(ulong guildId, Func<DbSet<GuildConfig>, IQueryable<GuildConfig>> includes)
        {
            GuildConfig config;

            if (includes == null)
            {
                config = IncludeEverything()
                    .FirstOrDefault(c => c.GuildId == guildId);
            }
            else
            {
                var set = includes(_set);
                config = set.FirstOrDefault(c => c.GuildId == guildId);
            }

            if (config == null)
            {
                _set.Add((config = new GuildConfig
                {
                    GuildId = guildId,
                    Permissions = Permissionv2.GetDefaultPermlist,
                    WarningsInitialized = true,
                    WarnPunishments = DefaultWarnPunishments,
                }));
                _context.SaveChanges();
            }

            if (!config.WarningsInitialized)
            {
                config.WarningsInitialized = true;
                config.WarnPunishments = DefaultWarnPunishments;
            }

            return config;
        }

        public GuildConfig LogSettingsFor(ulong guildId)
        {
            var config = _set
                .AsQueryable()
                .Include(gc => gc.LogSetting)
                    .ThenInclude(gc => gc.IgnoredChannels)
                .FirstOrDefault(x => x.GuildId == guildId);

            if (config == null)
            {
                _set.Add((config = new GuildConfig
                {
                    GuildId = guildId,
                    Permissions = Permissionv2.GetDefaultPermlist,
                    WarningsInitialized = true,
                    WarnPunishments = DefaultWarnPunishments,
                }));
                _context.SaveChanges();
            }

            if (!config.WarningsInitialized)
            {
                config.WarningsInitialized = true;
                config.WarnPunishments = DefaultWarnPunishments;
            }
            return config;
        }

        public IEnumerable<GuildConfig> Permissionsv2ForAll(List<ulong> include)
        {
            var query = _set.AsQueryable()
                .Where(x => include.Contains(x.GuildId))
                .Include(gc => gc.Permissions);

            return query.ToList();
        }

        public GuildConfig GcWithPermissionsv2For(ulong guildId)
        {
            var config = _set.AsQueryable()
                .Where(gc => gc.GuildId == guildId)
                .Include(gc => gc.Permissions)
                .FirstOrDefault();

            if (config == null) // if there is no guildconfig, create new one
            {
                _set.Add((config = new GuildConfig
                {
                    GuildId = guildId,
                    Permissions = Permissionv2.GetDefaultPermlist
                }));
                _context.SaveChanges();
            }
            else if (config.Permissions == null || !config.Permissions.Any()) // if no perms, add default ones
            {
                config.Permissions = Permissionv2.GetDefaultPermlist;
                _context.SaveChanges();
            }

            return config;
        }

        public IEnumerable<FollowedStream> GetFollowedStreams()
        {
            return _set
                .AsQueryable()
                .Include(x => x.FollowedStreams)
                .SelectMany(gc => gc.FollowedStreams)
                .ToArray();
        }

        public IEnumerable<FollowedStream> GetFollowedStreams(List<ulong> included)
        {
            return _set.AsQueryable()
                .Where(gc => included.Contains(gc.GuildId))
                .Include(gc => gc.FollowedStreams)
                .SelectMany(gc => gc.FollowedStreams)
                .ToList();
        }

        public void SetCleverbotEnabled(ulong id, bool cleverbotEnabled)
        {
            var conf = _set.FirstOrDefault(gc => gc.GuildId == id);

            if (conf == null)
                return;

            conf.CleverbotEnabled = cleverbotEnabled;
        }

        public XpSettings XpSettingsFor(ulong guildId)
        {
            var gc = ForId(guildId,
                set => set.Include(x => x.XpSettings)
                          .ThenInclude(x => x.RoleRewards)
                          .Include(x => x.XpSettings)
                          .ThenInclude(x => x.CurrencyRewards)
                          .Include(x => x.XpSettings)
                          .ThenInclude(x => x.ExclusionList));

            if (gc.XpSettings == null)
                gc.XpSettings = new XpSettings();

            return gc.XpSettings;
        }

        public IEnumerable<GeneratingChannel> GetGeneratingChannels()
        {
            return _set
                .AsQueryable()
                .Include(x => x.GenerateCurrencyChannelIds)
                .Where(x => x.GenerateCurrencyChannelIds.Any())
                .SelectMany(x => x.GenerateCurrencyChannelIds)
                .Select(x => new GeneratingChannel()
                {
                    ChannelId = x.ChannelId,
                    GuildId = x.GuildConfig.GuildId
                })
                .ToArray();
        }
    }
}