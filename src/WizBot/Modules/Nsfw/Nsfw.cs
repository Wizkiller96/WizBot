#nullable disable
using WizBot.Modules.Searches.Common;
using Newtonsoft.Json.Linq;

namespace WizBot.Modules.Nsfw;

public partial class NSFW : WizBotModule<ISearchImagesService>
{
    private static readonly ConcurrentHashSet<ulong> _hentaiBombBlacklist = new();
    private readonly IHttpClientFactory _httpFactory;
    private readonly WizBotRandom _rng;

    public NSFW(IHttpClientFactory factory)
    {
        _httpFactory = factory;
        _rng = new();
    }

    private async Task InternalBoobs()
    {
        try
        {
            JToken obj;
            using (var http = _httpFactory.CreateClient())
            {
                obj = JArray.Parse(
                    await http.GetStringAsync($"http://api.oboobs.ru/boobs/{new WizBotRandom().Next(0, 10330)}"))[0];
            }

            await ctx.Channel.SendMessageAsync($"http://media.oboobs.ru/{obj["preview"]}");
        }
        catch (Exception ex)
        {
            await SendErrorAsync(ex.Message);
        }
    }

    private async Task InternalButts(IMessageChannel channel)
    {
        try
        {
            JToken obj;
            using (var http = _httpFactory.CreateClient())
            {
                obj = JArray.Parse(
                    await http.GetStringAsync($"http://api.obutts.ru/butts/{new WizBotRandom().Next(0, 4335)}"))[0];
            }

            await channel.SendMessageAsync($"http://media.obutts.ru/{obj["preview"]}");
        }
        catch (Exception ex)
        {
            await SendErrorAsync(ex.Message);
        }
    }
    
    private async Task InternalNeko(IMessageChannel Channel, string format = "img", string category = "all_tags_lewd")
        {
            // List if category to pull an image from.
            string[] img_cat = { "pantyhose_lewd", "holo_lewd", "anus_lewd", "kemonomimi_lewd", "peeing_lewd", "cosplay_lewd", "futanari_lewd", "blowjob_lewd", "shinobu_ero", "shinobu_lewd", "kitsune_lewd", "all_tags_lewd", "kemonomimi_ero", "wallpaper_lewd", "feet_ero", "anal_lewd", "femdom_lewd", "kitsune_ero", "solo_lewd", "holo_ero", "yuri_lewd", "feet_lewd", "classic_lewd", "keta_lewd", "neko_lewd", "piersing_lewd", "trap_lewd", "pantyhose_ero", "yiff_lewd", "hplay_ero", "smallboobs_lewd", "neko_ero", "pussy_lewd", "cum_lewd", "keta_avatar", "ero_wallpaper_ero", "ahegao_avatar", "piersing_ero", "bdsm_lewd", "holo_avatar", "all_tags_ero", "tits_lewd", "yuri_ero" };

            string[] gif_cat = { "yiff", "pussy_wank", "neko", "kuni", "blow_job", "pussy", "girls_solo", "yuri", "anal", "tits", "classic", "feet", "spank", "cum", "all_tags" };

            // Check to see if the command is calling for a normal image or a gif.
            string[] img_format = { "img", "gif" };

            if (string.IsNullOrWhiteSpace(category))
                return;

            if (string.IsNullOrWhiteSpace(format))
                return;

            try
            {
                JToken nekotitle;
                JToken nekoimg;
                using (var http = _httpFactory.CreateClient())
                {
                    nekotitle = JObject.Parse(await http.GetStringAsync($"https://api.nekos.dev/api/v3/text/cat_emote/").ConfigureAwait(false));
                    nekoimg = JObject.Parse(await http.GetStringAsync($"https://api.nekos.dev/api/v3/images/nsfw/{format}/{category}/").ConfigureAwait(false));
                }
                if (img_format.Contains("img") && img_cat.Contains(category))
                    await Channel.EmbedAsync(_eb.Create().WithOkColor()
                        .WithAuthor($"Nekos Life - NSFW IMG Database {nekotitle["data"]["response"]["text"]}",
                            "https://i.imgur.com/a36AMkG.png",
                            "http://nekos.life/")
                        .WithImageUrl($"{nekoimg["data"]["response"]["url"]}")).ConfigureAwait(false);
                else if (img_format.Contains("gif") && gif_cat.Contains(category))
                    await Channel.EmbedAsync(_eb.Create().WithOkColor()
                        .WithAuthor($"Nekos Life - NSFW GIF Database {nekotitle["data"]["response"]["text"]}",
                            "https://i.imgur.com/a36AMkG.png",
                            "http://nekos.life/")
                        .WithImageUrl($"{nekoimg["data"]["response"]["url"]}")).ConfigureAwait(false);
                else if (img_format.Contains("img") && gif_cat.Contains(category))
                    await Channel.EmbedAsync(_eb.Create().WithErrorColor()
                    .WithAuthor("Nekos Life - Invalid NSFW IMG Category","https://i.imgur.com/a36AMkG.png","http://nekos.life/")
                    .WithDescription("Seems the image category you was looking for could not be found. Please use the categories listed below.")
                    .AddField("NSFW IMG Categories", "`classic_lewd`, `piersing_lewd`, `shinobu_lewd`, `feet_lewd`, `keta_avatar`, `piersing_ero`, `yuri_ero`, `solo_lewd`, `pantyhose_lewd`, `kemonomimi_lewd`, `cosplay_lewd`, `peeing_lewd`, `ahegao_avatar`, `wallpaper_lewd`, `ero_wallpaper_ero`, `blowjob_lewd`, `holo_avatar`, `neko_lewd`, `futanari_lewd`, `kitsune_ero`, `trap_lewd`, `keta_lewd`, `neko_ero`, `pantyhose_ero`, `cum_lewd`, `anal_lewd`, `smallboobs_lewd`, `all_tags_lewd`, `yuri_lewd`, `kemonomimi_ero`, `anus_lewd`, `holo_ero`, `all_tags_ero`, `kitsune_lewd`, `pussy_lewd`, `feet_ero`, `yiff_lewd`, `hplay_ero`, `bdsm_lewd`, `femdom_lewd`, `holo_lewd`, `shinobu_ero`, `tits_lewd`", false)).ConfigureAwait(false);
                else if (img_format.Contains("gif") && img_cat.Contains(category))
                    await Channel.EmbedAsync(_eb.Create().WithErrorColor()
                    .WithAuthor("Nekos Life - Invalid NSFW GIF Category",
                        "https://i.imgur.com/a36AMkG.png",
                        "http://nekos.life/")
                    .WithDescription("Seems the gif category you was looking for could not be found. Please use the categories listed below.")
                    .AddField("NSFW GIF Categories", "`blow_job`, `pussy_wank`, `classic`, `kuni`, `tits`, `pussy`, `cum`, `spank`, `feet`, `all_tags`, `yuri`, `anal`, `neko`, `girls_solo`, `yiff`", false)).ConfigureAwait(false);
                else
                    await Channel.EmbedAsync(_eb.Create().WithErrorColor()
                    .WithAuthor("Nekos Life - Invalid NSFW Image Type or Category",
                        "https://i.imgur.com/a36AMkG.png",
                        "http://nekos.life/")
                    .WithDescription("Seems the image type or category you was looking for could not be found. Please use the image type or categories listed below.")
                    .AddField("NSFW IMG Types", "`img`, `gif`", false)
                    .AddField("NSFW IMG Categories", "`classic_lewd`, `piersing_lewd`, `shinobu_lewd`, `feet_lewd`, `keta_avatar`, `piersing_ero`, `yuri_ero`, `solo_lewd`, `pantyhose_lewd`, `kemonomimi_lewd`, `cosplay_lewd`, `peeing_lewd`, `ahegao_avatar`, `wallpaper_lewd`, `ero_wallpaper_ero`, `blowjob_lewd`, `holo_avatar`, `neko_lewd`, `futanari_lewd`, `kitsune_ero`, `trap_lewd`, `keta_lewd`, `neko_ero`, `pantyhose_ero`, `cum_lewd`, `anal_lewd`, `smallboobs_lewd`, `all_tags_lewd`, `yuri_lewd`, `kemonomimi_ero`, `anus_lewd`, `holo_ero`, `all_tags_ero`, `kitsune_lewd`, `pussy_lewd`, `feet_ero`, `yiff_lewd`, `hplay_ero`, `bdsm_lewd`, `femdom_lewd`, `holo_lewd`, `shinobu_ero`, `tits_lewd`", false)
                    .AddField("NSFW GIF Categories", "`blow_job`, `pussy_wank`, `classic`, `kuni`, `tits`, `pussy`, `cum`, `spank`, `feet`, `all_tags`, `yuri`, `anal`, `neko`, `girls_solo`, `yiff`", false)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await SendErrorAsync(ex.Message).ConfigureAwait(false);
            }
        }

    [Cmd]
    [RequireNsfw]
    [RequireContext(ContextType.Guild)]
    [UserPerm(ChannelPerm.ManageMessages)]
    public async partial Task AutoHentai(int interval = 0, [Leftover] string tags = null)
    {
        Timer t;

        if (interval == 0)
        {
            if (!_service.AutoHentaiTimers.TryRemove(ctx.Channel.Id, out t))
                return;

            t.Change(Timeout.Infinite, Timeout.Infinite); //proper way to disable the timer
            await ReplyConfirmLocalizedAsync(strs.stopped);
            return;
        }

        if (interval < 20)
            return;

        t = new(async _ =>
            {
                try
                {
                    if (tags is null || tags.Length == 0)
                        await InternalDapiCommand(null, true, _service.Hentai);
                    else
                    {
                        var groups = tags.Split('|');
                        var group = groups[_rng.Next(0, groups.Length)];
                        await InternalDapiCommand(group.Split(' '), true, _service.Hentai);
                    }
                }
                catch
                {
                    // ignored
                }
            },
            null,
            interval * 1000,
            interval * 1000);

        _service.AutoHentaiTimers.AddOrUpdate(ctx.Channel.Id,
            t,
            (_, old) =>
            {
                old.Change(Timeout.Infinite, Timeout.Infinite);
                return t;
            });

        await ReplyConfirmLocalizedAsync(strs.autohentai_started(interval, string.Join(", ", tags)));
    }

    [Cmd]
    [RequireNsfw]
    [RequireContext(ContextType.Guild)]
    [UserPerm(ChannelPerm.ManageMessages)]
    public async partial Task AutoBoobs(int interval = 0)
    {
        Timer t;

        if (interval == 0)
        {
            if (!_service.AutoBoobTimers.TryRemove(ctx.Channel.Id, out t))
                return;

            t.Change(Timeout.Infinite, Timeout.Infinite);
            await ReplyConfirmLocalizedAsync(strs.stopped);
            return;
        }

        if (interval < 20)
            return;

        t = new(async _ =>
            {
                try
                {
                    await InternalBoobs();
                }
                catch
                {
                    // ignored
                }
            },
            null,
            interval * 1000,
            interval * 1000);

        _service.AutoBoobTimers.AddOrUpdate(ctx.Channel.Id,
            t,
            (_, old) =>
            {
                old.Change(Timeout.Infinite, Timeout.Infinite);
                return t;
            });

        await ReplyConfirmLocalizedAsync(strs.started(interval));
    }

    [Cmd]
    [RequireNsfw(Group = "nsfw_or_dm")]
    [RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
    [UserPerm(ChannelPerm.ManageMessages)]
    public async partial Task AutoButts(int interval = 0)
    {
        Timer t;

        if (interval == 0)
        {
            if (!_service.AutoButtTimers.TryRemove(ctx.Channel.Id, out t))
                return;

            t.Change(Timeout.Infinite, Timeout.Infinite); //proper way to disable the timer
            await ReplyConfirmLocalizedAsync(strs.stopped);
            return;
        }

        if (interval < 20)
            return;

        t = new(async _ =>
            {
                try
                {
                    await InternalButts(ctx.Channel);
                }
                catch
                {
                    // ignored
                }
            },
            null,
            interval * 1000,
            interval * 1000);

        _service.AutoButtTimers.AddOrUpdate(ctx.Channel.Id,
            t,
            (_, old) =>
            {
                old.Change(Timeout.Infinite, Timeout.Infinite);
                return t;
            });

        await ReplyConfirmLocalizedAsync(strs.started(interval));
    }

    [Cmd]
    [RequireNsfw(Group = "nsfw_or_dm")]
    [RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
    public partial Task Hentai(params string[] tags)
        => InternalDapiCommand(tags, true, _service.Hentai);

    [Cmd]
    [RequireNsfw(Group = "nsfw_or_dm")]
    [RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
    public async partial Task HentaiBomb(params string[] tags)
    {
        if (!_hentaiBombBlacklist.Add(ctx.Guild?.Id ?? ctx.User.Id))
            return;
        try
        {
            var images = await Task.WhenAll(_service.Yandere(ctx.Guild?.Id, true, tags),
                _service.Danbooru(ctx.Guild?.Id, true, tags),
                _service.Konachan(ctx.Guild?.Id, true, tags),
                _service.Gelbooru(ctx.Guild?.Id, true, tags));

            var linksEnum = images.Where(l => l is not null).ToArray();
            if (!linksEnum.Any())
            {
                await ReplyErrorLocalizedAsync(strs.no_results);
                return;
            }

            await ctx.Channel.SendMessageAsync(string.Join("\n\n", linksEnum.Select(x => x.Url)));
        }
        finally
        {
            _hentaiBombBlacklist.TryRemove(ctx.Guild?.Id ?? ctx.User.Id);
        }
    }

    [Cmd]
    [RequireNsfw(Group = "nsfw_or_dm")]
    [RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
    public partial Task Yandere(params string[] tags)
        => InternalDapiCommand(tags, false, _service.Yandere);

    [Cmd]
    [RequireNsfw(Group = "nsfw_or_dm")]
    [RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
    public partial Task Konachan(params string[] tags)
        => InternalDapiCommand(tags, false, _service.Konachan);

    [Cmd]
    [RequireNsfw(Group = "nsfw_or_dm")]
    [RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
    public partial Task Sankaku(params string[] tags)
        => InternalDapiCommand(tags, false, _service.Sankaku);

    [Cmd]
    [RequireNsfw(Group = "nsfw_or_dm")]
    [RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
    public partial Task E621(params string[] tags)
        => InternalDapiCommand(tags, false, _service.E621);

    [Cmd]
    [RequireNsfw(Group = "nsfw_or_dm")]
    [RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
    public partial Task Rule34(params string[] tags)
        => InternalDapiCommand(tags, false, _service.Rule34);

    [Cmd]
    [RequireNsfw(Group = "nsfw_or_dm")]
    [RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
    public partial Task Danbooru(params string[] tags)
        => InternalDapiCommand(tags, false, _service.Danbooru);

    [Cmd]
    [RequireNsfw(Group = "nsfw_or_dm")]
    [RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
    public partial Task Gelbooru(params string[] tags)
        => InternalDapiCommand(tags, false, _service.Gelbooru);

    [Cmd]
    [RequireNsfw(Group = "nsfw_or_dm")]
    [RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
    public partial Task Derpibooru(params string[] tags)
        => InternalDapiCommand(tags, false, _service.DerpiBooru);

    [Cmd]
    [RequireNsfw(Group = "nsfw_or_dm")]
    [RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
    public partial Task Safebooru(params string[] tags)
        => InternalDapiCommand(tags, false, _service.SafeBooru);

    [Cmd]
    [RequireNsfw(Group = "nsfw_or_dm")]
    [RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
    public async partial Task Boobs()
    {
        try
        {
            JToken obj;
            using (var http = _httpFactory.CreateClient())
            {
                obj = JArray.Parse(
                    await http.GetStringAsync($"http://api.oboobs.ru/boobs/{new WizBotRandom().Next(0, 12000)}"))[0];
            }

            await ctx.Channel.SendMessageAsync($"http://media.oboobs.ru/{obj["preview"]}");
        }
        catch (Exception ex)
        {
            await SendErrorAsync(ex.Message);
        }
    }

    [Cmd]
    [RequireNsfw(Group = "nsfw_or_dm")]
    [RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
    public async partial Task Butts()
    {
        try
        {
            JToken obj;
            using (var http = _httpFactory.CreateClient())
            {
                obj = JArray.Parse(
                    await http.GetStringAsync($"http://api.obutts.ru/butts/{new WizBotRandom().Next(0, 6100)}"))[0];
            }

            await ctx.Channel.SendMessageAsync($"http://media.obutts.ru/{obj["preview"]}");
        }
        catch (Exception ex)
        {
            await SendErrorAsync(ex.Message);
        }
    }
    
    [Cmd]
        [RequireNsfw(Group = "nsfw_or_dm"), RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
        public async partial Task Neko(string format = "img", [Remainder] string category = "neko_lewd")
        {
            // List if category to pull an image from.
            string[] img_cat = { "classic_lewd", "piersing_lewd", "shinobu_lewd", "feet_lewd", "keta_avatar", "piersing_ero", "yuri_ero", "solo_lewd", "pantyhose_lewd", "kemonomimi_lewd", "cosplay_lewd", "peeing_lewd", "ahegao_avatar", "wallpaper_lewd", "ero_wallpaper_ero", "blowjob_lewd", "holo_avatar", "neko_lewd", "futanari_lewd", "kitsune_ero", "trap_lewd", "keta_lewd", "neko_ero", "pantyhose_ero", "cum_lewd", "anal_lewd", "smallboobs_lewd", "all_tags_lewd", "yuri_lewd", "kemonomimi_ero", "anus_lewd", "holo_ero", "all_tags_ero", "kitsune_lewd", "pussy_lewd", "feet_ero", "yiff_lewd", "hplay_ero", "bdsm_lewd", "femdom_lewd", "holo_lewd", "shinobu_ero", "tits_lewd" };

            string[] gif_cat = { "blow_job", "pussy_wank", "classic", "kuni", "tits", "pussy", "cum", "spank", "feet", "all_tags", "yuri", "anal", "neko", "girls_solo", "yiff" };

            // Check to see if the command is calling for a normal image or a gif.
            string[] img_format = { "img", "gif" };

            if (string.IsNullOrWhiteSpace(category))
                return;

            if (string.IsNullOrWhiteSpace(format))
                return;

            try
            {
                JToken nekotitle;
                JToken nekoimg;
                using (var http = _httpFactory.CreateClient())
                {
                    nekotitle = JObject.Parse(await http.GetStringAsync($"https://api.nekos.dev/api/v3/text/cat_emote/").ConfigureAwait(false));
                    nekoimg = JObject.Parse(await http.GetStringAsync($"https://api.nekos.dev/api/v3/images/nsfw/{format}/{category}/").ConfigureAwait(false));
                }
                if (img_format.Contains("img") && img_cat.Contains(category))
                    await ctx.Channel.EmbedAsync(_eb.Create().WithOkColor()
                        .WithAuthor($"Nekos Life - NSFW IMG Database {nekotitle["data"]["response"]["text"]}",
                            "https://i.imgur.com/a36AMkG.png",
                            "http://nekos.life/")
                        .WithImageUrl($"{nekoimg["data"]["response"]["url"]}")).ConfigureAwait(false);
                else if (img_format.Contains("gif") && gif_cat.Contains(category))
                    await ctx.Channel.EmbedAsync(_eb.Create().WithOkColor()
                        .WithAuthor($"Nekos Life - NSFW GIF Database {nekotitle["data"]["response"]["text"]}",
                            "https://i.imgur.com/a36AMkG.png",
                            "http://nekos.life/")
                        .WithImageUrl($"{nekoimg["data"]["response"]["url"]}")).ConfigureAwait(false);
                else if (img_format.Contains("img") && gif_cat.Contains(category))
                    await ctx.Channel.EmbedAsync(_eb.Create().WithErrorColor()
                    .WithAuthor("Nekos Life - Invalid NSFW IMG Category","https://i.imgur.com/a36AMkG.png","http://nekos.life/")
                    .WithDescription("Seems the image category you was looking for could not be found. Please use the categories listed below.")
                    .AddField("NSFW IMG Categories", "`classic_lewd`, `piersing_lewd`, `shinobu_lewd`, `feet_lewd`, `keta_avatar`, `piersing_ero`, `yuri_ero`, `solo_lewd`, `pantyhose_lewd`, `kemonomimi_lewd`, `cosplay_lewd`, `peeing_lewd`, `ahegao_avatar`, `wallpaper_lewd`, `ero_wallpaper_ero`, `blowjob_lewd`, `holo_avatar`, `neko_lewd`, `futanari_lewd`, `kitsune_ero`, `trap_lewd`, `keta_lewd`, `neko_ero`, `pantyhose_ero`, `cum_lewd`, `anal_lewd`, `smallboobs_lewd`, `all_tags_lewd`, `yuri_lewd`, `kemonomimi_ero`, `anus_lewd`, `holo_ero`, `all_tags_ero`, `kitsune_lewd`, `pussy_lewd`, `feet_ero`, `yiff_lewd`, `hplay_ero`, `bdsm_lewd`, `femdom_lewd`, `holo_lewd`, `shinobu_ero`, `tits_lewd`", false)).ConfigureAwait(false);
                else if (img_format.Contains("gif") && img_cat.Contains(category))
                    await ctx.Channel.EmbedAsync(_eb.Create().WithErrorColor()
                    .WithAuthor("Nekos Life - Invalid NSFW GIF Category",
                        "https://i.imgur.com/a36AMkG.png",
                        "http://nekos.life/")
                    .WithDescription("Seems the gif category you was looking for could not be found. Please use the categories listed below.")
                    .AddField("NSFW GIF Categories", "`blow_job`, `pussy_wank`, `classic`, `kuni`, `tits`, `pussy`, `cum`, `spank`, `feet`, `all_tags`, `yuri`, `anal`, `neko`, `girls_solo`, `yiff`", false)).ConfigureAwait(false);
                else
                    await ctx.Channel.EmbedAsync(_eb.Create().WithErrorColor()
                    .WithAuthor("Nekos Life - Invalid NSFW Image Type or Category",
                        "https://i.imgur.com/a36AMkG.png",
                        "http://nekos.life/")
                    .WithDescription("Seems the image type or category you was looking for could not be found. Please use the image type or categories listed below.")
                    .AddField("NSFW IMG Types", "`img`, `gif`", false)
                    .AddField("NSFW IMG Categories", "`classic_lewd`, `piersing_lewd`, `shinobu_lewd`, `feet_lewd`, `keta_avatar`, `piersing_ero`, `yuri_ero`, `solo_lewd`, `pantyhose_lewd`, `kemonomimi_lewd`, `cosplay_lewd`, `peeing_lewd`, `ahegao_avatar`, `wallpaper_lewd`, `ero_wallpaper_ero`, `blowjob_lewd`, `holo_avatar`, `neko_lewd`, `futanari_lewd`, `kitsune_ero`, `trap_lewd`, `keta_lewd`, `neko_ero`, `pantyhose_ero`, `cum_lewd`, `anal_lewd`, `smallboobs_lewd`, `all_tags_lewd`, `yuri_lewd`, `kemonomimi_ero`, `anus_lewd`, `holo_ero`, `all_tags_ero`, `kitsune_lewd`, `pussy_lewd`, `feet_ero`, `yiff_lewd`, `hplay_ero`, `bdsm_lewd`, `femdom_lewd`, `holo_lewd`, `shinobu_ero`, `tits_lewd`", false)
                    .AddField("NSFW GIF Categories", "`blow_job`, `pussy_wank`, `classic`, `kuni`, `tits`, `pussy`, `cum`, `spank`, `feet`, `all_tags`, `yuri`, `anal`, `neko`, `girls_solo`, `yiff`", false)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await SendErrorAsync(ex.Message).ConfigureAwait(false);
            }
        }
        
        [Cmd]
        [RequireNsfw(Group = "nsfw_or_dm"), RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
        public async partial Task Nudes()
        {
            try
            {
                await ctx.Channel.EmbedAsync(_eb.Create().WithOkColor()
                    .WithImageUrl($"http://wizbot.cc/assets/wizbot/nsfw/wiz_{new WizBotRandom().Next(1, 18)}.jpg")).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await SendErrorAsync(ex.Message).ConfigureAwait(false);
            }
        }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    [UserPerm(GuildPerm.ManageMessages)]
    public async partial Task NsfwTagBlacklist([Leftover] string tag = null)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            var blTags = await _service.GetBlacklistedTags(ctx.Guild.Id);
            await SendConfirmAsync(GetText(strs.blacklisted_tag_list), blTags.Any() ? string.Join(", ", blTags) : "-");
        }
        else
        {
            tag = tag.Trim().ToLowerInvariant();
            var added = await _service.ToggleBlacklistTag(ctx.Guild.Id, tag);

            if (added)
                await ReplyPendingLocalizedAsync(strs.blacklisted_tag_add(tag));
            else
                await ReplyPendingLocalizedAsync(strs.blacklisted_tag_remove(tag));
        }
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    [RequireNsfw(Group = "nsfw_or_dm")]
    [RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
    [Priority(1)]
    public async partial Task Nhentai(uint id)
    {
        var g = await _service.GetNhentaiByIdAsync(id);

        if (g is null)
        {
            await ReplyErrorLocalizedAsync(strs.not_found);
            return;
        }

        await SendNhentaiGalleryInternalAsync(g);
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    [RequireNsfw(Group = "nsfw_or_dm")]
    [RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
    [Priority(0)]
    public async partial Task Nhentai([Leftover] string query)
    {
        var g = await _service.GetNhentaiBySearchAsync(query);

        if (g is null)
        {
            await ReplyErrorLocalizedAsync(strs.not_found);
            return;
        }

        await SendNhentaiGalleryInternalAsync(g);
    }

    private async Task SendNhentaiGalleryInternalAsync(Gallery g)
    {
        var count = 0;
        var tagString = g.Tags.Shuffle()
                         .Select(tag => $"[{tag.Name}]({tag.Url})")
                         .TakeWhile(tag => (count += tag.Length) < 1000)
                         .Join(" ");

        var embed = _eb.Create()
                       .WithTitle(g.Title)
                       .WithDescription(g.FullTitle)
                       .WithImageUrl(g.Thumbnail)
                       .WithUrl(g.Url)
                       .AddField(GetText(strs.favorites), g.Likes, true)
                       .AddField(GetText(strs.pages), g.PageCount, true)
                       .AddField(GetText(strs.tags),
                           string.IsNullOrWhiteSpace(tagString)
                               ? "?"
                               : tagString,
                           true)
                       .WithFooter(g.UploadedAt.ToString("f"))
                       .WithOkColor();

        await ctx.Channel.EmbedAsync(embed);
    }

    private async Task InternalDapiCommand(
        string[] tags,
        bool forceExplicit,
        Func<ulong?, bool, string[], Task<UrlReply>> func)
    {
        var data = await func(ctx.Guild?.Id, forceExplicit, tags);

        if (data is null || !string.IsNullOrWhiteSpace(data.Error))
        {
            await ReplyErrorLocalizedAsync(strs.no_results);
            return;
        }

        await ctx.Channel.EmbedAsync(_eb.Create(ctx)
                                        .WithOkColor()
                                        .WithImageUrl(data.Url)
                                        .WithDescription($"[link]({data.Url})")
                                        .WithFooter(
                                            $"{data.Rating} ({data.Provider}) | {string.Join(" | ", data.Tags.Where(x => !string.IsNullOrWhiteSpace(x)).Take(5))}"));
    }
}