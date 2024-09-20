#nullable disable warnings
using WizBot.Modules.Xp.Services;
using WizBot.Db.Models;
using WizBot.Modules.Patronage;

namespace WizBot.Modules.Xp;

public partial class Xp : WizBotModule<XpService>
{
    public enum Channel
    {
        Channel
    }

    public enum NotifyPlace
    {
        Server = 0,
        Guild = 0,
        Global = 1
    }

    public enum Role
    {
        Role
    }

    public enum Server
    {
        Server
    }

    private readonly DownloadTracker _tracker;
    private readonly ICurrencyProvider _gss;

    public Xp(DownloadTracker tracker, ICurrencyProvider gss)
    {
        _tracker = tracker;
        _gss = gss;
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    public async Task Experience([Leftover] IUser user = null)
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
    public async Task XpNotify()
    {
        var globalSetting = _service.GetNotificationType(ctx.User);
        var serverSetting = _service.GetNotificationType(ctx.User.Id, ctx.Guild.Id);

        var embed = _sender.CreateEmbed()
                           .WithOkColor()
                           .AddField(GetText(strs.xpn_setting_global), GetNotifLocationString(globalSetting))
                           .AddField(GetText(strs.xpn_setting_server), GetNotifLocationString(serverSetting));

        await Response().Embed(embed).SendAsync();
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    public async Task XpNotify(NotifyPlace place, XpNotificationLocation type)
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
    public async Task XpExclude(Server _)
    {
        var ex = _service.ToggleExcludeServer(ctx.Guild.Id);

        if (ex)
            await Response().Confirm(strs.excluded(Format.Bold(ctx.Guild.ToString()))).SendAsync();
        else
            await Response().Confirm(strs.not_excluded(Format.Bold(ctx.Guild.ToString()))).SendAsync();
    }

    [Cmd]
    [UserPerm(GuildPerm.ManageRoles)]
    [RequireContext(ContextType.Guild)]
    public async Task XpExclude(Role _, [Leftover] IRole role)
    {
        var ex = _service.ToggleExcludeRole(ctx.Guild.Id, role.Id);

        if (ex)
            await Response().Confirm(strs.excluded(Format.Bold(role.ToString()))).SendAsync();
        else
            await Response().Confirm(strs.not_excluded(Format.Bold(role.ToString()))).SendAsync();
    }

    [Cmd]
    [UserPerm(GuildPerm.ManageChannels)]
    [RequireContext(ContextType.Guild)]
    public async Task XpExclude(Channel _, [Leftover] IChannel channel = null)
    {
        if (channel is null)
            channel = ctx.Channel;

        var ex = _service.ToggleExcludeChannel(ctx.Guild.Id, channel.Id);

        if (ex)
            await Response().Confirm(strs.excluded(Format.Bold(channel.ToString()))).SendAsync();
        else
            await Response().Confirm(strs.not_excluded(Format.Bold(channel.ToString()))).SendAsync();
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    public async Task XpExclusionList()
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
        await Response()
              .Paginated()
              .Items(lines)
              .PageSize(15)
              .CurrentPage(0)
              .Page((items, _) =>
              {
                  var embed = _sender.CreateEmbed()
                                     .WithTitle(GetText(strs.exclusion_list))
                                     .WithDescription(string.Join('\n', items))
                                     .WithOkColor();

                  return embed;
              })
              .SendAsync();
    }

    [Cmd]
    [WizBotOptions<LbOpts>]
    [Priority(0)]
    [RequireContext(ContextType.Guild)]
    public Task XpLeaderboard(params string[] args)
        => XpLeaderboard(1, args);

    [Cmd]
    [WizBotOptions<LbOpts>]
    [Priority(1)]
    [RequireContext(ContextType.Guild)]
    public async Task XpLeaderboard(int page = 1, params string[] args)
    {
        if (--page < 0 || page > 100)
            return;

        var (opts, _) = OptionsParser.ParseFrom(new LbOpts(), args);

        await ctx.Channel.TriggerTypingAsync();

        var socketGuild = (SocketGuild)ctx.Guild;
        var allCleanUsers = new List<UserXpStats>();
        if (opts.Clean)
        {
            await ctx.Channel.TriggerTypingAsync();
            await _tracker.EnsureUsersDownloadedAsync(ctx.Guild);

            allCleanUsers = (await _service.GetTopUserXps(ctx.Guild.Id, 1000))
                            .Where(user => socketGuild.GetUser(user.UserId) is not null)
                            .ToList();
        }

        var res = opts.Clean
            ? Response()
              .Paginated()
              .Items(allCleanUsers)
            : Response()
              .Paginated()
              .PageItems((curPage) => _service.GetUserXps(ctx.Guild.Id, curPage));

        await res
              .PageSize(9)
              .CurrentPage(page)
              .Page((users, curPage) =>
              {
                  var embed = _sender.CreateEmbed().WithTitle(GetText(strs.server_leaderboard)).WithOkColor();

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
              })
              .SendAsync();
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    public async Task XpGlobalLeaderboard(int page = 1)
    {
        if (--page < 0 || page > 99)
            return;

        await Response()
              .Paginated()
              .PageItems(async curPage => await _service.GetUserXps(curPage))
              .PageSize(9)
              .Page((users, curPage) =>
              {
                  var embed = _sender.CreateEmbed()
                                     .WithOkColor()
                                     .WithTitle(GetText(strs.global_leaderboard));

                  if (!users.Any())
                  {
                      embed.WithDescription("-");
                      return embed;
                  }

                  for (var i = 0; i < users.Count; i++)
                  {
                      var user = users[i];
                      embed.AddField($"#{i + 1 + (curPage * 9)} {user}",
                          $"{GetText(strs.level_x(new LevelStats(users[i].TotalXp).Level))} - {users[i].TotalXp}xp");
                  }

                  return embed;
              })
              .SendAsync();
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    [UserPerm(GuildPerm.Administrator)]
    [Priority(2)]
    public async Task XpAdd(long amount, [Remainder] SocketRole role)
    {
        if (amount == 0)
            return;

        if (role.IsManaged)
            return;

        var count = await _service.AddXpToUsersAsync(ctx.Guild.Id, amount, role.Members.Select(x => x.Id).ToArray());
        await Response()
              .Confirm(
                  strs.xpadd_users(Format.Bold(amount.ToString()), Format.Bold(count.ToString())))
              .SendAsync();
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    [UserPerm(GuildPerm.Administrator)]
    [Priority(3)]
    public async Task XpAdd(int amount, ulong userId)
    {
        if (amount == 0)
            return;

        _service.AddXp(userId, ctx.Guild.Id, amount);
        var usr = ((SocketGuild)ctx.Guild).GetUser(userId)?.ToString() ?? userId.ToString();
        await Response().Confirm(strs.modified(Format.Bold(usr), Format.Bold(amount.ToString()))).SendAsync();
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    [UserPerm(GuildPerm.Administrator)]
    [Priority(4)]
    public Task XpAdd(int amount, [Leftover] IGuildUser user)
        => XpAdd(amount, user.Id);

    [Cmd]
    [RequireContext(ContextType.Guild)]
    [OwnerOnly]
    public async Task XpTemplateReload()
    {
        _service.ReloadXpTemplate();
        await Task.Delay(1000);
        await Response().Confirm(strs.template_reloaded).SendAsync();
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    [UserPerm(GuildPerm.Administrator)]
    public Task XpReset(IGuildUser user)
        => XpReset(user.Id);

    [Cmd]
    [RequireContext(ContextType.Guild)]
    [UserPerm(GuildPerm.Administrator)]
    public async Task XpReset(ulong userId)
    {
        var embed = _sender.CreateEmbed()
                           .WithTitle(GetText(strs.reset))
                           .WithDescription(GetText(strs.reset_user_confirm));

        if (!await PromptUserConfirmAsync(embed))
            return;

        _service.XpReset(ctx.Guild.Id, userId);

        await Response().Confirm(strs.reset_user(userId)).SendAsync();
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    [UserPerm(GuildPerm.Administrator)]
    public async Task XpReset()
    {
        var embed = _sender.CreateEmbed()
                           .WithTitle(GetText(strs.reset))
                           .WithDescription(GetText(strs.reset_server_confirm));

        if (!await PromptUserConfirmAsync(embed))
            return;

        _service.XpReset(ctx.Guild.Id);

        await Response().Confirm(strs.reset_server).SendAsync();
    }

    public enum XpShopInputType
    {
        Backgrounds = 0,
        B = 0,
        Bg = 0,
        Bgs = 0,
        Frames = 1,
        F = 1,
        Fr = 1,
        Frs = 1,
        Fs = 1,
    }

    [Cmd]
    public async Task XpShop()
    {
        if (!_service.IsShopEnabled())
        {
            await Response().Error(strs.xp_shop_disabled).SendAsync();
            return;
        }

        await Response()
              .Confirm(GetText(strs.available_commands),
                  $"""
                   `{prefix}xpshop bgs`
                   `{prefix}xpshop frames`

                   *{GetText(strs.xpshop_website)}*
                   """)
              .SendAsync();
    }

    [Cmd]
    public async Task XpShop(XpShopInputType type, int page = 1)
    {
        --page;

        if (page < 0)
            return;

        var allItems = type == XpShopInputType.Backgrounds
            ? await _service.GetShopBgs()
            : await _service.GetShopFrames();

        if (allItems is null)
        {
            await Response().Error(strs.xp_shop_disabled).SendAsync();
            return;
        }

        if (allItems.Count == 0)
        {
            await Response().Error(strs.not_found).SendAsync();
            return;
        }

        await Response()
              .Paginated()
              .Items(allItems)
              .PageSize(1)
              .CurrentPage(page)
              .AddFooter(false)
              .Page((items, _) =>
              {
                  if (!items.Any())
                      return _sender.CreateEmbed()
                                    .WithDescription(GetText(strs.not_found))
                                    .WithErrorColor();

                  var (key, item) = items.FirstOrDefault();

                  var eb = _sender.CreateEmbed()
                                  .WithOkColor()
                                  .WithTitle(item.Name)
                                  .AddField(GetText(strs.price),
                                      CurrencyHelper.N(item.Price, Culture, _gss.GetCurrencySign()),
                                      true)
                                  .WithImageUrl(string.IsNullOrWhiteSpace(item.Preview)
                                      ? item.Url
                                      : item.Preview);

                  if (!string.IsNullOrWhiteSpace(item.Desc))
                      eb.AddField(GetText(strs.desc), item.Desc);
                  
                  if (!string.IsNullOrWhiteSpace(item.Author))
                      eb.AddField(GetText(strs.author), item.Author);

#if GLOBAL_WIZBOT
                  if (key == "default")
                      eb.WithDescription(GetText(strs.xpshop_website));
#endif

                  var tier = _service.GetXpShopTierRequirement(type);
                  if (tier != PatronTier.None)
                  {
                      eb.WithFooter(GetText(strs.xp_shop_buy_required_tier(tier.ToString())));
                  }

                  return eb;
              })
              .Interaction(async current =>
              {
                  var (key, _) = allItems.Skip(current).First();

                  var itemType = type == XpShopInputType.Backgrounds
                      ? XpShopItemType.Background
                      : XpShopItemType.Frame;

                  var ownedItem = await _service.GetUserItemAsync(ctx.User.Id, itemType, key);
                  if (ownedItem is not null)
                  {
                      var button = new ButtonBuilder(ownedItem.IsUsing
                              ? GetText(strs.in_use)
                              : GetText(strs.use),
                          "xpshop:use",
                          emote: Emoji.Parse("👐"),
                          isDisabled: ownedItem.IsUsing);

                      var inter = _inter.Create(
                          ctx.User.Id,
                          button,
                          OnShopUse,
                          (key, itemType),
                          clearAfter: false);

                      return inter;
                  }
                  else
                  {
                      var button = new ButtonBuilder(GetText(strs.buy),
                          "xpshop:buy",
                          emote: Emoji.Parse("💰"));

                      var inter = _inter.Create(
                          ctx.User.Id,
                          button,
                          OnShopBuy,
                          (key, itemType),
                          singleUse: true,
                          clearAfter: false);

                      return inter;
                  }
              })
              .SendAsync();
    }

    [Cmd]
    public async Task XpShopBuy(XpShopInputType type, string key)
    {
        var result = await _service.BuyShopItemAsync(ctx.User.Id, (XpShopItemType)type, key);

        WizBotInteractionBase GetUseInteraction()
        {
            return _inter.Create(ctx.User.Id,
                new(label: "Use", customId: "xpshop:use_item", emote: Emoji.Parse("👐")),
                async (_, state) => await XpShopUse(state.type, state.key),
                (type, key)
            );
        }

        if (result != BuyResult.Success)
        {
            var _ = result switch
            {
                BuyResult.XpShopDisabled => await Response().Error(strs.xp_shop_disabled).SendAsync(),
                BuyResult.InsufficientFunds => await Response()
                                                     .Error(strs.not_enough(_gss.GetCurrencySign()))
                                                     .SendAsync(),
                BuyResult.AlreadyOwned =>
                    await Response().Error(strs.xpshop_already_owned).Interaction(GetUseInteraction()).SendAsync(),
                BuyResult.UnknownItem => await Response().Error(strs.xpshop_item_not_found).SendAsync(),
                BuyResult.InsufficientPatronTier => await Response().Error(strs.patron_insuff_tier).SendAsync(),
                _ => throw new ArgumentOutOfRangeException()
            };
            return;
        }

        await Response()
              .Confirm(strs.xpshop_buy_success(type.ToString().ToLowerInvariant(),
                  key.ToLowerInvariant()))
              .Interaction(GetUseInteraction())
              .SendAsync();
    }

    [Cmd]
    public async Task XpShopUse(XpShopInputType type, string key)
    {
        var result = await _service.UseShopItemAsync(ctx.User.Id, (XpShopItemType)type, key);

        if (!result)
        {
            await Response().Confirm(strs.xp_shop_item_cant_use).SendAsync();
            return;
        }

        await ctx.OkAsync();
    }

    private async Task OnShopUse(SocketMessageComponent smc, (string key, XpShopItemType type) state)
    {
        var (key, type) = state;

        var result = await _service.UseShopItemAsync(ctx.User.Id, type, key);


        if (!result)
        {
            await Response().Confirm(strs.xp_shop_item_cant_use).SendAsync();
        }
    }

    private async Task OnShopBuy(SocketMessageComponent smc, (string key, XpShopItemType type) state)
    {
        var (key, type) = state;

        var result = await _service.BuyShopItemAsync(ctx.User.Id, type, key);

        if (result == BuyResult.InsufficientFunds)
        {
            await Response().Error(strs.not_enough(_gss.GetCurrencySign())).SendAsync();
        }
        else if (result == BuyResult.Success)
        {
            await _service.UseShopItemAsync(ctx.User.Id, type, key);
        }
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