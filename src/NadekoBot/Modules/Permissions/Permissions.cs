#nullable disable
using NadekoBot.Common.TypeReaders;
using NadekoBot.Common.TypeReaders.Models;
using NadekoBot.Db;
using NadekoBot.Modules.Permissions.Common;
using NadekoBot.Modules.Permissions.Services;
using NadekoBot.Services.Database.Models;

namespace NadekoBot.Modules.Permissions;

public partial class Permissions : NadekoModule<PermissionService>
{
    public enum Reset { Reset }

    private readonly DbService _db;

    public Permissions(DbService db)
        => _db = db;

    [Cmd]
    [RequireContext(ContextType.Guild)]
    public async partial Task Verbose(PermissionAction action = null)
    {
        await using (var uow = _db.GetDbContext())
        {
            var config = uow.GcWithPermissionsFor(ctx.Guild.Id);
            if (action is null)
                action = new(!config.VerbosePermissions); // New behaviour, can toggle.
            config.VerbosePermissions = action.Value;
            await uow.SaveChangesAsync();
            _service.UpdateCache(config);
        }

        if (action.Value)
            await ReplyConfirmLocalizedAsync(strs.verbose_true);
        else
            await ReplyConfirmLocalizedAsync(strs.verbose_false);
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    [UserPerm(GuildPerm.Administrator)]
    [Priority(0)]
    public async partial Task PermRole([Leftover] IRole role = null)
    {
        if (role is not null && role == role.Guild.EveryoneRole)
            return;

        if (role is null)
        {
            var cache = _service.GetCacheFor(ctx.Guild.Id);
            if (!ulong.TryParse(cache.PermRole, out var roleId)
                || (role = ((SocketGuild)ctx.Guild).GetRole(roleId)) is null)
                await ReplyConfirmLocalizedAsync(strs.permrole_not_set);
            else
                await ReplyConfirmLocalizedAsync(strs.permrole(Format.Bold(role.ToString())));
            return;
        }

        await using (var uow = _db.GetDbContext())
        {
            var config = uow.GcWithPermissionsFor(ctx.Guild.Id);
            config.PermissionRole = role.Id.ToString();
            uow.SaveChanges();
            _service.UpdateCache(config);
        }

        await ReplyConfirmLocalizedAsync(strs.permrole_changed(Format.Bold(role.Name)));
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    [UserPerm(GuildPerm.Administrator)]
    [Priority(1)]
    public async partial Task PermRole(Reset _)
    {
        await using (var uow = _db.GetDbContext())
        {
            var config = uow.GcWithPermissionsFor(ctx.Guild.Id);
            config.PermissionRole = null;
            await uow.SaveChangesAsync();
            _service.UpdateCache(config);
        }

        await ReplyConfirmLocalizedAsync(strs.permrole_reset);
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    public async partial Task ListPerms(int page = 1)
    {
        if (page < 1)
            return;

        IList<Permissionv2> perms;

        if (_service.Cache.TryGetValue(ctx.Guild.Id, out var permCache))
            perms = permCache.Permissions.Source.ToList();
        else
            perms = Permissionv2.GetDefaultPermlist;

        var startPos = 20 * (page - 1);
        var toSend = Format.Bold(GetText(strs.page(page)))
                     + "\n\n"
                     + string.Join("\n",
                         perms.Reverse()
                              .Skip(startPos)
                              .Take(20)
                              .Select(p =>
                              {
                                  var str =
                                      $"`{p.Index + 1}.` {Format.Bold(p.GetCommand(prefix, (SocketGuild)ctx.Guild))}";
                                  if (p.Index == 0)
                                      str += $" [{GetText(strs.uneditable)}]";
                                  return str;
                              }));

        await ctx.Channel.SendMessageAsync(toSend);
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    public async partial Task RemovePerm(int index)
    {
        index -= 1;
        if (index < 0)
            return;
        try
        {
            Permissionv2 p;
            await using (var uow = _db.GetDbContext())
            {
                var config = uow.GcWithPermissionsFor(ctx.Guild.Id);
                var permsCol = new PermissionsCollection<Permissionv2>(config.Permissions);
                p = permsCol[index];
                permsCol.RemoveAt(index);
                uow.Remove(p);
                await uow.SaveChangesAsync();
                _service.UpdateCache(config);
            }

            await ReplyConfirmLocalizedAsync(strs.removed(index + 1,
                Format.Code(p.GetCommand(prefix, (SocketGuild)ctx.Guild))));
        }
        catch (IndexOutOfRangeException)
        {
            await ReplyErrorLocalizedAsync(strs.perm_out_of_range);
        }
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    public async partial Task MovePerm(int from, int to)
    {
        from -= 1;
        to -= 1;
        if (!(from == to || from < 0 || to < 0))
        {
            try
            {
                Permissionv2 fromPerm;
                await using (var uow = _db.GetDbContext())
                {
                    var config = uow.GcWithPermissionsFor(ctx.Guild.Id);
                    var permsCol = new PermissionsCollection<Permissionv2>(config.Permissions);

                    var fromFound = @from < permsCol.Count;
                    var toFound = to < permsCol.Count;

                    if (!fromFound)
                    {
                        await ReplyErrorLocalizedAsync(strs.perm_not_found(++@from));
                        return;
                    }

                    if (!toFound)
                    {
                        await ReplyErrorLocalizedAsync(strs.perm_not_found(++to));
                        return;
                    }

                    fromPerm = permsCol[@from];

                    permsCol.RemoveAt(@from);
                    permsCol.Insert(to, fromPerm);
                    await uow.SaveChangesAsync();
                    _service.UpdateCache(config);
                }

                await ReplyConfirmLocalizedAsync(strs.moved_permission(
                    Format.Code(fromPerm.GetCommand(prefix, (SocketGuild)ctx.Guild)),
                    ++@from,
                    ++to));

                return;
            }
            catch (Exception e) when (e is ArgumentOutOfRangeException or IndexOutOfRangeException)
            {
            }
        }

        await ReplyConfirmLocalizedAsync(strs.perm_out_of_range);
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    public async partial Task SrvrCmd(CommandOrCrInfo command, PermissionAction action)
    {
        await _service.AddPermissions(ctx.Guild.Id,
            new Permissionv2
            {
                PrimaryTarget = PrimaryPermissionType.Server,
                PrimaryTargetId = 0,
                SecondaryTarget = SecondaryPermissionType.Command,
                SecondaryTargetName = command.Name.ToLowerInvariant(),
                State = action.Value,
                IsCustomCommand = command.IsCustom
            });

        if (action.Value)
            await ReplyConfirmLocalizedAsync(strs.sx_enable(Format.Code(command.Name), GetText(strs.of_command)));
        else
            await ReplyConfirmLocalizedAsync(strs.sx_disable(Format.Code(command.Name), GetText(strs.of_command)));
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    public async partial Task SrvrMdl(ModuleOrCrInfo module, PermissionAction action)
    {
        await _service.AddPermissions(ctx.Guild.Id,
            new Permissionv2
            {
                PrimaryTarget = PrimaryPermissionType.Server,
                PrimaryTargetId = 0,
                SecondaryTarget = SecondaryPermissionType.Module,
                SecondaryTargetName = module.Name.ToLowerInvariant(),
                State = action.Value
            });

        if (action.Value)
            await ReplyConfirmLocalizedAsync(strs.sx_enable(Format.Code(module.Name), GetText(strs.of_module)));
        else
            await ReplyConfirmLocalizedAsync(strs.sx_disable(Format.Code(module.Name), GetText(strs.of_module)));
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    public async partial Task UsrCmd(CommandOrCrInfo command, PermissionAction action, [Leftover] IGuildUser user)
    {
        await _service.AddPermissions(ctx.Guild.Id,
            new Permissionv2
            {
                PrimaryTarget = PrimaryPermissionType.User,
                PrimaryTargetId = user.Id,
                SecondaryTarget = SecondaryPermissionType.Command,
                SecondaryTargetName = command.Name.ToLowerInvariant(),
                State = action.Value,
                IsCustomCommand = command.IsCustom
            });

        if (action.Value)
        {
            await ReplyConfirmLocalizedAsync(strs.ux_enable(Format.Code(command.Name),
                GetText(strs.of_command),
                Format.Code(user.ToString())));
        }
        else
        {
            await ReplyConfirmLocalizedAsync(strs.ux_disable(Format.Code(command.Name),
                GetText(strs.of_command),
                Format.Code(user.ToString())));
        }
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    public async partial Task UsrMdl(ModuleOrCrInfo module, PermissionAction action, [Leftover] IGuildUser user)
    {
        await _service.AddPermissions(ctx.Guild.Id,
            new Permissionv2
            {
                PrimaryTarget = PrimaryPermissionType.User,
                PrimaryTargetId = user.Id,
                SecondaryTarget = SecondaryPermissionType.Module,
                SecondaryTargetName = module.Name.ToLowerInvariant(),
                State = action.Value
            });

        if (action.Value)
        {
            await ReplyConfirmLocalizedAsync(strs.ux_enable(Format.Code(module.Name),
                GetText(strs.of_module),
                Format.Code(user.ToString())));
        }
        else
        {
            await ReplyConfirmLocalizedAsync(strs.ux_disable(Format.Code(module.Name),
                GetText(strs.of_module),
                Format.Code(user.ToString())));
        }
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    public async partial Task RoleCmd(CommandOrCrInfo command, PermissionAction action, [Leftover] IRole role)
    {
        if (role == role.Guild.EveryoneRole)
            return;

        await _service.AddPermissions(ctx.Guild.Id,
            new Permissionv2
            {
                PrimaryTarget = PrimaryPermissionType.Role,
                PrimaryTargetId = role.Id,
                SecondaryTarget = SecondaryPermissionType.Command,
                SecondaryTargetName = command.Name.ToLowerInvariant(),
                State = action.Value,
                IsCustomCommand = command.IsCustom
            });

        if (action.Value)
        {
            await ReplyConfirmLocalizedAsync(strs.rx_enable(Format.Code(command.Name),
                GetText(strs.of_command),
                Format.Code(role.Name)));
        }
        else
        {
            await ReplyConfirmLocalizedAsync(strs.rx_disable(Format.Code(command.Name),
                GetText(strs.of_command),
                Format.Code(role.Name)));
        }
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    public async partial Task RoleMdl(ModuleOrCrInfo module, PermissionAction action, [Leftover] IRole role)
    {
        if (role == role.Guild.EveryoneRole)
            return;

        await _service.AddPermissions(ctx.Guild.Id,
            new Permissionv2
            {
                PrimaryTarget = PrimaryPermissionType.Role,
                PrimaryTargetId = role.Id,
                SecondaryTarget = SecondaryPermissionType.Module,
                SecondaryTargetName = module.Name.ToLowerInvariant(),
                State = action.Value
            });


        if (action.Value)
        {
            await ReplyConfirmLocalizedAsync(strs.rx_enable(Format.Code(module.Name),
                GetText(strs.of_module),
                Format.Code(role.Name)));
        }
        else
        {
            await ReplyConfirmLocalizedAsync(strs.rx_disable(Format.Code(module.Name),
                GetText(strs.of_module),
                Format.Code(role.Name)));
        }
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    public async partial Task ChnlCmd(CommandOrCrInfo command, PermissionAction action, [Leftover] ITextChannel chnl)
    {
        await _service.AddPermissions(ctx.Guild.Id,
            new Permissionv2
            {
                PrimaryTarget = PrimaryPermissionType.Channel,
                PrimaryTargetId = chnl.Id,
                SecondaryTarget = SecondaryPermissionType.Command,
                SecondaryTargetName = command.Name.ToLowerInvariant(),
                State = action.Value,
                IsCustomCommand = command.IsCustom
            });

        if (action.Value)
        {
            await ReplyConfirmLocalizedAsync(strs.cx_enable(Format.Code(command.Name),
                GetText(strs.of_command),
                Format.Code(chnl.Name)));
        }
        else
        {
            await ReplyConfirmLocalizedAsync(strs.cx_disable(Format.Code(command.Name),
                GetText(strs.of_command),
                Format.Code(chnl.Name)));
        }
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    public async partial Task ChnlMdl(ModuleOrCrInfo module, PermissionAction action, [Leftover] ITextChannel chnl)
    {
        await _service.AddPermissions(ctx.Guild.Id,
            new Permissionv2
            {
                PrimaryTarget = PrimaryPermissionType.Channel,
                PrimaryTargetId = chnl.Id,
                SecondaryTarget = SecondaryPermissionType.Module,
                SecondaryTargetName = module.Name.ToLowerInvariant(),
                State = action.Value
            });

        if (action.Value)
        {
            await ReplyConfirmLocalizedAsync(strs.cx_enable(Format.Code(module.Name),
                GetText(strs.of_module),
                Format.Code(chnl.Name)));
        }
        else
        {
            await ReplyConfirmLocalizedAsync(strs.cx_disable(Format.Code(module.Name),
                GetText(strs.of_module),
                Format.Code(chnl.Name)));
        }
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    public async partial Task AllChnlMdls(PermissionAction action, [Leftover] ITextChannel chnl)
    {
        await _service.AddPermissions(ctx.Guild.Id,
            new Permissionv2
            {
                PrimaryTarget = PrimaryPermissionType.Channel,
                PrimaryTargetId = chnl.Id,
                SecondaryTarget = SecondaryPermissionType.AllModules,
                SecondaryTargetName = "*",
                State = action.Value
            });

        if (action.Value)
            await ReplyConfirmLocalizedAsync(strs.acm_enable(Format.Code(chnl.Name)));
        else
            await ReplyConfirmLocalizedAsync(strs.acm_disable(Format.Code(chnl.Name)));
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    public async partial Task AllRoleMdls(PermissionAction action, [Leftover] IRole role)
    {
        if (role == role.Guild.EveryoneRole)
            return;

        await _service.AddPermissions(ctx.Guild.Id,
            new Permissionv2
            {
                PrimaryTarget = PrimaryPermissionType.Role,
                PrimaryTargetId = role.Id,
                SecondaryTarget = SecondaryPermissionType.AllModules,
                SecondaryTargetName = "*",
                State = action.Value
            });

        if (action.Value)
            await ReplyConfirmLocalizedAsync(strs.arm_enable(Format.Code(role.Name)));
        else
            await ReplyConfirmLocalizedAsync(strs.arm_disable(Format.Code(role.Name)));
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    public async partial Task AllUsrMdls(PermissionAction action, [Leftover] IUser user)
    {
        await _service.AddPermissions(ctx.Guild.Id,
            new Permissionv2
            {
                PrimaryTarget = PrimaryPermissionType.User,
                PrimaryTargetId = user.Id,
                SecondaryTarget = SecondaryPermissionType.AllModules,
                SecondaryTargetName = "*",
                State = action.Value
            });

        if (action.Value)
            await ReplyConfirmLocalizedAsync(strs.aum_enable(Format.Code(user.ToString())));
        else
            await ReplyConfirmLocalizedAsync(strs.aum_disable(Format.Code(user.ToString())));
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    public async partial Task AllSrvrMdls(PermissionAction action)
    {
        var newPerm = new Permissionv2
        {
            PrimaryTarget = PrimaryPermissionType.Server,
            PrimaryTargetId = 0,
            SecondaryTarget = SecondaryPermissionType.AllModules,
            SecondaryTargetName = "*",
            State = action.Value
        };

        var allowUser = new Permissionv2
        {
            PrimaryTarget = PrimaryPermissionType.User,
            PrimaryTargetId = ctx.User.Id,
            SecondaryTarget = SecondaryPermissionType.AllModules,
            SecondaryTargetName = "*",
            State = true
        };

        await _service.AddPermissions(ctx.Guild.Id, newPerm, allowUser);

        if (action.Value)
            await ReplyConfirmLocalizedAsync(strs.asm_enable);
        else
            await ReplyConfirmLocalizedAsync(strs.asm_disable);
    }
}