#nullable disable
using NadekoBot.Db.Models;
using NadekoBot.Modules.Xp.Services;

namespace NadekoBot.Modules.Xp;

public partial class Xp
{
    [Group]
    public partial class Club : NadekoModule<ClubService>
    {
        private readonly XpService _xps;

        public Club(XpService xps)
            => _xps = xps;

        [Cmd]
        public async partial Task ClubTransfer([Leftover] IUser newOwner)
        {
            var club = _service.TransferClub(ctx.User, newOwner);

            if (club is not null)
            {
                await ReplyConfirmLocalizedAsync(strs.club_transfered(Format.Bold(club.Name),
                    Format.Bold(newOwner.ToString())));
            }
            else
                await ReplyErrorLocalizedAsync(strs.club_transfer_failed);
        }

        [Cmd]
        public async partial Task ClubAdmin([Leftover] IUser toAdmin)
        {
            bool admin;
            try
            {
                admin = _service.ToggleAdmin(ctx.User, toAdmin);
            }
            catch (InvalidOperationException)
            {
                await ReplyErrorLocalizedAsync(strs.club_admin_error);
                return;
            }

            if (admin)
                await ReplyConfirmLocalizedAsync(strs.club_admin_add(Format.Bold(toAdmin.ToString())));
            else
                await ReplyConfirmLocalizedAsync(strs.club_admin_remove(Format.Bold(toAdmin.ToString())));
        }

        [Cmd]
        public async partial Task ClubCreate([Leftover] string clubName)
        {
            if (string.IsNullOrWhiteSpace(clubName) || clubName.Length > 20)
            {
                await ReplyErrorLocalizedAsync(strs.club_name_too_long);
                return;
            }

            if (!_service.CreateClub(ctx.User, clubName, out var club))
            {
                await ReplyErrorLocalizedAsync(strs.club_create_error);
                return;
            }

            await ReplyConfirmLocalizedAsync(strs.club_created(Format.Bold(club.ToString())));
        }

        [Cmd]
        public async partial Task ClubIcon([Leftover] string url = null)
        {
            if ((!Uri.IsWellFormedUriString(url, UriKind.Absolute) && url is not null)
                || !await _service.SetClubIcon(ctx.User.Id, url is null ? null : new Uri(url)))
            {
                await ReplyErrorLocalizedAsync(strs.club_icon_error);
                return;
            }

            await ReplyConfirmLocalizedAsync(strs.club_icon_set);
        }

        private async Task InternalClubInfoAsync(ClubInfo club)
        {
            var lvl = new LevelStats(club.Xp);
            var users = club.Users.OrderByDescending(x =>
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
                                   .AddField(GetText(strs.level_req), club.MinimumLevelReq.ToString(), true)
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
                club.Users.Count,
                10);
        }

        [Cmd]
        [Priority(1)]
        public async partial Task ClubInformation(IUser user = null)
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
        public async partial Task ClubInformation([Leftover] string clubName = null)
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
        public partial Task ClubBans(int page = 1)
        {
            if (--page < 0)
                return Task.CompletedTask;

            var club = _service.GetClubWithBansAndApplications(ctx.User.Id);
            if (club is null)
                return ReplyErrorLocalizedAsync(strs.club_not_exists_owner);

            var bans = club.Bans.Select(x => x.User).ToArray();

            return ctx.SendPaginatedConfirmAsync(page,
                _ =>
                {
                    var toShow = string.Join("\n", bans.Skip(page * 10).Take(10).Select(x => x.ToString()));

                    return _eb.Create()
                              .WithTitle(GetText(strs.club_bans_for(club.ToString())))
                              .WithDescription(toShow)
                              .WithOkColor();
                },
                bans.Length,
                10);
        }


        [Cmd]
        public partial Task ClubApps(int page = 1)
        {
            if (--page < 0)
                return Task.CompletedTask;

            var club = _service.GetClubWithBansAndApplications(ctx.User.Id);
            if (club is null)
                return ReplyErrorLocalizedAsync(strs.club_not_exists_owner);

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
        public async partial Task ClubApply([Leftover] string clubName)
        {
            if (string.IsNullOrWhiteSpace(clubName))
                return;

            if (!_service.GetClubByName(clubName, out var club))
            {
                await ReplyErrorLocalizedAsync(strs.club_not_exists);
                return;
            }

            if (_service.ApplyToClub(ctx.User, club))
                await ReplyConfirmLocalizedAsync(strs.club_applied(Format.Bold(club.ToString())));
            else
                await ReplyErrorLocalizedAsync(strs.club_apply_error);
        }

        [Cmd]
        [Priority(1)]
        public partial Task ClubAccept(IUser user)
            => ClubAccept($"{user.Username}#{user.Discriminator}");

        [Cmd]
        [Priority(0)]
        public async partial Task ClubAccept([Leftover] string userName)
        {
            if (_service.AcceptApplication(ctx.User.Id, userName, out var discordUser))
                await ReplyConfirmLocalizedAsync(strs.club_accepted(Format.Bold(discordUser.ToString())));
            else
                await ReplyErrorLocalizedAsync(strs.club_accept_error);
        }

        [Cmd]
        public async partial Task Clubleave()
        {
            if (_service.LeaveClub(ctx.User))
                await ReplyConfirmLocalizedAsync(strs.club_left);
            else
                await ReplyErrorLocalizedAsync(strs.club_not_in_club);
        }

        [Cmd]
        [Priority(1)]
        public partial Task ClubKick([Leftover] IUser user)
            => ClubKick(user.ToString());

        [Cmd]
        [Priority(0)]
        public partial Task ClubKick([Leftover] string userName)
        {
            if (_service.Kick(ctx.User.Id, userName, out var club))
            {
                return ReplyConfirmLocalizedAsync(strs.club_user_kick(Format.Bold(userName),
                    Format.Bold(club.ToString())));
            }

            return ReplyErrorLocalizedAsync(strs.club_user_kick_fail);
        }

        [Cmd]
        [Priority(1)]
        public partial Task ClubBan([Leftover] IUser user)
            => ClubBan($"{user.Username}#{user.Discriminator}");

        [Cmd]
        [Priority(0)]
        public partial Task ClubBan([Leftover] string userName)
        {
            if (_service.Ban(ctx.User.Id, userName, out var club))
            {
                return ReplyConfirmLocalizedAsync(strs.club_user_banned(Format.Bold(userName),
                    Format.Bold(club.ToString())));
            }

            return ReplyErrorLocalizedAsync(strs.club_user_ban_fail);
        }

        [Cmd]
        [Priority(1)]
        public partial Task ClubUnBan([Leftover] IUser user)
            => ClubUnBan($"{user.Username}#{user.Discriminator}");

        [Cmd]
        [Priority(0)]
        public partial Task ClubUnBan([Leftover] string userName)
        {
            if (_service.UnBan(ctx.User.Id, userName, out var club))
            {
                return ReplyConfirmLocalizedAsync(strs.club_user_unbanned(Format.Bold(userName),
                    Format.Bold(club.ToString())));
            }

            return ReplyErrorLocalizedAsync(strs.club_user_unban_fail);
        }

        [Cmd]
        public async partial Task ClubLevelReq(int level)
        {
            if (_service.ChangeClubLevelReq(ctx.User.Id, level))
                await ReplyConfirmLocalizedAsync(strs.club_level_req_changed(Format.Bold(level.ToString())));
            else
                await ReplyErrorLocalizedAsync(strs.club_level_req_change_error);
        }

        [Cmd]
        public async partial Task ClubDescription([Leftover] string desc = null)
        {
            if (_service.ChangeClubDescription(ctx.User.Id, desc))
                await ReplyConfirmLocalizedAsync(strs.club_desc_updated(Format.Bold(desc ?? "-")));
            else
                await ReplyErrorLocalizedAsync(strs.club_desc_update_failed);
        }

        [Cmd]
        public async partial Task ClubDisband()
        {
            if (_service.Disband(ctx.User.Id, out var club))
                await ReplyConfirmLocalizedAsync(strs.club_disbanded(Format.Bold(club.ToString())));
            else
                await ReplyErrorLocalizedAsync(strs.club_disband_error);
        }

        [Cmd]
        public partial Task ClubLeaderboard(int page = 1)
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