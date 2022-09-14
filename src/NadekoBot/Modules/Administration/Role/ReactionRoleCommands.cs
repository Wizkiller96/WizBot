using NadekoBot.Modules.Administration.Services;

namespace NadekoBot.Modules.Administration;

public partial class Administration
{
    public partial class ReactionRoleCommands : NadekoModule
    {
        private readonly IReactionRoleService _rero;

        public ReactionRoleCommands(IReactionRoleService rero)
        {
            _rero = rero;
        }
        
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageRoles)]
        [BotPerm(GuildPerm.ManageRoles)]
        public async Task ReactionRoleAdd(
            ulong messageId,
            string emoteStr,
            IRole role,
            int group = 0,
            int levelReq = 0)
        {
            if (group < 0)
                return;

            if (levelReq < 0)
                return;
            
            var msg = await ctx.Channel.GetMessageAsync(messageId);
            if (msg is null)
            {
                await ReplyErrorLocalizedAsync(strs.not_found);
                return;
            }
            
            if (ctx.User.Id != ctx.Guild.OwnerId && ((IGuildUser)ctx.User).GetRoles().Max(x => x.Position) <= role.Position)
            {
                await ReplyErrorLocalizedAsync(strs.hierarchy);
                return;
            }

            var emote = emoteStr.ToIEmote();
            await msg.AddReactionAsync(emote);
            var res = await _rero.AddReactionRole(ctx.Guild,
                msg,
                emoteStr,
                role,
                group,
                levelReq);

            await res.Match(
                _ => ctx.OkAsync(),
                fl =>
                {
                    _ = msg.RemoveReactionAsync(emote, ctx.Client.CurrentUser);
                    return !fl.IsPatronLimit
                        ? ReplyErrorLocalizedAsync(strs.limit_reached(fl.Quota))
                        : ReplyPendingLocalizedAsync(strs.feature_limit_reached_owner(fl.Quota, fl.Name));
                });
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageRoles)]
        [BotPerm(GuildPerm.ManageRoles)]
        public async Task ReactionRolesList(int page = 1)
        {
            if (--page < 0)
                return;
            
            var reros = await _rero.GetReactionRolesAsync(ctx.Guild.Id);

            await ctx.SendPaginatedConfirmAsync(page, curPage =>
            {
                var embed = _eb.Create(ctx)
                               .WithOkColor();

                var content = string.Empty;
                foreach (var g in reros.OrderBy(x => x.Group)
                                       .Skip(curPage * 10)
                                       .GroupBy(x => x.MessageId)
                                       .OrderBy(x => x.Key))
                {
                    var messageId = g.Key;
                    content +=
                        $"[{messageId}](https://discord.com/channels/{ctx.Guild.Id}/{g.First().ChannelId}/{g.Key})\n";

                    var groupGroups = g.GroupBy(x => x.Group);

                    foreach (var ggs in groupGroups)
                    {
                        content += $"`< {(g.Key == 0 ? ("Not Exclusive (Group 0)") : ($"Group {ggs.Key}"))} >`\n";

                        foreach (var rero in ggs)
                        {
                            content +=
                                $"\t{rero.Emote} -> {(ctx.Guild.GetRole(rero.RoleId)?.Mention ?? "<missing role>")}";
                            if (rero.LevelReq > 0)
                                content += $" (lvl {rero.LevelReq}+)";
                            content += '\n';
                        }
                    }

                }

                embed.WithDescription(string.IsNullOrWhiteSpace(content)
                    ? "There are no reaction roles on this server"
                    : content);

                return embed;
            }, reros.Count, 10);
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageRoles)]
        [BotPerm(GuildPerm.ManageRoles)]
        public async Task ReactionRolesRemove(ulong messageId)
        {
            var succ = await _rero.RemoveReactionRoles(ctx.Guild.Id, messageId);
            if (succ)
                await ctx.OkAsync();
            else
                await ctx.ErrorAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageRoles)]
        [BotPerm(GuildPerm.ManageRoles)]
        public async Task ReactionRolesDeleteAll()
        {
            await _rero.RemoveAllReactionRoles(ctx.Guild.Id);
            await ctx.OkAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageRoles)]
        [BotPerm(GuildPerm.ManageRoles)]
        [Ratelimit(60)]
        public async Task ReactionRolesTransfer(ulong fromMessageId, ulong toMessageId)
        {
            var msg = await ctx.Channel.GetMessageAsync(toMessageId);

            if (msg is null)
            {
                await ctx.ErrorAsync();
                return;
            }

            var reactions = await _rero.TransferReactionRolesAsync(ctx.Guild.Id, fromMessageId, toMessageId);

            if (reactions.Count == 0)
            {
                await ctx.ErrorAsync();
            }
            else
            {
                foreach (var r in reactions)
                {
                    await msg.AddReactionAsync(r);
                }
            }
        }
    }
}