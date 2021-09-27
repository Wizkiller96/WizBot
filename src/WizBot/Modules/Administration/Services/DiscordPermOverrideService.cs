﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using WizBot.Common.ModuleBehaviors;
using WizBot.Services;
using WizBot.Services.Database.Models;
using WizBot.Extensions;

namespace WizBot.Modules.Administration.Services
{
    public class DiscordPermOverrideService : INService, ILateBlocker
    {
        private readonly DbService _db;
        private readonly IServiceProvider _services;

        public int Priority { get; } = int.MaxValue;

        private readonly ConcurrentDictionary<(ulong, string), DiscordPermOverride> _overrides;

        public DiscordPermOverrideService(DbService db, IServiceProvider services)
        {
            _db = db;
            _services = services;
            using var uow = _db.GetDbContext();
            _overrides = uow.DiscordPermOverrides
                .AsNoTracking()
                .AsEnumerable()
                .ToDictionary(o => (o.GuildId ?? 0, o.Command), o => o)
                .ToConcurrent();
        }
        
        public bool TryGetOverrides(ulong guildId, string commandName, out GuildPerm? perm)
        {
            commandName = commandName.ToLowerInvariant();
            if (_overrides.TryGetValue((guildId, commandName), out var dpo))
            {
                perm = dpo.Perm;
                return true;
            }

            perm = null;
            return false;
        }

        public Task<PreconditionResult> ExecuteOverrides(ICommandContext ctx, CommandInfo command,
            GuildPerm perms, IServiceProvider services)
        {
            var rupa = new RequireUserPermissionAttribute((GuildPermission) perms);
            return rupa.CheckPermissionsAsync(ctx, command, services);
        }

        public async Task AddOverride(ulong guildId, string commandName, GuildPerm perm)
        {
            commandName = commandName.ToLowerInvariant();
            using (var uow = _db.GetDbContext())
            {
                var over = await uow
                    .Set<DiscordPermOverride>()
                    .AsQueryable()
                    .FirstOrDefaultAsync(x => x.GuildId == guildId && commandName == x.Command);

                if (over is null)
                {
                    uow.Set<DiscordPermOverride>()
                        .Add(over = new DiscordPermOverride()
                        {
                            Command = commandName,
                            Perm = perm,
                            GuildId = guildId,
                        });
                }
                else
                {
                    over.Perm = perm;
                }

                _overrides[(guildId, commandName)] = over;

                await uow.SaveChangesAsync();
            }
        }

        public async Task ClearAllOverrides(ulong guildId)
        {
            using (var uow = _db.GetDbContext())
            {
                var overrides = await uow
                    .Set<DiscordPermOverride>()
                    .AsQueryable()
                    .AsNoTracking()
                    .Where(x => x.GuildId == guildId)
                    .ToListAsync();
                
                uow.RemoveRange(overrides);
                await uow.SaveChangesAsync();

                foreach (var over in overrides)
                {
                    _overrides.TryRemove((guildId, over.Command), out _);
                }
            }
        }
        
        public async Task RemoveOverride(ulong guildId, string commandName)
        {
            commandName = commandName.ToLowerInvariant();
            
            using (var uow = _db.GetDbContext())
            {
                var over = await uow
                    .Set<DiscordPermOverride>()
                    .AsQueryable()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.GuildId == guildId && x.Command == commandName);

                if (over is null)
                    return;
                
                uow.Remove(over);
                await uow.SaveChangesAsync();

                _overrides.TryRemove((guildId, commandName), out _);
            }
        }

        public Task<List<DiscordPermOverride>> GetAllOverrides(ulong guildId)
        {
            using (var uow = _db.GetDbContext())
            {
                return uow
                    .Set<DiscordPermOverride>()
                    .AsQueryable()
                    .AsNoTracking()
                    .Where(x => x.GuildId == guildId)
                    .ToListAsync();
            }
        }

        public async Task<bool> TryBlockLate(ICommandContext context, string moduleName, CommandInfo command)
        {
            if (TryGetOverrides(context.Guild?.Id ?? 0, command.Name, out var perm) && perm is not null)
            {
                var result = await new RequireUserPermissionAttribute((GuildPermission) perm)
                    .CheckPermissionsAsync(context, command, _services);

                return !result.IsSuccess;
            }

            return false;
        }
    }
}