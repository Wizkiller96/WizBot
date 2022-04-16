#nullable disable
using CommandLine;
using Humanizer.Localisation;
using NadekoBot.Common.TypeReaders.Models;
using NadekoBot.Modules.Administration.Services;
using NadekoBot.Modules.Permissions.Services;
using NadekoBot.Services.Database.Models;

namespace NadekoBot.Modules.Administration;

public partial class Administration
{
    [Group]
    public partial class UserPunishCommands : NadekoModule<UserPunishService>
    {
        public enum AddRole
        {
            AddRole
        }

        private readonly MuteService _mute;
        private readonly BlacklistService _blacklistService;

        public UserPunishCommands(MuteService mute, BlacklistService blacklistService)
        {
            _mute = mute;
            _blacklistService = blacklistService;
        }

        private async Task<bool> CheckRoleHierarchy(IGuildUser target)
        {
            var curUser = ((SocketGuild)ctx.Guild).CurrentUser;
            var ownerId = ctx.Guild.OwnerId;
            var modMaxRole = ((IGuildUser)ctx.User).GetRoles().Max(r => r.Position);
            var targetMaxRole = target.GetRoles().Max(r => r.Position);
            var botMaxRole = curUser.GetRoles().Max(r => r.Position);
            // bot can't punish a user who is higher in the hierarchy. Discord will return 403
            // moderator can be owner, in which case role hierarchy doesn't matter
            // otherwise, moderator has to have a higher role
            if (botMaxRole <= targetMaxRole
                || (ctx.User.Id != ownerId && targetMaxRole >= modMaxRole)
                || target.Id == ownerId)
            {
                await ReplyErrorLocalizedAsync(strs.hierarchy);
                return false;
            }

            return true;
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.BanMembers)]
        public partial Task Warn(IGuildUser user, [Leftover] string reason = null)
            => Warn(1, user, reason);

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.BanMembers)]
        public async partial Task Warn(int weight, IGuildUser user, [Leftover] string reason = null)
        {
            if (weight <= 0)
                return;

            if (!await CheckRoleHierarchy(user))
                return;

            var dmFailed = false;
            try
            {
                await user.EmbedAsync(_eb.Create()
                                         .WithErrorColor()
                                         .WithDescription(GetText(strs.warned_on(ctx.Guild.ToString())))
                                         .AddField(GetText(strs.moderator), ctx.User.ToString())
                                         .AddField(GetText(strs.reason), reason ?? "-"));
            }
            catch
            {
                dmFailed = true;
            }

            WarningPunishment punishment;
            try
            {
                punishment = await _service.Warn(ctx.Guild, user.Id, ctx.User, weight, reason);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Exception occured while warning a user");
                var errorEmbed = _eb.Create().WithErrorColor().WithDescription(GetText(strs.cant_apply_punishment));

                if (dmFailed)
                    errorEmbed.WithFooter("⚠️ " + GetText(strs.unable_to_dm_user));

                await ctx.Channel.EmbedAsync(errorEmbed);
                return;
            }

            var embed = _eb.Create().WithOkColor();
            if (punishment is null)
                embed.WithDescription(GetText(strs.user_warned(Format.Bold(user.ToString()))));
            else
            {
                embed.WithDescription(GetText(strs.user_warned_and_punished(Format.Bold(user.ToString()),
                    Format.Bold(punishment.Punishment.ToString()))));
            }

            if (dmFailed)
                embed.WithFooter("⚠️ " + GetText(strs.unable_to_dm_user));

            await ctx.Channel.EmbedAsync(embed);
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        [NadekoOptions(typeof(WarnExpireOptions))]
        [Priority(1)]
        public async partial Task WarnExpire()
        {
            var expireDays = await _service.GetWarnExpire(ctx.Guild.Id);

            if (expireDays == 0)
                await ReplyConfirmLocalizedAsync(strs.warns_dont_expire);
            else
                await ReplyErrorLocalizedAsync(strs.warns_expire_in(expireDays));
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        [NadekoOptions(typeof(WarnExpireOptions))]
        [Priority(2)]
        public async partial Task WarnExpire(int days, params string[] args)
        {
            if (days is < 0 or > 366)
                return;

            var opts = OptionsParser.ParseFrom<WarnExpireOptions>(args);

            await ctx.Channel.TriggerTypingAsync();

            await _service.WarnExpireAsync(ctx.Guild.Id, days, opts.Delete);
            if (days == 0)
            {
                await ReplyConfirmLocalizedAsync(strs.warn_expire_reset);
                return;
            }

            if (opts.Delete)
                await ReplyConfirmLocalizedAsync(strs.warn_expire_set_delete(Format.Bold(days.ToString())));
            else
                await ReplyConfirmLocalizedAsync(strs.warn_expire_set_clear(Format.Bold(days.ToString())));
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.BanMembers)]
        [Priority(2)]
        public partial Task Warnlog(int page, [Leftover] IGuildUser user = null)
        {
            user ??= (IGuildUser)ctx.User;

            return Warnlog(page, user.Id);
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [Priority(3)]
        public partial Task Warnlog(IGuildUser user = null)
        {
            user ??= (IGuildUser)ctx.User;

            return ctx.User.Id == user.Id || ((IGuildUser)ctx.User).GuildPermissions.BanMembers
                ? Warnlog(user.Id)
                : Task.CompletedTask;
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.BanMembers)]
        [Priority(0)]
        public partial Task Warnlog(int page, ulong userId)
            => InternalWarnlog(userId, page - 1);

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.BanMembers)]
        [Priority(1)]
        public partial Task Warnlog(ulong userId)
            => InternalWarnlog(userId, 0);

        private async Task InternalWarnlog(ulong userId, int inputPage)
        {
            if (inputPage < 0)
                return;

            var allWarnings = _service.UserWarnings(ctx.Guild.Id, userId);

            await ctx.SendPaginatedConfirmAsync(inputPage,
                page =>
                {
                    var warnings = allWarnings.Skip(page * 9).Take(9).ToArray();

                    var user = (ctx.Guild as SocketGuild)?.GetUser(userId)?.ToString() ?? userId.ToString();
                    var embed = _eb.Create().WithOkColor().WithTitle(GetText(strs.warnlog_for(user)));

                    if (!warnings.Any())
                        embed.WithDescription(GetText(strs.warnings_none));
                    else
                    {
                        var descText = GetText(strs.warn_count(
                            Format.Bold(warnings.Where(x => !x.Forgiven).Sum(x => x.Weight).ToString()),
                            Format.Bold(warnings.Sum(x => x.Weight).ToString())));

                        embed.WithDescription(descText);

                        var i = page * 9;
                        foreach (var w in warnings)
                        {
                            i++;
                            var name = GetText(strs.warned_on_by(w.DateAdded?.ToString("dd.MM.yyy"),
                                w.DateAdded?.ToString("HH:mm"),
                                w.Moderator));

                            if (w.Forgiven)
                                name = $"{Format.Strikethrough(name)} {GetText(strs.warn_cleared_by(w.ForgivenBy))}";


                            embed.AddField($"#`{i}` " + name,
                                Format.Code(GetText(strs.warn_weight(w.Weight))) + '\n' + w.Reason.TrimTo(1000));
                        }
                    }

                    return embed;
                },
                allWarnings.Length,
                9);
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.BanMembers)]
        public async partial Task WarnlogAll(int page = 1)
        {
            if (--page < 0)
                return;
            var warnings = _service.WarnlogAll(ctx.Guild.Id);

            await ctx.SendPaginatedConfirmAsync(page,
                curPage =>
                {
                    var ws = warnings.Skip(curPage * 15)
                                     .Take(15)
                                     .ToArray()
                                     .Select(x =>
                                     {
                                         var all = x.Count();
                                         var forgiven = x.Count(y => y.Forgiven);
                                         var total = all - forgiven;
                                         var usr = ((SocketGuild)ctx.Guild).GetUser(x.Key);
                                         return (usr?.ToString() ?? x.Key.ToString())
                                                + $" | {total} ({all} - {forgiven})";
                                     });

                    return _eb.Create()
                              .WithOkColor()
                              .WithTitle(GetText(strs.warnings_list))
                              .WithDescription(string.Join("\n", ws));
                },
                warnings.Length,
                15);
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.BanMembers)]
        public partial Task Warnclear(IGuildUser user, int index = 0)
            => Warnclear(user.Id, index);

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.BanMembers)]
        public async partial Task Warnclear(ulong userId, int index = 0)
        {
            if (index < 0)
                return;
            var success = await _service.WarnClearAsync(ctx.Guild.Id, userId, index, ctx.User.ToString());
            var userStr = Format.Bold((ctx.Guild as SocketGuild)?.GetUser(userId)?.ToString() ?? userId.ToString());
            if (index == 0)
                await ReplyErrorLocalizedAsync(strs.warnings_cleared(userStr));
            else
            {
                if (success)
                    await ReplyConfirmLocalizedAsync(strs.warning_cleared(Format.Bold(index.ToString()), userStr));
                else
                    await ReplyErrorLocalizedAsync(strs.warning_clear_fail);
            }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.BanMembers)]
        [Priority(1)]
        public async partial Task WarnPunish(
            int number,
            AddRole _,
            IRole role,
            StoopidTime time = null)
        {
            var punish = PunishmentAction.AddRole;

            if (ctx.Guild.OwnerId != ctx.User.Id
                && role.Position >= ((IGuildUser)ctx.User).GetRoles().Max(x => x.Position))
            {
                await ReplyErrorLocalizedAsync(strs.role_too_high);
                return;
            }

            var success = _service.WarnPunish(ctx.Guild.Id, number, punish, time, role);

            if (!success)
                return;

            if (time is null)
            {
                await ReplyConfirmLocalizedAsync(strs.warn_punish_set(Format.Bold(punish.ToString()),
                    Format.Bold(number.ToString())));
            }
            else
            {
                await ReplyConfirmLocalizedAsync(strs.warn_punish_set_timed(Format.Bold(punish.ToString()),
                    Format.Bold(number.ToString()),
                    Format.Bold(time.Input)));
            }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.BanMembers)]
        public async partial Task WarnPunish(int number, PunishmentAction punish, StoopidTime time = null)
        {
            // this should never happen. Addrole has its own method with higher priority
            if (punish == PunishmentAction.AddRole)
                return;

            var success = _service.WarnPunish(ctx.Guild.Id, number, punish, time);

            if (!success)
                return;

            if (time is null)
            {
                await ReplyConfirmLocalizedAsync(strs.warn_punish_set(Format.Bold(punish.ToString()),
                    Format.Bold(number.ToString())));
            }
            else
            {
                await ReplyConfirmLocalizedAsync(strs.warn_punish_set_timed(Format.Bold(punish.ToString()),
                    Format.Bold(number.ToString()),
                    Format.Bold(time.Input)));
            }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.BanMembers)]
        public async partial Task WarnPunish(int number)
        {
            if (!_service.WarnPunishRemove(ctx.Guild.Id, number))
                return;

            await ReplyConfirmLocalizedAsync(strs.warn_punish_rem(Format.Bold(number.ToString())));
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async partial Task WarnPunishList()
        {
            var ps = _service.WarnPunishList(ctx.Guild.Id);

            string list;
            if (ps.Any())
            {
                list = string.Join("\n",
                    ps.Select(x
                        => $"{x.Count} -> {x.Punishment} {(x.Punishment == PunishmentAction.AddRole ? $"<@&{x.RoleId}>" : "")} {(x.Time <= 0 ? "" : x.Time + "m")} "));
            }
            else
                list = GetText(strs.warnpl_none);

            await SendConfirmAsync(GetText(strs.warn_punish_list), list);
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.BanMembers)]
        [BotPerm(GuildPerm.BanMembers)]
        [Priority(1)]
        public async partial Task Ban(StoopidTime time, IUser user, [Leftover] string msg = null)
        {
            if (time.Time > TimeSpan.FromDays(49))
                return;

            var guildUser = await ((DiscordSocketClient)Context.Client).Rest.GetGuildUserAsync(ctx.Guild.Id, user.Id);

            if (guildUser is not null && !await CheckRoleHierarchy(guildUser))
                return;

            var dmFailed = false;

            if (guildUser is not null)
            {
                try
                {
                    var defaultMessage = GetText(strs.bandm(Format.Bold(ctx.Guild.Name), msg));
                    var embed = _service.GetBanUserDmEmbed(Context, guildUser, defaultMessage, msg, time.Time);
                    if (embed is not null)
                        await guildUser.SendAsync(embed);
                }
                catch
                {
                    dmFailed = true;
                }
            }

            await _mute.TimedBan(ctx.Guild, user, time.Time, (ctx.User + " | " + msg).TrimTo(512));
            var toSend = _eb.Create()
                            .WithOkColor()
                            .WithTitle("⛔️ " + GetText(strs.banned_user))
                            .AddField(GetText(strs.username), user.ToString(), true)
                            .AddField("ID", user.Id.ToString(), true)
                            .AddField(GetText(strs.duration),
                                time.Time.Humanize(3, minUnit: TimeUnit.Minute, culture: Culture),
                                true);

            if (dmFailed)
                toSend.WithFooter("⚠️ " + GetText(strs.unable_to_dm_user));

            await ctx.Channel.EmbedAsync(toSend);
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.BanMembers)]
        [BotPerm(GuildPerm.BanMembers)]
        [Priority(0)]
        public async partial Task Ban(ulong userId, [Leftover] string msg = null)
        {
            var user = await ((DiscordSocketClient)Context.Client).Rest.GetGuildUserAsync(ctx.Guild.Id, userId);
            if (user is null)
            {
                await ctx.Guild.AddBanAsync(userId, 7, (ctx.User + " | " + msg).TrimTo(512));

                await ctx.Channel.EmbedAsync(_eb.Create()
                                                .WithOkColor()
                                                .WithTitle("⛔️ " + GetText(strs.banned_user))
                                                .AddField("ID", userId.ToString(), true));
            }
            else
                await Ban(user, msg);
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.BanMembers)]
        [BotPerm(GuildPerm.BanMembers)]
        [Priority(2)]
        public async partial Task Ban(IGuildUser user, [Leftover] string msg = null)
        {
            if (!await CheckRoleHierarchy(user))
                return;

            var dmFailed = false;

            try
            {
                var defaultMessage = GetText(strs.bandm(Format.Bold(ctx.Guild.Name), msg));
                var embed = _service.GetBanUserDmEmbed(Context, user, defaultMessage, msg, null);
                if (embed is not null)
                    await user.SendAsync(embed);
            }
            catch
            {
                dmFailed = true;
            }

            await ctx.Guild.AddBanAsync(user, 7, (ctx.User + " | " + msg).TrimTo(512));

            var toSend = _eb.Create()
                            .WithOkColor()
                            .WithTitle("⛔️ " + GetText(strs.banned_user))
                            .AddField(GetText(strs.username), user.ToString(), true)
                            .AddField("ID", user.Id.ToString(), true);

            if (dmFailed)
                toSend.WithFooter("⚠️ " + GetText(strs.unable_to_dm_user));

            await ctx.Channel.EmbedAsync(toSend);
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.BanMembers)]
        [BotPerm(GuildPerm.BanMembers)]
        public async partial Task BanMessage([Leftover] string message = null)
        {
            if (message is null)
            {
                var template = _service.GetBanTemplate(ctx.Guild.Id);
                if (template is null)
                {
                    await ReplyConfirmLocalizedAsync(strs.banmsg_default);
                    return;
                }

                await SendConfirmAsync(template);
                return;
            }

            _service.SetBanTemplate(ctx.Guild.Id, message);
            await ctx.OkAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.BanMembers)]
        [BotPerm(GuildPerm.BanMembers)]
        public async partial Task BanMsgReset()
        {
            _service.SetBanTemplate(ctx.Guild.Id, null);
            await ctx.OkAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.BanMembers)]
        [BotPerm(GuildPerm.BanMembers)]
        [Priority(0)]
        public partial Task BanMessageTest([Leftover] string reason = null)
            => InternalBanMessageTest(reason, null);

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.BanMembers)]
        [BotPerm(GuildPerm.BanMembers)]
        [Priority(1)]
        public partial Task BanMessageTest(StoopidTime duration, [Leftover] string reason = null)
            => InternalBanMessageTest(reason, duration.Time);

        private async Task InternalBanMessageTest(string reason, TimeSpan? duration)
        {
            var defaultMessage = GetText(strs.bandm(Format.Bold(ctx.Guild.Name), reason));
            var embed = _service.GetBanUserDmEmbed(Context, (IGuildUser)ctx.User, defaultMessage, reason, duration);

            if (embed is null)
                await ConfirmLocalizedAsync(strs.banmsg_disabled);
            else
            {
                try
                {
                    await ctx.User.SendAsync(embed);
                }
                catch (Exception)
                {
                    await ReplyErrorLocalizedAsync(strs.unable_to_dm_user);
                    return;
                }

                await ctx.OkAsync();
            }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.BanMembers)]
        [BotPerm(GuildPerm.BanMembers)]
        public async partial Task Unban([Leftover] string user)
        {
            var bans = await ctx.Guild.GetBansAsync().FlattenAsync();

            var bun = bans.FirstOrDefault(x => x.User.ToString()!.ToLowerInvariant() == user.ToLowerInvariant());

            if (bun is null)
            {
                await ReplyErrorLocalizedAsync(strs.user_not_found);
                return;
            }

            await UnbanInternal(bun.User);
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.BanMembers)]
        [BotPerm(GuildPerm.BanMembers)]
        public async partial Task Unban(ulong userId)
        {
            var bun = await ctx.Guild.GetBanAsync(userId);

            if (bun is null)
            {
                await ReplyErrorLocalizedAsync(strs.user_not_found);
                return;
            }

            await UnbanInternal(bun.User);
        }

        private async Task UnbanInternal(IUser user)
        {
            await ctx.Guild.RemoveBanAsync(user);

            await ReplyConfirmLocalizedAsync(strs.unbanned_user(Format.Bold(user.ToString())));
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.KickMembers | GuildPerm.ManageMessages)]
        [BotPerm(GuildPerm.BanMembers)]
        public partial Task Softban(IGuildUser user, [Leftover] string msg = null)
            => SoftbanInternal(user, msg);

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.KickMembers | GuildPerm.ManageMessages)]
        [BotPerm(GuildPerm.BanMembers)]
        public async partial Task Softban(ulong userId, [Leftover] string msg = null)
        {
            var user = await ((DiscordSocketClient)Context.Client).Rest.GetGuildUserAsync(ctx.Guild.Id, userId);
            if (user is null)
                return;

            await SoftbanInternal(user, msg);
        }

        private async Task SoftbanInternal(IGuildUser user, [Leftover] string msg = null)
        {
            if (!await CheckRoleHierarchy(user))
                return;

            var dmFailed = false;

            try
            {
                await user.SendErrorAsync(_eb, GetText(strs.sbdm(Format.Bold(ctx.Guild.Name), msg)));
            }
            catch
            {
                dmFailed = true;
            }

            await ctx.Guild.AddBanAsync(user, 7, ("Softban | " + ctx.User + " | " + msg).TrimTo(512));
            try { await ctx.Guild.RemoveBanAsync(user); }
            catch { await ctx.Guild.RemoveBanAsync(user); }

            var toSend = _eb.Create()
                            .WithOkColor()
                            .WithTitle("☣ " + GetText(strs.sb_user))
                            .AddField(GetText(strs.username), user.ToString(), true)
                            .AddField("ID", user.Id.ToString(), true);

            if (dmFailed)
                toSend.WithFooter("⚠️ " + GetText(strs.unable_to_dm_user));

            await ctx.Channel.EmbedAsync(toSend);
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.KickMembers)]
        [BotPerm(GuildPerm.KickMembers)]
        [Priority(1)]
        public partial Task Kick(IGuildUser user, [Leftover] string msg = null)
            => KickInternal(user, msg);

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.KickMembers)]
        [BotPerm(GuildPerm.KickMembers)]
        [Priority(0)]
        public async partial Task Kick(ulong userId, [Leftover] string msg = null)
        {
            var user = await ((DiscordSocketClient)Context.Client).Rest.GetGuildUserAsync(ctx.Guild.Id, userId);
            if (user is null)
                return;

            await KickInternal(user, msg);
        }

        private async Task KickInternal(IGuildUser user, string msg = null)
        {
            if (!await CheckRoleHierarchy(user))
                return;

            var dmFailed = false;

            try
            {
                await user.SendErrorAsync(_eb, GetText(strs.kickdm(Format.Bold(ctx.Guild.Name), msg)));
            }
            catch
            {
                dmFailed = true;
            }

            await user.KickAsync((ctx.User + " | " + msg).TrimTo(512));

            var toSend = _eb.Create()
                            .WithOkColor()
                            .WithTitle(GetText(strs.kicked_user))
                            .AddField(GetText(strs.username), user.ToString(), true)
                            .AddField("ID", user.Id.ToString(), true);

            if (dmFailed)
                toSend.WithFooter("⚠️ " + GetText(strs.unable_to_dm_user));

            await ctx.Channel.EmbedAsync(toSend);
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.BanMembers)]
        [BotPerm(GuildPerm.BanMembers)]
        [Ratelimit(30)]
        public async partial Task MassBan(params string[] userStrings)
        {
            if (userStrings.Length == 0)
                return;

            var missing = new List<string>();
            var banning = new HashSet<IUser>();

            await ctx.Channel.TriggerTypingAsync();
            foreach (var userStr in userStrings)
            {
                if (ulong.TryParse(userStr, out var userId))
                {
                    IUser user = await ctx.Guild.GetUserAsync(userId)
                                 ?? await ((DiscordSocketClient)Context.Client).Rest.GetGuildUserAsync(ctx.Guild.Id,
                                     userId);

                    if (user is null)
                    {
                        // if IGuildUser is null, try to get IUser
                        user = await ((DiscordSocketClient)Context.Client).Rest.GetUserAsync(userId);

                        // only add to missing if *still* null
                        if (user is null)
                        {
                            missing.Add(userStr);
                            continue;
                        }
                    }

                    //Hierachy checks only if the user is in the guild
                    if (user is IGuildUser gu && !await CheckRoleHierarchy(gu))
                        return;

                    banning.Add(user);
                }
                else
                    missing.Add(userStr);
            }

            var missStr = string.Join("\n", missing);
            if (string.IsNullOrWhiteSpace(missStr))
                missStr = "-";

            var toSend = _eb.Create(ctx)
                            .WithDescription(GetText(strs.mass_ban_in_progress(banning.Count)))
                            .AddField(GetText(strs.invalid(missing.Count)), missStr)
                            .WithPendingColor();

            var banningMessage = await ctx.Channel.EmbedAsync(toSend);

            foreach (var toBan in banning)
            {
                try
                {
                    await ctx.Guild.AddBanAsync(toBan.Id, 7, $"{ctx.User} | Massban");
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Error banning {User} user in {GuildId} server", toBan.Id, ctx.Guild.Id);
                }
            }

            await banningMessage.ModifyAsync(x => x.Embed = _eb.Create()
                                                               .WithDescription(
                                                                   GetText(strs.mass_ban_completed(banning.Count())))
                                                               .AddField(GetText(strs.invalid(missing.Count)), missStr)
                                                               .WithOkColor()
                                                               .Build());
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.BanMembers)]
        [BotPerm(GuildPerm.BanMembers)]
        [OwnerOnly]
        public async partial Task MassKill([Leftover] string people)
        {
            if (string.IsNullOrWhiteSpace(people))
                return;

            var (bans, missing) = _service.MassKill((SocketGuild)ctx.Guild, people);

            var missStr = string.Join("\n", missing);
            if (string.IsNullOrWhiteSpace(missStr))
                missStr = "-";

            //send a message but don't wait for it
            var banningMessageTask = ctx.Channel.EmbedAsync(_eb.Create()
                                                               .WithDescription(
                                                                   GetText(strs.mass_kill_in_progress(bans.Count())))
                                                               .AddField(GetText(strs.invalid(missing)), missStr)
                                                               .WithPendingColor());

            //do the banning
            await Task.WhenAll(bans.Where(x => x.Id.HasValue)
                                   .Select(x => ctx.Guild.AddBanAsync(x.Id.Value,
                                       7,
                                       x.Reason,
                                       new()
                                       {
                                           RetryMode = RetryMode.AlwaysRetry
                                       })));

            //wait for the message and edit it
            var banningMessage = await banningMessageTask;

            await banningMessage.ModifyAsync(x => x.Embed = _eb.Create()
                                                               .WithDescription(
                                                                   GetText(strs.mass_kill_completed(bans.Count())))
                                                               .AddField(GetText(strs.invalid(missing)), missStr)
                                                               .WithOkColor()
                                                               .Build());
        }

        public class WarnExpireOptions : INadekoCommandOptions
        {
            [Option('d', "delete", Default = false, HelpText = "Delete warnings instead of clearing them.")]
            public bool Delete { get; set; } = false;

            public void NormalizeOptions()
            {
            }
        }
    }
}