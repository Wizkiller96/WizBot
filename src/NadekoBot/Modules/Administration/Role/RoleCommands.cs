#nullable disable
using NadekoBot.Modules.Administration.Services;
using NadekoBot.Services.Database.Models;
using SixLabors.ImageSharp.PixelFormats;
using System.Net;
using Color = SixLabors.ImageSharp.Color;

namespace NadekoBot.Modules.Administration;


public partial class Administration
{
    public partial class RoleCommands : NadekoModule
    {
        public enum Exclude { Excl }

        private readonly IServiceProvider _services;

        public RoleCommands(IServiceProvider services)
        {
            _services = services;
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageRoles)]
        [BotPerm(GuildPerm.ManageRoles)]
        public async partial Task SetRole(IGuildUser targetUser, [Leftover] IRole roleToAdd)
        {
            var runnerUser = (IGuildUser)ctx.User;
            var runnerMaxRolePosition = runnerUser.GetRoles().Max(x => x.Position);
            if (ctx.User.Id != ctx.Guild.OwnerId && runnerMaxRolePosition <= roleToAdd.Position)
                return;
            try
            {
                await targetUser.AddRoleAsync(roleToAdd);

                await ReplyConfirmLocalizedAsync(strs.setrole(Format.Bold(roleToAdd.Name),
                    Format.Bold(targetUser.ToString())));
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error in setrole command");
                await ReplyErrorLocalizedAsync(strs.setrole_err);
            }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageRoles)]
        [BotPerm(GuildPerm.ManageRoles)]
        public async partial Task RemoveRole(IGuildUser targetUser, [Leftover] IRole roleToRemove)
        {
            var runnerUser = (IGuildUser)ctx.User;
            if (ctx.User.Id != runnerUser.Guild.OwnerId
                && runnerUser.GetRoles().Max(x => x.Position) <= roleToRemove.Position)
                return;
            try
            {
                await targetUser.RemoveRoleAsync(roleToRemove);
                await ReplyConfirmLocalizedAsync(strs.remrole(Format.Bold(roleToRemove.Name),
                    Format.Bold(targetUser.ToString())));
            }
            catch
            {
                await ReplyErrorLocalizedAsync(strs.remrole_err);
            }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageRoles)]
        [BotPerm(GuildPerm.ManageRoles)]
        public async partial Task RenameRole(IRole roleToEdit, [Leftover] string newname)
        {
            var guser = (IGuildUser)ctx.User;
            if (ctx.User.Id != guser.Guild.OwnerId && guser.GetRoles().Max(x => x.Position) <= roleToEdit.Position)
                return;
            try
            {
                if (roleToEdit.Position > (await ctx.Guild.GetCurrentUserAsync()).GetRoles().Max(r => r.Position))
                {
                    await ReplyErrorLocalizedAsync(strs.renrole_perms);
                    return;
                }

                await roleToEdit.ModifyAsync(g => g.Name = newname);
                await ReplyConfirmLocalizedAsync(strs.renrole);
            }
            catch (Exception)
            {
                await ReplyErrorLocalizedAsync(strs.renrole_err);
            }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageRoles)]
        [BotPerm(GuildPerm.ManageRoles)]
        public async partial Task RemoveAllRoles([Leftover] IGuildUser user)
        {
            var guser = (IGuildUser)ctx.User;

            var userRoles = user.GetRoles().Where(x => !x.IsManaged && x != x.Guild.EveryoneRole).ToList();

            if (user.Id == ctx.Guild.OwnerId
                || (ctx.User.Id != ctx.Guild.OwnerId
                    && guser.GetRoles().Max(x => x.Position) <= userRoles.Max(x => x.Position)))
                return;
            try
            {
                await user.RemoveRolesAsync(userRoles);
                await ReplyConfirmLocalizedAsync(strs.rar(Format.Bold(user.ToString())));
            }
            catch (Exception)
            {
                await ReplyErrorLocalizedAsync(strs.rar_err);
            }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageRoles)]
        [BotPerm(GuildPerm.ManageRoles)]
        public async partial Task CreateRole([Leftover] string roleName = null)
        {
            if (string.IsNullOrWhiteSpace(roleName))
                return;

            var r = await ctx.Guild.CreateRoleAsync(roleName, isMentionable: false);
            await ReplyConfirmLocalizedAsync(strs.cr(Format.Bold(r.Name)));
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageRoles)]
        [BotPerm(GuildPerm.ManageRoles)]
        public async partial Task DeleteRole([Leftover] IRole role)
        {
            var guser = (IGuildUser)ctx.User;
            if (ctx.User.Id != guser.Guild.OwnerId && guser.GetRoles().Max(x => x.Position) <= role.Position)
                return;

            await role.DeleteAsync();
            await ReplyConfirmLocalizedAsync(strs.dr(Format.Bold(role.Name)));
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageRoles)]
        [BotPerm(GuildPerm.ManageRoles)]
        public async partial Task RoleHoist(IRole role)
        {
            var newHoisted = !role.IsHoisted;
            await role.ModifyAsync(r => r.Hoist = newHoisted);
            if (newHoisted)
                await ReplyConfirmLocalizedAsync(strs.rolehoist_enabled(Format.Bold(role.Name)));
            else
                await ReplyConfirmLocalizedAsync(strs.rolehoist_disabled(Format.Bold(role.Name)));
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [Priority(1)]
        public async partial Task RoleColor([Leftover] IRole role)
            => await SendConfirmAsync("Role Color", role.Color.RawValue.ToString("x6"));

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageRoles)]
        [BotPerm(GuildPerm.ManageRoles)]
        [Priority(0)]
        public async partial Task RoleColor(Color color, [Leftover] IRole role)
        {
            try
            {
                var rgba32 = color.ToPixel<Rgba32>();
                await role.ModifyAsync(r => r.Color = new Discord.Color(rgba32.R, rgba32.G, rgba32.B));
                await ReplyConfirmLocalizedAsync(strs.rc(Format.Bold(role.Name)));
            }
            catch (Exception)
            {
                await ReplyErrorLocalizedAsync(strs.rc_perms);
            }
        }
    }
}