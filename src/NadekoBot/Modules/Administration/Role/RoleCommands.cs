#nullable disable
using NadekoBot.Modules.Administration.Services;
using NadekoBot.Services.Database.Models;
using SixLabors.ImageSharp.PixelFormats;
using System.Net;
using Color = SixLabors.ImageSharp.Color;

namespace NadekoBot.Modules.Administration;

public partial class Administration
{
    public partial class RoleCommands : NadekoModule<RoleCommandsService>
    {
        public enum Exclude { Excl }

        private readonly IServiceProvider _services;

        public RoleCommands(IServiceProvider services)
            => _services = services;

        public async Task InternalReactionRoles(bool exclusive, ulong? messageId, params string[] input)
        {
            var target = messageId is { } msgId
                ? await ctx.Channel.GetMessageAsync(msgId)
                : (await ctx.Channel.GetMessagesAsync(2).FlattenAsync()).Skip(1).FirstOrDefault();

            if (input.Length % 2 != 0 || target is null)
                return;

            var all = await input.Chunk(2)
                                 .Select(async x =>
                                 {
                                     var inputRoleStr = x.First();
                                     var roleReader = new RoleTypeReader<SocketRole>();
                                     var roleResult = await roleReader.ReadAsync(ctx, inputRoleStr, _services);
                                     if (!roleResult.IsSuccess)
                                     {
                                         Log.Warning("Role {Role} not found", inputRoleStr);
                                         return null;
                                     }

                                     var role = (IRole)roleResult.BestMatch;
                                     if (role.Position
                                         > ((IGuildUser)ctx.User).GetRoles()
                                                                 .Select(r => r.Position)
                                                                 .Max()
                                         && ctx.User.Id != ctx.Guild.OwnerId)
                                         return null;
                                     var emote = x.Last().ToIEmote();
                                     return new
                                     {
                                         role,
                                         emote
                                     };
                                 })
                                 .Where(x => x is not null)
                                 .WhenAll();

            if (!all.Any())
                return;

            foreach (var x in all)
            {
                try
                {
                    await target.AddReactionAsync(x.emote,
                        new()
                        {
                            RetryMode = RetryMode.Retry502 | RetryMode.RetryRatelimit
                        });
                }
                catch (HttpException ex) when (ex.HttpCode == HttpStatusCode.BadRequest)
                {
                    await ReplyErrorLocalizedAsync(strs.reaction_cant_access(Format.Code(x.emote.ToString())));
                    return;
                }

                await Task.Delay(500);
            }

            if (_service.Add(ctx.Guild.Id,
                    new()
                    {
                        Exclusive = exclusive,
                        MessageId = target.Id,
                        ChannelId = target.Channel.Id,
                        ReactionRoles = all.Select(x =>
                                           {
                                               return new ReactionRole
                                               {
                                                   EmoteName = x.emote.ToString(),
                                                   RoleId = x.role.Id
                                               };
                                           })
                                           .ToList()
                    }))
                await ctx.OkAsync();
            else
                await ReplyErrorLocalizedAsync(strs.reaction_roles_full);
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [NoPublicBot]
        [UserPerm(GuildPerm.ManageRoles)]
        [BotPerm(GuildPerm.ManageRoles)]
        [Priority(0)]
        public partial Task ReactionRoles(ulong messageId, params string[] input)
            => InternalReactionRoles(false, messageId, input);

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [NoPublicBot]
        [UserPerm(GuildPerm.ManageRoles)]
        [BotPerm(GuildPerm.ManageRoles)]
        [Priority(1)]
        public partial Task ReactionRoles(ulong messageId, Exclude _, params string[] input)
            => InternalReactionRoles(true, messageId, input);

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [NoPublicBot]
        [UserPerm(GuildPerm.ManageRoles)]
        [BotPerm(GuildPerm.ManageRoles)]
        [Priority(0)]
        public partial Task ReactionRoles(params string[] input)
            => InternalReactionRoles(false, null, input);

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [NoPublicBot]
        [UserPerm(GuildPerm.ManageRoles)]
        [BotPerm(GuildPerm.ManageRoles)]
        [Priority(1)]
        public partial Task ReactionRoles(Exclude _, params string[] input)
            => InternalReactionRoles(true, null, input);

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [NoPublicBot]
        [UserPerm(GuildPerm.ManageRoles)]
        public async partial Task ReactionRolesList()
        {
            var embed = _eb.Create().WithOkColor();
            if (!_service.Get(ctx.Guild.Id, out var rrs) || !rrs.Any())
                embed.WithDescription(GetText(strs.no_reaction_roles));
            else
            {
                var g = (SocketGuild)ctx.Guild;
                foreach (var rr in rrs)
                {
                    var ch = g.GetTextChannel(rr.ChannelId);
                    IUserMessage msg = null;
                    if (ch is not null)
                        msg = await ch.GetMessageAsync(rr.MessageId) as IUserMessage;
                    var content = msg?.Content.TrimTo(30) ?? "DELETED!";
                    embed.AddField($"**{rr.Index + 1}.** {ch?.Name ?? "DELETED!"}",
                        GetText(strs.reaction_roles_message(rr.ReactionRoles?.Count ?? 0, content)));
                }
            }

            await ctx.Channel.EmbedAsync(embed);
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [NoPublicBot]
        [UserPerm(GuildPerm.ManageRoles)]
        public async partial Task ReactionRolesRemove(int index)
        {
            if (index < 1 || !_service.Get(ctx.Guild.Id, out var rrs) || !rrs.Any() || rrs.Count < index)
                return;
            index--;
            _service.Remove(ctx.Guild.Id, index);
            await ReplyConfirmLocalizedAsync(strs.reaction_role_removed(index + 1));
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