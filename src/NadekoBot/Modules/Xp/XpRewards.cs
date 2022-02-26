using NadekoBot.Modules.Gambling.Services;
using NadekoBot.Modules.Xp.Services;

namespace NadekoBot.Modules.Xp;

public partial class Xp
{
    public partial class XpRewards : NadekoModule<XpService>
    {
        private readonly GamblingConfigService _gss;

        public XpRewards(GamblingConfigService gcs)
            => _gss = gcs;

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async partial Task XpRewsReset()
        {
            var promptEmbed = _eb.Create()
                                 .WithPendingColor()
                                 .WithDescription(GetText(strs.xprewsreset_confirm));
            
            var reply = await PromptUserConfirmAsync(promptEmbed);

            if (!reply)
                return;

            await _service.ResetXpRewards(ctx.Guild.Id);
            await ctx.OkAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public partial Task XpLevelUpRewards(int page = 1)
        {
            page--;

            if (page is < 0 or > 100)
                return Task.CompletedTask;

            var allRewards = _service.GetRoleRewards(ctx.Guild.Id)
                                     .OrderBy(x => x.Level)
                                     .Select(x =>
                                     {
                                         var sign = !x.Remove ? @"✅ " : @"❌ ";

                                         var str = ctx.Guild.GetRole(x.RoleId)?.ToString();

                                         if (str is null)
                                             str = GetText(strs.role_not_found(Format.Code(x.RoleId.ToString())));
                                         else
                                         {
                                             if (!x.Remove)
                                                 str = GetText(strs.xp_receive_role(Format.Bold(str)));
                                             else
                                                 str = GetText(strs.xp_lose_role(Format.Bold(str)));
                                         }

                                         return (x.Level, Text: sign + str);
                                     })
                                     .Concat(_service.GetCurrencyRewards(ctx.Guild.Id)
                                                     .OrderBy(x => x.Level)
                                                     .Select(x => (x.Level,
                                                         Format.Bold(x.Amount + _gss.Data.Currency.Sign))))
                                     .GroupBy(x => x.Level)
                                     .OrderBy(x => x.Key)
                                     .ToList();

            return Context.SendPaginatedConfirmAsync(page,
                cur =>
                {
                    var embed = _eb.Create().WithTitle(GetText(strs.level_up_rewards)).WithOkColor();

                    var localRewards = allRewards.Skip(cur * 9).Take(9).ToList();

                    if (!localRewards.Any())
                        return embed.WithDescription(GetText(strs.no_level_up_rewards));

                    foreach (var reward in localRewards)
                        embed.AddField(GetText(strs.level_x(reward.Key)),
                            string.Join("\n", reward.Select(y => y.Item2)));

                    return embed;
                },
                allRewards.Count,
                9);
        }

        [Cmd]
        [UserPerm(GuildPerm.Administrator)]
        [BotPerm(GuildPerm.ManageRoles)]
        [RequireContext(ContextType.Guild)]
        [Priority(2)]
        public async partial Task XpRoleReward(int level)
        {
            _service.ResetRoleReward(ctx.Guild.Id, level);
            await ReplyConfirmLocalizedAsync(strs.xp_role_reward_cleared(level));
        }

        [Cmd]
        [UserPerm(GuildPerm.Administrator)]
        [BotPerm(GuildPerm.ManageRoles)]
        [RequireContext(ContextType.Guild)]
        [Priority(1)]
        public async partial Task XpRoleReward(int level, AddRemove action, [Leftover] IRole role)
        {
            if (level < 1)
                return;

            _service.SetRoleReward(ctx.Guild.Id, level, role.Id, action == AddRemove.Remove);
            if (action == AddRemove.Add)
                await ReplyConfirmLocalizedAsync(strs.xp_role_reward_add_role(level, Format.Bold(role.ToString())));
            else
            {
                await ReplyConfirmLocalizedAsync(strs.xp_role_reward_remove_role(Format.Bold(level.ToString()),
                    Format.Bold(role.ToString())));
            }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async partial Task XpCurrencyReward(int level, int amount = 0)
        {
            if (level < 1 || amount < 0)
                return;

            _service.SetCurrencyReward(ctx.Guild.Id, level, amount);
            var config = _gss.Data;

            if (amount == 0)
                await ReplyConfirmLocalizedAsync(strs.cur_reward_cleared(level, config.Currency.Sign));
            else
                await ReplyConfirmLocalizedAsync(strs.cur_reward_added(level,
                    Format.Bold(amount + config.Currency.Sign)));
        }
    }
}