﻿using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using WizBot.Services;
using System.Collections.Generic;
using System.Threading.Channels;
using LinqToDB;
using Microsoft.EntityFrameworkCore;
using WizBot.Services.Database.Models;
using WizBot.Db;
using WizBot.Extensions;
using Serilog;

namespace WizBot.Modules.Administration.Services
{
    public sealed class AutoAssignRoleService : INService
    {
        private readonly DiscordSocketClient _client;
        private readonly DbService _db;

        //guildid/roleid
        private readonly ConcurrentDictionary<ulong, IReadOnlyList<ulong>> _autoAssignableRoles;

        private Channel<SocketGuildUser> _assignQueue = Channel.CreateBounded<SocketGuildUser>(
            new BoundedChannelOptions(100)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false,
            });

        public AutoAssignRoleService(DiscordSocketClient client, Bot bot, DbService db)
        {
            _client = client;
            _db = db;

            _autoAssignableRoles = bot.AllGuildConfigs
                    .Where(x => !string.IsNullOrWhiteSpace(x.AutoAssignRoleIds))
                    .ToDictionary<GuildConfig, ulong, IReadOnlyList<ulong>>(k => k.GuildId, v => v.GetAutoAssignableRoles())
                    .ToConcurrent();

            _ = Task.Run(async () =>
            {
                while (true)
                {
                    var user = await _assignQueue.Reader.ReadAsync();
                    if (!_autoAssignableRoles.TryGetValue(user.Guild.Id, out var savedRoleIds))
                        continue;
                    
                    try
                    {
                        var roleIds = savedRoleIds
                            .Select(roleId => user.Guild.GetRole(roleId))
                            .Where(x => x is not null)
                            .ToList();
                        
                        if (roleIds.Any())
                        {
                            await user.AddRolesAsync(roleIds).ConfigureAwait(false);
                            await Task.Delay(250).ConfigureAwait(false);
                        }
                        else
                        {
                            Log.Warning(
                                "Disabled 'Auto assign role' feature on {GuildName} [{GuildId}] server the roles dont exist",
                                user.Guild.Name,
                                user.Guild.Id);
                            
                            await DisableAarAsync(user.Guild.Id);
                        }
                    }
                    catch (Discord.Net.HttpException ex) when (ex.HttpCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        Log.Warning("Disabled 'Auto assign role' feature on {GuildName} [{GuildId}] server because I don't have role management permissions",
                            user.Guild.Name,
                            user.Guild.Id);
                        
                        await DisableAarAsync(user.Guild.Id);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "Error in aar. Probably one of the roles doesn't exist");
                    }
                }
            });

            _client.UserJoined += OnClientOnUserJoined;
            _client.RoleDeleted += OnClientRoleDeleted;
        }

        private async Task OnClientRoleDeleted(SocketRole role)
        {
            if (_autoAssignableRoles.TryGetValue(role.Guild.Id, out var roles)
                && roles.Contains(role.Id))
            {
                await ToggleAarAsync(role.Guild.Id, role.Id);
            }
        }

        private async Task OnClientOnUserJoined(SocketGuildUser user)
        {
            if (_autoAssignableRoles.TryGetValue(user.Guild.Id, out _)) 
                await _assignQueue.Writer.WriteAsync(user);
        }

        public async Task<IReadOnlyList<ulong>> ToggleAarAsync(ulong guildId, ulong roleId)
        {
            using var uow = _db.GetDbContext();
            var gc = uow.GuildConfigsForId(guildId, set => set);
            var roles = gc.GetAutoAssignableRoles();
            if(!roles.Remove(roleId) && roles.Count < 3)
                roles.Add(roleId);
                
            gc.SetAutoAssignableRoles(roles);
            await uow.SaveChangesAsync();
            
            if (roles.Count > 0)
                _autoAssignableRoles[guildId] = roles;
            else
                _autoAssignableRoles.TryRemove(guildId, out _);
            
            return roles;
        }

        public async Task DisableAarAsync(ulong guildId)
        {
            using var uow = _db.GetDbContext();
            
            await uow
                .GuildConfigs
                .AsNoTracking()
                .Where(x => x.GuildId == guildId)
                .UpdateAsync(_ => new GuildConfig(){ AutoAssignRoleIds = null});
            
            _autoAssignableRoles.TryRemove(guildId, out _);
            
            await uow.SaveChangesAsync();
        }

        public async Task SetAarRolesAsync(ulong guildId, IEnumerable<ulong> newRoles)
        {
            using var uow = _db.GetDbContext();
            
            var gc = uow.GuildConfigsForId(guildId, set => set);
            gc.SetAutoAssignableRoles(newRoles);
            
            await uow.SaveChangesAsync();
        }

        public bool TryGetRoles(ulong guildId, out IReadOnlyList<ulong> roles)
            => _autoAssignableRoles.TryGetValue(guildId, out roles);
    }

    public static class GuildConfigExtensions
    {
        public static List<ulong> GetAutoAssignableRoles(this GuildConfig gc)
        {
            if (string.IsNullOrWhiteSpace(gc.AutoAssignRoleIds))
                return new List<ulong>();

            return gc.AutoAssignRoleIds.Split(',').Select(ulong.Parse).ToList();
        }

        public static void SetAutoAssignableRoles(this GuildConfig gc, IEnumerable<ulong> roles)
        {
            gc.AutoAssignRoleIds = roles.JoinWith(',');
        }
    }
}
