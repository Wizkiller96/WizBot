﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using WizBot.Common;
using WizBot.Common.Attributes;
using WizBot.Core.Services.Database.Models;
using WizBot.Extensions;
using WizBot.Modules.Administration.Services;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace WizBot.Modules.Administration
{
    public partial class Administration
    {
        public class RoleCommands : WizBotSubmodule<RoleCommandsService>
        {
            private IServiceProvider _services;
            public enum Exclude { Excl }

            public RoleCommands(IServiceProvider services)
            {
                _services = services;
            }

            public async Task InternalReactionRoles(bool exclusive, params string[] input)
            {
                var msgs = await ((SocketTextChannel)ctx.Channel).GetMessagesAsync().FlattenAsync().ConfigureAwait(false);
                var prev = (IUserMessage)msgs.FirstOrDefault(x => x is IUserMessage && x.Id != ctx.Message.Id);

                if (prev == null)
                    return;

                if (input.Length % 2 != 0)
                    return;

                var g = (SocketGuild)ctx.Guild;

                var grp = 0;
                var results = input
                    .GroupBy(x => grp++ / 2)
                    .Select(async x =>
                    {
                        var inputRoleStr = x.First();
                        var roleReader = new RoleTypeReader<SocketRole>();
                        var roleResult = await roleReader.ReadAsync(ctx, inputRoleStr, _services);
                        if (!roleResult.IsSuccess)
                        {
                            _log.Warn("Role {0} not found.", inputRoleStr);
                            return null;
                        }
                        var role = (IRole)roleResult.BestMatch;
                        if (role.Position > ((IGuildUser)ctx.User).GetRoles().Select(r => r.Position).Max()
                            && ctx.User.Id != ctx.Guild.OwnerId)
                            return null;
                        var emote = x.Last().ToIEmote();
                        return new { role, emote };
                    })
                    .Where(x => x != null);

                var all = await Task.WhenAll(results);

                if (!all.Any())
                    return;

                foreach (var x in all)
                {
                    await prev.AddReactionAsync(x.emote, new RequestOptions()
                    {
                        RetryMode = RetryMode.Retry502 | RetryMode.RetryRatelimit
                    }).ConfigureAwait(false);
                    await Task.Delay(100).ConfigureAwait(false);
                }

                if (_service.Add(ctx.Guild.Id, new ReactionRoleMessage()
                {
                    Exclusive = exclusive,
                    MessageId = prev.Id,
                    ChannelId = prev.Channel.Id,
                    ReactionRoles = all.Select(x =>
                    {
                        return new ReactionRole()
                        {
                            EmoteName = x.emote.ToString(),
                            RoleId = x.role.Id,
                        };
                    }).ToList(),
                }))
                {
                    await ctx.Channel.SendConfirmAsync(":ok:")
                        .ConfigureAwait(false);
                }
                else
                {
                    await ReplyErrorLocalizedAsync("reaction_roles_full").ConfigureAwait(false);
                }
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageRoles)]
            [BotPerm(GuildPerm.ManageRoles)]
            [Priority(0)]
            public Task ReactionRoles(params string[] input) =>
                InternalReactionRoles(false, input);

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageRoles)]
            [BotPerm(GuildPerm.ManageRoles)]
            [Priority(1)]
            public Task ReactionRoles(Exclude _, params string[] input) =>
                InternalReactionRoles(true, input);

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageRoles)]
            public async Task ReactionRolesList()
            {
                var embed = new EmbedBuilder()
                    .WithOkColor();
                if (!_service.Get(ctx.Guild.Id, out var rrs) ||
                    !rrs.Any())
                {
                    embed.WithDescription(GetText("no_reaction_roles"));
                }
                else
                {
                    var g = ((SocketGuild)ctx.Guild);
                    foreach (var rr in rrs)
                    {
                        var ch = g.GetTextChannel(rr.ChannelId);
                        IUserMessage msg = null;
                        if (!(ch is null))
                        {
                            msg = await ch.GetMessageAsync(rr.MessageId).ConfigureAwait(false) as IUserMessage;
                        }
                        var content = msg?.Content.TrimTo(30) ?? "DELETED!";
                        embed.AddField($"**{rr.Index + 1}.** {(ch?.Name ?? "DELETED!")}",
                            GetText("reaction_roles_message", rr.ReactionRoles?.Count ?? 0, content));
                    }
                }
                await ctx.Channel.EmbedAsync(embed).ConfigureAwait(false);
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageRoles)]
            public async Task ReactionRolesRemove(int index)
            {
                if (index < 1 || index > 5 ||
                    !_service.Get(ctx.Guild.Id, out var rrs) ||
                    !rrs.Any() || rrs.Count < index)
                {
                    return;
                }
                index--;
                var rr = rrs[index];
                _service.Remove(ctx.Guild.Id, index);
                await ReplyConfirmLocalizedAsync("reaction_role_removed", index + 1).ConfigureAwait(false);
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageRoles)]
            [BotPerm(GuildPerm.ManageRoles)]
            public async Task SetRole(IGuildUser targetUser, [Leftover] IRole roleToAdd)
            {
                var runnerUser = (IGuildUser)ctx.User;
                var runnerMaxRolePosition = runnerUser.GetRoles().Max(x => x.Position);
                if ((ctx.User.Id != ctx.Guild.OwnerId) && runnerMaxRolePosition <= roleToAdd.Position)
                    return;
                try
                {
                    await targetUser.AddRoleAsync(roleToAdd).ConfigureAwait(false);

                    await ReplyConfirmLocalizedAsync("setrole", Format.Bold(roleToAdd.Name), Format.Bold(targetUser.ToString()))
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    await ReplyErrorLocalizedAsync("setrole_err").ConfigureAwait(false);
                    _log.Info(ex);
                }
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageRoles)]
            [BotPerm(GuildPerm.ManageRoles)]
            public async Task RemoveRole(IGuildUser targetUser, [Leftover] IRole roleToRemove)
            {
                var runnerUser = (IGuildUser)ctx.User;
                if (ctx.User.Id != runnerUser.Guild.OwnerId && runnerUser.GetRoles().Max(x => x.Position) <= roleToRemove.Position)
                    return;
                try
                {
                    await targetUser.RemoveRoleAsync(roleToRemove).ConfigureAwait(false);
                    await ReplyConfirmLocalizedAsync("remrole", Format.Bold(roleToRemove.Name), Format.Bold(targetUser.ToString())).ConfigureAwait(false);
                }
                catch
                {
                    await ReplyErrorLocalizedAsync("remrole_err").ConfigureAwait(false);
                }
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageRoles)]
            [BotPerm(GuildPerm.ManageRoles)]
            public async Task RenameRole(IRole roleToEdit, string newname)
            {
                var guser = (IGuildUser)ctx.User;
                if (ctx.User.Id != guser.Guild.OwnerId && guser.GetRoles().Max(x => x.Position) <= roleToEdit.Position)
                    return;
                try
                {
                    if (roleToEdit.Position > (await ctx.Guild.GetCurrentUserAsync().ConfigureAwait(false)).GetRoles().Max(r => r.Position))
                    {
                        await ReplyErrorLocalizedAsync("renrole_perms").ConfigureAwait(false);
                        return;
                    }
                    await roleToEdit.ModifyAsync(g => g.Name = newname).ConfigureAwait(false);
                    await ReplyConfirmLocalizedAsync("renrole").ConfigureAwait(false);
                }
                catch (Exception)
                {
                    await ReplyErrorLocalizedAsync("renrole_err").ConfigureAwait(false);
                }
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageRoles)]
            [BotPerm(GuildPerm.ManageRoles)]
            public async Task RemoveAllRoles([Leftover] IGuildUser user)
            {
                var guser = (IGuildUser)ctx.User;

                var userRoles = user.GetRoles().Except(new[] { guser.Guild.EveryoneRole });
                if (user.Id == ctx.Guild.OwnerId || (ctx.User.Id != ctx.Guild.OwnerId && guser.GetRoles().Max(x => x.Position) <= userRoles.Max(x => x.Position)))
                    return;
                try
                {
                    await user.RemoveRolesAsync(userRoles).ConfigureAwait(false);
                    await ReplyConfirmLocalizedAsync("rar", Format.Bold(user.ToString())).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    await ReplyErrorLocalizedAsync("rar_err").ConfigureAwait(false);
                }
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageRoles)]
            [BotPerm(GuildPerm.ManageRoles)]
            public async Task CreateRole([Leftover] string roleName = null)
            {
                if (string.IsNullOrWhiteSpace(roleName))
                    return;

                var r = await ctx.Guild.CreateRoleAsync(roleName, isMentionable: false).ConfigureAwait(false);
                await ReplyConfirmLocalizedAsync("cr", Format.Bold(r.Name)).ConfigureAwait(false);
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageRoles)]
            [BotPerm(GuildPerm.ManageRoles)]
            public async Task DeleteRole([Leftover] IRole role)
            {
                var guser = (IGuildUser)ctx.User;
                if (ctx.User.Id != guser.Guild.OwnerId
                    && guser.GetRoles().Max(x => x.Position) <= role.Position)
                    return;

                await role.DeleteAsync().ConfigureAwait(false);
                await ReplyConfirmLocalizedAsync("dr", Format.Bold(role.Name)).ConfigureAwait(false);
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageRoles)]
            [BotPerm(GuildPerm.ManageRoles)]
            public async Task RoleHoist(IRole role)
            {
                var newHoisted = !role.IsHoisted;
                await role.ModifyAsync(r => r.Hoist = newHoisted).ConfigureAwait(false);
                if (newHoisted)
                {
                    await ReplyConfirmLocalizedAsync("rolehoist_enabled", Format.Bold(role.Name)).ConfigureAwait(false);
                }
                else
                {
                    await ReplyConfirmLocalizedAsync("rolehoist_disabled", Format.Bold(role.Name)).ConfigureAwait(false);
                }
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [Priority(1)]
            public async Task RoleColor([Leftover] IRole role)
            {
                await ctx.Channel.SendConfirmAsync("Role Color", role.Color.RawValue.ToString("x6")).ConfigureAwait(false);
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageRoles)]
            [BotPerm(GuildPerm.ManageRoles)]
            [Priority(0)]
            public async Task RoleColor(IRole role, SixLabors.ImageSharp.Color color)
            {
                try
                {
                    var rgba32 = color.ToPixel<Rgba32>();
                    await role.ModifyAsync(r => r.Color = new Color(rgba32.R, rgba32.G, rgba32.B)).ConfigureAwait(false);
                    await ReplyConfirmLocalizedAsync("rc", Format.Bold(role.Name)).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    await ReplyErrorLocalizedAsync("rc_perms").ConfigureAwait(false);
                }
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.MentionEveryone)]
            [BotPerm(GuildPerm.ManageRoles)]
            public async Task MentionRole([Leftover] IRole role)
            {
                if (!role.IsMentionable)
                {
                    await role.ModifyAsync(x => x.Mentionable = true).ConfigureAwait(false);
                    await ctx.Channel.SendMessageAsync(role.Mention).ConfigureAwait(false);
                    await role.ModifyAsync(x => x.Mentionable = false).ConfigureAwait(false);
                }
                else
                {
                    await ctx.Channel.SendMessageAsync(role.Mention).ConfigureAwait(false);
                }
            }
        }
    }
}
