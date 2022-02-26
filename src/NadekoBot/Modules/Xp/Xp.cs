#nullable disable
using NadekoBot.Modules.Gambling.Services;
using NadekoBot.Modules.Xp.Services;
using NadekoBot.Services.Database.Models;

namespace NadekoBot.Modules.Xp;

public partial class Xp : NadekoModule<XpService>
{
    public enum Channel { Channel }

    public enum NotifyPlace
    {
        Server = 0,
        Guild = 0,
        Global = 1
    }

    public enum Role { Role }

    public enum Server { Server }

    private readonly DownloadTracker _tracker;
    private readonly GamblingConfigService _gss;

    public Xp(DownloadTracker tracker, GamblingConfigService gss)
    {
        _tracker = tracker;
        _gss = gss;
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    public async partial Task Experience([Leftover] IUser user = null)
    {
        user ??= ctx.User;
        await ctx.Channel.TriggerTypingAsync();
        var (img, fmt) = await _service.GenerateXpImageAsync((IGuildUser)user);
        await using (img)
        {
            await ctx.Channel.SendFileAsync(img, $"{ctx.Guild.Id}_{user.Id}_xp.{fmt.FileExtensions.FirstOrDefault()}");
        }
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    public async partial Task XpNotify()
    {
        var globalSetting = _service.GetNotificationType(ctx.User);
        var serverSetting = _service.GetNotificationType(ctx.User.Id, ctx.Guild.Id);

        var embed = _eb.Create()
                       .WithOkColor()
                       .AddField(GetText(strs.xpn_setting_global), GetNotifLocationString(globalSetting))
                       .AddField(GetText(strs.xpn_setting_server), GetNotifLocationString(serverSetting));

        await ctx.Channel.EmbedAsync(embed);
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    public async partial Task XpNotify(NotifyPlace place, XpNotificationLocation type)
    {
        if (place == NotifyPlace.Guild)
            await _service.ChangeNotificationType(ctx.User.Id, ctx.Guild.Id, type);
        else
            await _service.ChangeNotificationType(ctx.User, type);

        await ctx.OkAsync();
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    [UserPerm(GuildPerm.Administrator)]
    public async partial Task XpExclude(Server _)
    {
        var ex = _service.ToggleExcludeServer(ctx.Guild.Id);

        if (ex)
            await ReplyConfirmLocalizedAsync(strs.excluded(Format.Bold(ctx.Guild.ToString())));
        else
            await ReplyConfirmLocalizedAsync(strs.not_excluded(Format.Bold(ctx.Guild.ToString())));
    }

    [Cmd]
    [UserPerm(GuildPerm.ManageRoles)]
    [RequireContext(ContextType.Guild)]
    public async partial Task XpExclude(Role _, [Leftover] IRole role)
    {
        var ex = _service.ToggleExcludeRole(ctx.Guild.Id, role.Id);

        if (ex)
            await ReplyConfirmLocalizedAsync(strs.excluded(Format.Bold(role.ToString())));
        else
            await ReplyConfirmLocalizedAsync(strs.not_excluded(Format.Bold(role.ToString())));
    }

    [Cmd]
    [UserPerm(GuildPerm.ManageChannels)]
    [RequireContext(ContextType.Guild)]
    public async partial Task XpExclude(Channel _, [Leftover] IChannel channel = null)
    {
        if (channel is null)
            channel = ctx.Channel;

        var ex = _service.ToggleExcludeChannel(ctx.Guild.Id, channel.Id);

        if (ex)
            await ReplyConfirmLocalizedAsync(strs.excluded(Format.Bold(channel.ToString())));
        else
            await ReplyConfirmLocalizedAsync(strs.not_excluded(Format.Bold(channel.ToString())));
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    public async partial Task XpExclusionList()
    {
        var serverExcluded = _service.IsServerExcluded(ctx.Guild.Id);
        var roles = _service.GetExcludedRoles(ctx.Guild.Id)
                            .Select(x => ctx.Guild.GetRole(x))
                            .Where(x => x is not null)
                            .Select(x => $"`role`   {x.Mention}")
                            .ToList();

        var chans = (await _service.GetExcludedChannels(ctx.Guild.Id)
                                   .Select(x => ctx.Guild.GetChannelAsync(x))
                                   .WhenAll()).Where(x => x is not null)
                                              .Select(x => $"`channel` <#{x.Id}>")
                                              .ToList();

        var rolesStr = roles.Any() ? string.Join("\n", roles) + "\n" : string.Empty;
        var chansStr = chans.Count > 0 ? string.Join("\n", chans) + "\n" : string.Empty;
        var desc = Format.Code(serverExcluded
            ? GetText(strs.server_is_excluded)
            : GetText(strs.server_is_not_excluded));

        desc += "\n\n" + rolesStr + chansStr;

        var lines = desc.Split('\n');
        await ctx.SendPaginatedConfirmAsync(0,
            curpage =>
            {
                var embed = _eb.Create()
                               .WithTitle(GetText(strs.exclusion_list))
                               .WithDescription(string.Join('\n', lines.Skip(15 * curpage).Take(15)))
                               .WithOkColor();

                return embed;
            },
            lines.Length,
            15);
    }

    [Cmd]
    [NadekoOptions(typeof(LbOpts))]
    [Priority(0)]
    [RequireContext(ContextType.Guild)]
    public partial Task XpLeaderboard(params string[] args)
        => XpLeaderboard(1, args);

    [Cmd]
    [NadekoOptions(typeof(LbOpts))]
    [Priority(1)]
    [RequireContext(ContextType.Guild)]
    public async partial Task XpLeaderboard(int page = 1, params string[] args)
    {
        if (--page < 0 || page > 100)
            return;

        var (opts, _) = OptionsParser.ParseFrom(new LbOpts(), args);

        await ctx.Channel.TriggerTypingAsync();

        var socketGuild = (SocketGuild)ctx.Guild;
        var allUsers = new List<UserXpStats>();
        if (opts.Clean)
        {
            await ctx.Channel.TriggerTypingAsync();
            await _tracker.EnsureUsersDownloadedAsync(ctx.Guild);

            allUsers = _service.GetTopUserXps(ctx.Guild.Id, 1000)
                               .Where(user => socketGuild.GetUser(user.UserId) is not null)
                               .ToList();
        }

        await ctx.SendPaginatedConfirmAsync(page,
            curPage =>
            {
                var embed = _eb.Create().WithTitle(GetText(strs.server_leaderboard)).WithOkColor();

                List<UserXpStats> users;
                if (opts.Clean)
                    users = allUsers.Skip(curPage * 9).Take(9).ToList();
                else
                    users = _service.GetUserXps(ctx.Guild.Id, curPage);

                if (!users.Any())
                    return embed.WithDescription("-");

                for (var i = 0; i < users.Count; i++)
                {
                    var levelStats = new LevelStats(users[i].Xp + users[i].AwardedXp);
                    var user = ((SocketGuild)ctx.Guild).GetUser(users[i].UserId);

                    var userXpData = users[i];

                    var awardStr = string.Empty;
                    if (userXpData.AwardedXp > 0)
                        awardStr = $"(+{userXpData.AwardedXp})";
                    else if (userXpData.AwardedXp < 0)
                        awardStr = $"({userXpData.AwardedXp})";

                    embed.AddField($"#{i + 1 + (curPage * 9)} {user?.ToString() ?? users[i].UserId.ToString()}",
                        $"{GetText(strs.level_x(levelStats.Level))} - {levelStats.TotalXp}xp {awardStr}");
                }

                return embed;
            },
            900,
            9,
            false);
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    public async partial Task XpGlobalLeaderboard(int page = 1)
    {
        if (--page < 0 || page > 99)
            return;
        var users = _service.GetUserXps(page);

        var embed = _eb.Create().WithTitle(GetText(strs.global_leaderboard)).WithOkColor();

        if (!users.Any())
            embed.WithDescription("-");
        else
        {
            for (var i = 0; i < users.Length; i++)
            {
                var user = users[i];
                embed.AddField($"#{i + 1 + (page * 9)} {user.ToString()}",
                    $"{GetText(strs.level_x(new LevelStats(users[i].TotalXp).Level))} - {users[i].TotalXp}xp");
            }
        }

        await ctx.Channel.EmbedAsync(embed);
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    [UserPerm(GuildPerm.Administrator)]
    public async partial Task XpAdd(int amount, ulong userId)
    {
        if (amount == 0)
            return;

        _service.AddXp(userId, ctx.Guild.Id, amount);
        var usr = ((SocketGuild)ctx.Guild).GetUser(userId)?.ToString() ?? userId.ToString();
        await ReplyConfirmLocalizedAsync(strs.modified(Format.Bold(usr), Format.Bold(amount.ToString())));
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    [UserPerm(GuildPerm.Administrator)]
    public partial Task XpAdd(int amount, [Leftover] IGuildUser user)
        => XpAdd(amount, user.Id);

    [Cmd]
    [RequireContext(ContextType.Guild)]
    [OwnerOnly]
    public async partial Task XpTemplateReload()
    {
        _service.ReloadXpTemplate();
        await Task.Delay(1000);
        await ReplyConfirmLocalizedAsync(strs.template_reloaded);
    }
    
    [Cmd]
    [RequireContext(ContextType.Guild)]
    [UserPerm(GuildPerm.Administrator)]
    public partial Task XpReset(IGuildUser user)
        => XpReset(user.Id);

    [Cmd]
    [RequireContext(ContextType.Guild)]
    [UserPerm(GuildPerm.Administrator)]
    public async partial Task XpReset(ulong userId)
    {
        var embed = _eb.Create().WithTitle(GetText(strs.reset)).WithDescription(GetText(strs.reset_user_confirm));

        if (!await PromptUserConfirmAsync(embed))
            return;

        _service.XpReset(ctx.Guild.Id, userId);

        await ReplyConfirmLocalizedAsync(strs.reset_user(userId));
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    [UserPerm(GuildPerm.Administrator)]
    public async partial Task XpReset()
    {
        var embed = _eb.Create().WithTitle(GetText(strs.reset)).WithDescription(GetText(strs.reset_server_confirm));

        if (!await PromptUserConfirmAsync(embed))
            return;

        _service.XpReset(ctx.Guild.Id);

        await ReplyConfirmLocalizedAsync(strs.reset_server);
    }

    private string GetNotifLocationString(XpNotificationLocation loc)
    {
        if (loc == XpNotificationLocation.Channel)
            return GetText(strs.xpn_notif_channel);

        if (loc == XpNotificationLocation.Dm)
            return GetText(strs.xpn_notif_dm);

        return GetText(strs.xpn_notif_disabled);
    }
}