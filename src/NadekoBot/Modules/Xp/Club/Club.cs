#nullable disable
using NadekoBot.Db.Models;
using NadekoBot.Modules.Xp.Services;

namespace NadekoBot.Modules.Xp;

public partial class Xp
{
    [Group]
    public partial class Club : NadekoModule<IClubService>
    {
        private readonly XpService _xps;

        public Club(XpService xps)
            => _xps = xps;

        [Cmd]
        public async Task ClubTransfer([Leftover] IUser newOwner)
        {
            var result = _service.TransferClub(ctx.User, newOwner);

            if (!result.TryPickT0(out var club, out var error))
            {
                if(error == ClubTransferError.NotOwner)
                    await ReplyErrorLocalizedAsync(strs.club_owner_only);
                else 
                    await ReplyErrorLocalizedAsync(strs.club_target_not_member);
            }
            else
            {
                await ReplyConfirmLocalizedAsync(
                    strs.club_transfered(
                        Format.Bold(club.Name),
                        Format.Bold(newOwner.ToString())
                    )
                );
            }
        }

        [Cmd]
        public async Task ClubAdmin([Leftover] IUser toAdmin)
        {
            var result = await _service.ToggleAdminAsync(ctx.User, toAdmin);

            if (result == ToggleAdminResult.AddedAdmin)
                await ReplyConfirmLocalizedAsync(strs.club_admin_add(Format.Bold(toAdmin.ToString())));
            else if (result == ToggleAdminResult.RemovedAdmin)
                await ReplyConfirmLocalizedAsync(strs.club_admin_remove(Format.Bold(toAdmin.ToString())));
            else if (result == ToggleAdminResult.NotOwner)
                await ReplyErrorLocalizedAsync(strs.club_owner_only);
            else if (result == ToggleAdminResult.CantTargetThyself)
                await ReplyErrorLocalizedAsync(strs.club_admin_invalid_target);
            else if (result == ToggleAdminResult.TargetNotMember)
                await ReplyErrorLocalizedAsync(strs.club_target_not_member);
        }

        [Cmd]
        public async Task ClubCreate([Leftover] string clubName)
        {
            if (string.IsNullOrWhiteSpace(clubName) || clubName.Length > 20)
            {
                await ReplyErrorLocalizedAsync(strs.club_name_too_long);
                return;
            }

            var result = await _service.CreateClubAsync(ctx.User, clubName);

            if (result == ClubCreateResult.NameTaken)
            {
                await ReplyErrorLocalizedAsync(strs.club_create_error_name);
                return;
            }

            if (result == ClubCreateResult.InsufficientLevel)
            {
                await ReplyErrorLocalizedAsync(strs.club_create_insuff_lvl);
                return;
            }

            if (result == ClubCreateResult.AlreadyInAClub)
            {
                await ReplyErrorLocalizedAsync(strs.club_already_in);
                return;
            }

            await ReplyConfirmLocalizedAsync(strs.club_created(Format.Bold(clubName)));
        }

        [Cmd]
        public async Task ClubIcon([Leftover] string url = null)
        {
            if ((!Uri.IsWellFormedUriString(url, UriKind.Absolute) && url is not null))
            {
                await ReplyErrorLocalizedAsync(strs.club_icon_url_format);
                return;
            }

            var result = await _service.SetClubIconAsync(ctx.User.Id, url);
            if(result == SetClubIconResult.Success)
                await ReplyConfirmLocalizedAsync(strs.club_icon_set);
            else if (result == SetClubIconResult.NotOwner)
                await ReplyErrorLocalizedAsync(strs.club_owner_only);
            else if (result == SetClubIconResult.TooLarge)
                await ReplyErrorLocalizedAsync(strs.club_icon_too_large);
            else if (result == SetClubIconResult.InvalidFileType)
                await ReplyErrorLocalizedAsync(strs.club_icon_invalid_filetype);
        }

        private async Task InternalClubInfoAsync(ClubInfo club)
        {
            var lvl = new LevelStats(club.Xp);
            var users = club.Members.OrderByDescending(x =>
            {
                var l = new LevelStats(x.TotalXp).Level;
                if (club.OwnerId == x.Id)
                    return int.MaxValue;
                if (x.IsClubAdmin)
                    return (int.MaxValue / 2) + l;
                return l;
            });

            await ctx.SendPaginatedConfirmAsync(0,
                page =>
                {
                    var embed = _eb.Create()
                        .WithOkColor()
                        .WithTitle($"{club}")
                        .WithDescription(GetText(strs.level_x(lvl.Level + $" ({club.Xp} xp)")))
                        .AddField(GetText(strs.desc),
                            string.IsNullOrWhiteSpace(club.Description) ? "-" : club.Description)
                        .AddField(GetText(strs.owner), club.Owner.ToString(), true)
                        // .AddField(GetText(strs.level_req), club.MinimumLevelReq.ToString(), true)
                        .AddField(GetText(strs.members),
                            string.Join("\n",
                                users.Skip(page * 10)
                                    .Take(10)
                                    .Select(x =>
                                    {
                                        var l = new LevelStats(x.TotalXp);
                                        var lvlStr = Format.Bold($" ⟪{l.Level}⟫");
                                        if (club.OwnerId == x.Id)
                                            return x + "🌟" + lvlStr;
                                        if (x.IsClubAdmin)
                                            return x + "⭐" + lvlStr;
                                        return x + lvlStr;
                                    })));

                    if (Uri.IsWellFormedUriString(club.ImageUrl, UriKind.Absolute))
                        return embed.WithThumbnailUrl(club.ImageUrl);

                    return embed;
                },
                club.Members.Count,
                10);
        }

        [Cmd]
        [Priority(1)]
        public async Task ClubInformation(IUser user = null)
        {
            user ??= ctx.User;
            var club = _service.GetClubByMember(user);
            if (club is null)
            {
                await ErrorLocalizedAsync(strs.club_user_not_in_club(Format.Bold(user.ToString())));
                return;
            }

            await InternalClubInfoAsync(club);
        }

        [Cmd]
        [Priority(0)]
        public async Task ClubInformation([Leftover] string clubName = null)
        {
            if (string.IsNullOrWhiteSpace(clubName))
            {
                await ClubInformation(ctx.User);
                return;
            }

            if (!_service.GetClubByName(clubName, out var club))
            {
                await ReplyErrorLocalizedAsync(strs.club_not_exists);
                return;
            }

            await InternalClubInfoAsync(club);
        }

        [Cmd]
        public Task ClubBans(int page = 1)
        {
            if (--page < 0)
                return Task.CompletedTask;

            var club = _service.GetClubWithBansAndApplications(ctx.User.Id);
            if (club is null)
                return ReplyErrorLocalizedAsync(strs.club_admin_perms);

            var bans = club.Bans.Select(x => x.User).ToArray();

            return ctx.SendPaginatedConfirmAsync(page,
                _ =>
                {
                    var toShow = string.Join("\n", bans
                        .Skip(page * 10).Take(10)
                        .Select(x => x.ToString()));

                    return _eb.Create()
                        .WithTitle(GetText(strs.club_bans_for(club.ToString())))
                        .WithDescription(toShow)
                        .WithOkColor();
                },
                bans.Length,
                10);
        }

        [Cmd]
        public Task ClubApps(int page = 1)
        {
            if (--page < 0)
                return Task.CompletedTask;

            var club = _service.GetClubWithBansAndApplications(ctx.User.Id);
            if (club is null)
                return ReplyErrorLocalizedAsync(strs.club_admin_perms);

            var apps = club.Applicants.Select(x => x.User).ToArray();

            return ctx.SendPaginatedConfirmAsync(page,
                _ =>
                {
                    var toShow = string.Join("\n", apps.Skip(page * 10).Take(10).Select(x => x.ToString()));

                    return _eb.Create()
                        .WithTitle(GetText(strs.club_apps_for(club.ToString())))
                        .WithDescription(toShow)
                        .WithOkColor();
                },
                apps.Length,
                10);
        }

        [Cmd]
        public async Task ClubApply([Leftover] string clubName)
        {
            if (string.IsNullOrWhiteSpace(clubName))
                return;

            if (!_service.GetClubByName(clubName, out var club))
            {
                await ReplyErrorLocalizedAsync(strs.club_not_exists);
                return;
            }

            var result = _service.ApplyToClub(ctx.User, club);
            if (result == ClubApplyResult.Success)
                await ReplyConfirmLocalizedAsync(strs.club_applied(Format.Bold(club.ToString())));
            else if (result == ClubApplyResult.Banned)
                await ReplyErrorLocalizedAsync(strs.club_join_banned);
            else if (result == ClubApplyResult.InsufficientLevel)
                await ReplyErrorLocalizedAsync(strs.club_insuff_lvl);
            else if (result == ClubApplyResult.AlreadyInAClub)
                await ReplyErrorLocalizedAsync(strs.club_already_in);
        }

        [Cmd]
        [Priority(1)]
        public Task ClubAccept(IUser user)
            => ClubAccept(user.ToString());

        [Cmd]
        [Priority(0)]
        public async Task ClubAccept([Leftover] string userName)
        {
            var result = _service.AcceptApplication(ctx.User.Id, userName, out var discordUser);
            if (result == ClubAcceptResult.Accepted)
                await ReplyConfirmLocalizedAsync(strs.club_accepted(Format.Bold(discordUser.ToString())));
            else if(result == ClubAcceptResult.NoSuchApplicant)
                await ReplyErrorLocalizedAsync(strs.club_accept_invalid_applicant);
            else if(result == ClubAcceptResult.NotOwnerOrAdmin)
                await ReplyErrorLocalizedAsync(strs.club_admin_perms);
        }

        [Cmd]
        public async Task ClubLeave()
        {
            var res = _service.LeaveClub(ctx.User);

            if (res == ClubLeaveResult.Success)
                await ReplyConfirmLocalizedAsync(strs.club_left);
            else if (res == ClubLeaveResult.NotInAClub)
                await ReplyErrorLocalizedAsync(strs.club_not_in_a_club);
            else
                await ReplyErrorLocalizedAsync(strs.club_owner_cant_leave);
        }

        [Cmd]
        [Priority(1)]
        public Task ClubKick([Leftover] IUser user)
            => ClubKick(user.ToString());

        [Cmd]
        [Priority(0)]
        public Task ClubKick([Leftover] string userName)
        {
            var result = _service.Kick(ctx.User.Id, userName, out var club);
            if(result == ClubKickResult.Success)
            {
                return ReplyConfirmLocalizedAsync(strs.club_user_kick(Format.Bold(userName),
                    Format.Bold(club.ToString())));
            }

            if (result == ClubKickResult.Hierarchy)
                return ReplyErrorLocalizedAsync(strs.club_kick_hierarchy);

            if (result == ClubKickResult.NotOwnerOrAdmin)
                return ReplyErrorLocalizedAsync(strs.club_admin_perms);
            
            return ReplyErrorLocalizedAsync(strs.club_target_not_member);
        }

        [Cmd]
        [Priority(1)]
        public Task ClubBan([Leftover] IUser user)
            => ClubBan(user.ToString());

        [Cmd]
        [Priority(0)]
        public Task ClubBan([Leftover] string userName)
        {
            var result = _service.Ban(ctx.User.Id, userName, out var club);
            if (result == ClubBanResult.Success)
            {
                return ReplyConfirmLocalizedAsync(strs.club_user_banned(Format.Bold(userName),
                    Format.Bold(club.ToString())));
            }

            if (result == ClubBanResult.Unbannable)
                return ReplyErrorLocalizedAsync(strs.club_ban_fail_unbannable);

            if (result == ClubBanResult.WrongUser)
                return ReplyErrorLocalizedAsync(strs.club_ban_fail_user_not_found);

            return ReplyErrorLocalizedAsync(strs.club_admin_perms);
        }

        [Cmd]
        [Priority(1)]
        public Task ClubUnBan([Leftover] IUser user)
            => ClubUnBan(user.ToString());

        [Cmd]
        [Priority(0)]
        public Task ClubUnBan([Leftover] string userName)
        {
            var result = _service.UnBan(ctx.User.Id, userName, out var club);

            if (result == ClubUnbanResult.Success)
            {
                return ReplyConfirmLocalizedAsync(strs.club_user_unbanned(Format.Bold(userName),
                    Format.Bold(club.ToString())));
            }

            if (result == ClubUnbanResult.WrongUser)
            {
                return ReplyErrorLocalizedAsync(strs.club_unban_fail_user_not_found);
            }

            return ReplyErrorLocalizedAsync(strs.club_admin_perms);
        }

        [Cmd]
        public async Task ClubDescription([Leftover] string desc = null)
        {
            if (_service.SetDescription(ctx.User.Id, desc))
            {
                desc = string.IsNullOrWhiteSpace(desc)
                    ? "-"
                    : desc;

                var eb = _eb.Create(ctx)
                    .WithAuthor(ctx.User)
                    .WithTitle(GetText(strs.club_desc_update))
                    .WithOkColor()
                    .WithDescription(desc);

                await ctx.Channel.EmbedAsync(eb);
            }
            else
            {
                await ReplyErrorLocalizedAsync(strs.club_desc_update_failed);
            }
        }

        [Cmd]
        public async Task ClubDisband()
        {
            if (_service.Disband(ctx.User.Id, out var club))
                await ReplyConfirmLocalizedAsync(strs.club_disbanded(Format.Bold(club.Name)));
            else
                await ReplyErrorLocalizedAsync(strs.club_disband_error);
        }

        [Cmd]
        public Task ClubLeaderboard(int page = 1)
        {
            if (--page < 0)
                return Task.CompletedTask;

            var clubs = _service.GetClubLeaderboardPage(page);

            var embed = _eb.Create().WithTitle(GetText(strs.club_leaderboard(page + 1))).WithOkColor();

            var i = page * 9;
            foreach (var club in clubs)
                embed.AddField($"#{++i} " + club, club.Xp + " xp");

            return ctx.Channel.EmbedAsync(embed);
        }
    }
}