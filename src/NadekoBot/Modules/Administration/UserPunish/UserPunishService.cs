#nullable disable
using LinqToDB;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Common.ModuleBehaviors;
using NadekoBot.Common.TypeReaders.Models;
using NadekoBot.Db;
using NadekoBot.Modules.Permissions.Services;
using NadekoBot.Services.Database.Models;
using Newtonsoft.Json;

namespace NadekoBot.Modules.Administration.Services;

public class UserPunishService : INService, IReadyExecutor
{
    private readonly MuteService _mute;
    private readonly DbService _db;
    private readonly BlacklistService _blacklistService;
    private readonly BotConfigService _bcs;
    private readonly DiscordSocketClient _client;

    public UserPunishService(
        MuteService mute,
        DbService db,
        BlacklistService blacklistService,
        BotConfigService bcs,
        DiscordSocketClient client)
    {
        _mute = mute;
        _db = db;
        _blacklistService = blacklistService;
        _bcs = bcs;
        _client = client;
    }

    public async Task OnReadyAsync()
    {
        if (_client.ShardId != 0)
            return;

        using var expiryTimer = new PeriodicTimer(TimeSpan.FromHours(12));
        do
        {
            try
            {
                await CheckAllWarnExpiresAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error while checking for warn expiries: {ErrorMessage}", ex.Message);
            }
        } while (await expiryTimer.WaitForNextTickAsync());
    }

    public async Task<WarningPunishment> Warn(
        IGuild guild,
        ulong userId,
        IUser mod,
        long weight,
        string reason)
    {
        if (weight <= 0)
            throw new ArgumentOutOfRangeException(nameof(weight));

        var modName = mod.ToString();

        if (string.IsNullOrWhiteSpace(reason))
            reason = "-";

        var guildId = guild.Id;

        var warn = new Warning
        {
            UserId = userId,
            GuildId = guildId,
            Forgiven = false,
            Reason = reason,
            Moderator = modName,
            Weight = weight
        };

        long previousCount;
        List<WarningPunishment> ps;
        await using (var uow = _db.GetDbContext())
        {
            ps = uow.GuildConfigsForId(guildId, set => set.Include(x => x.WarnPunishments)).WarnPunishments;

            previousCount = uow.Warnings.ForId(guildId, userId)
                                .Where(w => !w.Forgiven && w.UserId == userId)
                                .Sum(x => x.Weight);

            uow.Warnings.Add(warn);

            await uow.SaveChangesAsync();
        }

        var totalCount = previousCount + weight;
        
        var p = ps.Where(x => x.Count > previousCount && x.Count <= totalCount)
                  .MaxBy(x => x.Count);

        if (p is not null)
        {
            var user = await guild.GetUserAsync(userId);
            if (user is null)
                return null;

            await ApplyPunishment(guild, user, mod, p.Punishment, p.Time, p.RoleId, "Warned too many times.");
            return p;
        }

        return null;
    }

    public async Task ApplyPunishment(
        IGuild guild,
        IGuildUser user,
        IUser mod,
        PunishmentAction p,
        int minutes,
        ulong? roleId,
        string reason)
    {
        if (!await CheckPermission(guild, p))
            return;

        switch (p)
        {
            case PunishmentAction.Mute:
                if (minutes == 0)
                    await _mute.MuteUser(user, mod, reason: reason);
                else
                    await _mute.TimedMute(user, mod, TimeSpan.FromMinutes(minutes), reason: reason);
                break;
            case PunishmentAction.VoiceMute:
                if (minutes == 0)
                    await _mute.MuteUser(user, mod, MuteType.Voice, reason);
                else
                    await _mute.TimedMute(user, mod, TimeSpan.FromMinutes(minutes), MuteType.Voice, reason);
                break;
            case PunishmentAction.ChatMute:
                if (minutes == 0)
                    await _mute.MuteUser(user, mod, MuteType.Chat, reason);
                else
                    await _mute.TimedMute(user, mod, TimeSpan.FromMinutes(minutes), MuteType.Chat, reason);
                break;
            case PunishmentAction.Kick:
                await user.KickAsync(reason);
                break;
            case PunishmentAction.Ban:
                if (minutes == 0)
                    await guild.AddBanAsync(user, reason: reason, pruneDays: 7);
                else
                    await _mute.TimedBan(user.Guild, user, TimeSpan.FromMinutes(minutes), reason);
                break;
            case PunishmentAction.Softban:
                await guild.AddBanAsync(user, 7, $"Softban | {reason}");
                try
                {
                    await guild.RemoveBanAsync(user);
                }
                catch
                {
                    await guild.RemoveBanAsync(user);
                }

                break;
            case PunishmentAction.RemoveRoles:
                await user.RemoveRolesAsync(user.GetRoles().Where(x => !x.IsManaged && x != x.Guild.EveryoneRole));
                break;
            case PunishmentAction.AddRole:
                if (roleId is null)
                    return;
                var role = guild.GetRole(roleId.Value);
                if (role is not null)
                {
                    if (minutes == 0)
                        await user.AddRoleAsync(role);
                    else
                        await _mute.TimedRole(user, TimeSpan.FromMinutes(minutes), reason, role);
                }
                else
                {
                    Log.Warning("Can't find role {RoleId} on server {GuildId} to apply punishment",
                        roleId.Value,
                        guild.Id);
                }

                break;
        }
    }

    /// <summary>
    ///     Used to prevent the bot from hitting 403's when it needs to
    ///     apply punishments with insufficient permissions
    /// </summary>
    /// <param name="guild">Guild the punishment is applied in</param>
    /// <param name="punish">Punishment to apply</param>
    /// <returns>Whether the bot has sufficient permissions</returns>
    private async Task<bool> CheckPermission(IGuild guild, PunishmentAction punish)
    {
        var botUser = await guild.GetCurrentUserAsync();
        switch (punish)
        {
            case PunishmentAction.Mute:
                return botUser.GuildPermissions.MuteMembers && botUser.GuildPermissions.ManageRoles;
            case PunishmentAction.Kick:
                return botUser.GuildPermissions.KickMembers;
            case PunishmentAction.Ban:
                return botUser.GuildPermissions.BanMembers;
            case PunishmentAction.Softban:
                return botUser.GuildPermissions.BanMembers; // ban + unban
            case PunishmentAction.RemoveRoles:
                return botUser.GuildPermissions.ManageRoles;
            case PunishmentAction.ChatMute:
                return botUser.GuildPermissions.ManageRoles; // adds nadeko-mute role
            case PunishmentAction.VoiceMute:
                return botUser.GuildPermissions.MuteMembers;
            case PunishmentAction.AddRole:
                return botUser.GuildPermissions.ManageRoles;
            default:
                return true;
        }
    }

    public async Task CheckAllWarnExpiresAsync()
    {
        await using var uow = _db.GetDbContext();
        var cleared = await uow.Warnings
                               .Where(x => uow.GuildConfigs
                                                 .Any(y => y.GuildId == x.GuildId
                                                           && y.WarnExpireHours > 0
                                                           && y.WarnExpireAction == WarnExpireAction.Clear)
                                           && x.Forgiven == false 
                                           && x.DateAdded
                                           < DateTime.UtcNow.AddHours(-uow.GuildConfigs
                                                                          .Where(y => x.GuildId == y.GuildId)
                                                                          .Select(y => y.WarnExpireHours)
                                                                          .First()))
                               .UpdateAsync(_ => new()
                               {
                                   Forgiven = true,
                                   ForgivenBy = "expiry"
                               });

        var deleted = await uow.Warnings
                               .Where(x => uow.GuildConfigs
                                              .Any(y => y.GuildId == x.GuildId
                                                        && y.WarnExpireHours > 0
                                                        && y.WarnExpireAction == WarnExpireAction.Delete)
                                           && x.DateAdded
                                           < DateTime.UtcNow.AddHours(-uow.GuildConfigs
                                                                          .Where(y => x.GuildId == y.GuildId)
                                                                          .Select(y => y.WarnExpireHours)
                                                                          .First()))
                               .DeleteAsync();

        if (cleared > 0 || deleted > 0)
        {
            Log.Information("Cleared {ClearedWarnings} warnings and deleted {DeletedWarnings} warnings due to expiry",
                cleared,
                deleted);
        }

        await uow.SaveChangesAsync();
    }

    public async Task CheckWarnExpiresAsync(ulong guildId)
    {
        await using var uow = _db.GetDbContext();
        var config = uow.GuildConfigsForId(guildId, inc => inc);

        if (config.WarnExpireHours == 0)
            return;

        if (config.WarnExpireAction == WarnExpireAction.Clear)
        {
            await uow.Warnings
                     .Where(x => x.GuildId == guildId
                                 && x.Forgiven == false
                                 && x.DateAdded < DateTime.UtcNow.AddHours(-config.WarnExpireHours))
                     .UpdateAsync(_ => new()
                     {
                         Forgiven = true,
                         ForgivenBy = "expiry"
                     });
        }
        else if (config.WarnExpireAction == WarnExpireAction.Delete)
        {
            await uow.Warnings
                     .Where(x => x.GuildId == guildId
                                 && x.DateAdded < DateTime.UtcNow.AddHours(-config.WarnExpireHours))
                     .DeleteAsync();
        }

        await uow.SaveChangesAsync();
    }

    public Task<int> GetWarnExpire(ulong guildId)
    {
        using var uow = _db.GetDbContext();
        var config = uow.GuildConfigsForId(guildId, set => set);
        return Task.FromResult(config.WarnExpireHours / 24);
    }

    public async Task WarnExpireAsync(ulong guildId, int days, bool delete)
    {
        await using (var uow = _db.GetDbContext())
        {
            var config = uow.GuildConfigsForId(guildId, inc => inc);

            config.WarnExpireHours = days * 24;
            config.WarnExpireAction = delete ? WarnExpireAction.Delete : WarnExpireAction.Clear;
            await uow.SaveChangesAsync();

            // no need to check for warn expires
            if (config.WarnExpireHours == 0)
                return;
        }

        await CheckWarnExpiresAsync(guildId);
    }

    public IGrouping<ulong, Warning>[] WarnlogAll(ulong gid)
    {
        using var uow = _db.GetDbContext();
        return uow.Warnings.GetForGuild(gid).GroupBy(x => x.UserId).ToArray();
    }

    public Warning[] UserWarnings(ulong gid, ulong userId)
    {
        using var uow = _db.GetDbContext();
        return uow.Warnings.ForId(gid, userId);
    }

    public async Task<bool> WarnClearAsync(
        ulong guildId,
        ulong userId,
        int index,
        string moderator)
    {
        var toReturn = true;
        await using var uow = _db.GetDbContext();
        if (index == 0)
            await uow.Warnings.ForgiveAll(guildId, userId, moderator);
        else
            toReturn = uow.Warnings.Forgive(guildId, userId, moderator, index - 1);
        uow.SaveChanges();
        return toReturn;
    }

    public bool WarnPunish(
        ulong guildId,
        int number,
        PunishmentAction punish,
        StoopidTime time,
        IRole role = null)
    {
        // these 3 don't make sense with time
        if (punish is PunishmentAction.Softban or PunishmentAction.Kick or PunishmentAction.RemoveRoles
            && time is not null)
            return false;
        if (number <= 0 || (time is not null && time.Time > TimeSpan.FromDays(49)))
            return false;

        if (punish is PunishmentAction.AddRole && role is null)
            return false;

        using var uow = _db.GetDbContext();
        var ps = uow.GuildConfigsForId(guildId, set => set.Include(x => x.WarnPunishments)).WarnPunishments;
        var toDelete = ps.Where(x => x.Count == number);

        uow.RemoveRange(toDelete);

        ps.Add(new()
        {
            Count = number,
            Punishment = punish,
            Time = (int?)time?.Time.TotalMinutes ?? 0,
            RoleId = punish == PunishmentAction.AddRole ? role!.Id : default(ulong?)
        });
        uow.SaveChanges();
        return true;
    }

    public bool WarnPunishRemove(ulong guildId, int number)
    {
        if (number <= 0)
            return false;

        using var uow = _db.GetDbContext();
        var ps = uow.GuildConfigsForId(guildId, set => set.Include(x => x.WarnPunishments)).WarnPunishments;
        var p = ps.FirstOrDefault(x => x.Count == number);

        if (p is not null)
        {
            uow.Remove(p);
            uow.SaveChanges();
        }

        return true;
    }

    public WarningPunishment[] WarnPunishList(ulong guildId)
    {
        using var uow = _db.GetDbContext();
        return uow.GuildConfigsForId(guildId, gc => gc.Include(x => x.WarnPunishments))
                  .WarnPunishments.OrderBy(x => x.Count)
                  .ToArray();
    }

    public (IReadOnlyCollection<(string Original, ulong? Id, string Reason)> Bans, int Missing) MassKill(
        SocketGuild guild,
        string people)
    {
        var gusers = guild.Users;
        //get user objects and reasons
        var bans = people.Split("\n")
                         .Select(x =>
                         {
                             var split = x.Trim().Split(" ");

                             var reason = string.Join(" ", split.Skip(1));

                             if (ulong.TryParse(split[0], out var id))
                                 return (Original: split[0], Id: id, Reason: reason);

                             return (Original: split[0],
                                 gusers.FirstOrDefault(u => u.ToString().ToLowerInvariant() == x)?.Id,
                                 Reason: reason);
                         })
                         .ToArray();

        //if user is null, means that person couldn't be found
        var missing = bans.Count(x => !x.Id.HasValue);

        //get only data for found users
        var found = bans.Where(x => x.Id.HasValue).Select(x => x.Id.Value).ToList();

        _blacklistService.BlacklistUsers(found);

        return (bans, missing);
    }

    public string GetBanTemplate(ulong guildId)
    {
        using var uow = _db.GetDbContext();
        var template = uow.BanTemplates.AsQueryable().FirstOrDefault(x => x.GuildId == guildId);
        return template?.Text;
    }

    public void SetBanTemplate(ulong guildId, string text)
    {
        using var uow = _db.GetDbContext();
        var template = uow.BanTemplates.AsQueryable().FirstOrDefault(x => x.GuildId == guildId);

        if (text is null)
        {
            if (template is null)
                return;

            uow.Remove(template);
        }
        else if (template is null)
        {
            uow.BanTemplates.Add(new()
            {
                GuildId = guildId,
                Text = text
            });
        }
        else
            template.Text = text;

        uow.SaveChanges();
    }

    public SmartText GetBanUserDmEmbed(
        ICommandContext context,
        IGuildUser target,
        string defaultMessage,
        string banReason,
        TimeSpan? duration)
        => GetBanUserDmEmbed((DiscordSocketClient)context.Client,
            (SocketGuild)context.Guild,
            (IGuildUser)context.User,
            target,
            defaultMessage,
            banReason,
            duration);

    public SmartText GetBanUserDmEmbed(
        DiscordSocketClient client,
        SocketGuild guild,
        IGuildUser moderator,
        IGuildUser target,
        string defaultMessage,
        string banReason,
        TimeSpan? duration)
    {
        var template = GetBanTemplate(guild.Id);

        banReason = string.IsNullOrWhiteSpace(banReason) ? "-" : banReason;

        var replacer = new ReplacementBuilder().WithServer(client, guild)
                                               .WithOverride("%ban.mod%", () => moderator.ToString())
                                               .WithOverride("%ban.mod.fullname%", () => moderator.ToString())
                                               .WithOverride("%ban.mod.name%", () => moderator.Username)
                                               .WithOverride("%ban.mod.discrim%", () => moderator.Discriminator)
                                               .WithOverride("%ban.user%", () => target.ToString())
                                               .WithOverride("%ban.user.fullname%", () => target.ToString())
                                               .WithOverride("%ban.user.name%", () => target.Username)
                                               .WithOverride("%ban.user.discrim%", () => target.Discriminator)
                                               .WithOverride("%reason%", () => banReason)
                                               .WithOverride("%ban.reason%", () => banReason)
                                               .WithOverride("%ban.duration%",
                                                   () => duration?.ToString(@"d\.hh\:mm") ?? "perma")
                                               .Build();

        // if template isn't set, use the old message style
        if (string.IsNullOrWhiteSpace(template))
        {
            template = JsonConvert.SerializeObject(new
            {
                color = _bcs.Data.Color.Error.PackedValue >> 8,
                description = defaultMessage
            });
        }
        // if template is set to "-" do not dm the user
        else if (template == "-")
            return default;
        // if template is an embed, send that embed with replacements
        // otherwise, treat template as a regular string with replacements
        else if (SmartText.CreateFrom(template) is not { IsEmbed: true } or { IsEmbedArray: true })
        {
            template = JsonConvert.SerializeObject(new
            {
                color = _bcs.Data.Color.Error.PackedValue >> 8,
                description = template
            });
        }

        var output = SmartText.CreateFrom(template);
        return replacer.Replace(output);
    }
}