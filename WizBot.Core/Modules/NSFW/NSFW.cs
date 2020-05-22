using Discord;
using Discord.Commands;
using WizBot.Common;
using WizBot.Common.Attributes;
using WizBot.Common.Collections;
using WizBot.Extensions;
using WizBot.Modules.Searches.Common;
using WizBot.Modules.Searches.Services;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace WizBot.Modules.NSFW
{
    // thanks to halitalf for adding autoboob and autobutt features :D
    public class NSFW : WizBotTopLevelModule<SearchesService>
    {
        private static readonly ConcurrentHashSet<ulong> _hentaiBombBlacklist = new ConcurrentHashSet<ulong>();
        private readonly IHttpClientFactory _httpFactory;

        public NSFW(IHttpClientFactory factory)
        {
            _httpFactory = factory;
        }

        private async Task InternalHentai(IMessageChannel channel, string tag)
        {
            // create a random number generator
            var rng = new WizBotRandom();

            // get all of the DAPI search types, except first 3 
            // which are safebooru (not nsfw), and 2 furry ones 🤢
            var listOfProviders = Enum.GetValues(typeof(DapiSearchType))
                .Cast<DapiSearchType>()
                .Skip(3)
                .ToList();

            // now try to get an image, if it fails return an error,
            // keep trying for each provider until one of them is successful, or until 
            // we run out of providers. If we run out, then return an error
            ImageCacherObject img;
            do
            {
                // random index of the providers
                var num = rng.Next(0, listOfProviders.Count);
                // get the type
                var type = listOfProviders[num];
                // remove it 
                listOfProviders.RemoveAt(num);
                // get the image
                img = await _service.DapiSearch(tag, type, ctx.Guild?.Id, true).ConfigureAwait(false);
                // if i can't find the image, ran out of providers, or tag is blacklisted
                // return the error
                if (img == null && !listOfProviders.Any())
                {
                    await ReplyErrorLocalizedAsync("not_found").ConfigureAwait(false);
                    return;
                }

            } while (img == null);

            await channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                .WithImageUrl(img.FileUrl)
                .WithDescription($"[{GetText("tag")}: {tag}]({img})"))
                .ConfigureAwait(false);
        }

        private async Task InternalBoobs(IMessageChannel Channel)
        {
            try
            {
                JToken obj;
                using (var http = _httpFactory.CreateClient())
                {
                    obj = JArray.Parse(await http.GetStringAsync($"http://api.oboobs.ru/boobs/{new WizBotRandom().Next(0, 10330)}").ConfigureAwait(false))[0];
                }
                await Channel.SendMessageAsync($"http://media.oboobs.ru/{obj["preview"]}").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await Channel.SendErrorAsync(ex.Message).ConfigureAwait(false);
            }
        }
        private async Task InternalButts(IMessageChannel Channel)
        {
            try
            {
                JToken obj;
                using (var http = _httpFactory.CreateClient())
                {
                    obj = JArray.Parse(await http.GetStringAsync($"http://api.obutts.ru/butts/{new WizBotRandom().Next(0, 4335)}").ConfigureAwait(false))[0];
                }
                await Channel.SendMessageAsync($"http://media.obutts.ru/{obj["preview"]}").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await Channel.SendErrorAsync(ex.Message).ConfigureAwait(false);
            }
        }


        /* private async Task InternalNeko(IMessageChannel Channel, string category)
        {
            try
            {
                JToken nekotitle;
                JToken nekoimg;
                using (var http = _httpFactory.CreateClient())
                {
                    nekotitle = JObject.Parse(await http.GetStringAsync($"https://nekos.life/api/v2/cat").ConfigureAwait(false));
                    nekoimg = JObject.Parse(await http.GetStringAsync($"https://nekos.life/api/v2/img/{category}").ConfigureAwait(false));
                }
                await ctx.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                    .WithAuthor(eab => eab.WithUrl("http://nekos.life/")
                        .WithIconUrl("https://i.imgur.com/a36AMkG.png")
                        .WithName($"Nekos Life - Database {nekotitle["cat"]}"))
                    .WithImageUrl($"{nekoimg["url"]}")).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await Channel.SendErrorAsync(ex.Message).ConfigureAwait(false);
            }
        } */

        public async Task InternalNeko(IMessageChannel Channel, string format = "img", string category = "all_tags_lewd")
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
                    await ctx.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                        .WithAuthor(eab => eab.WithUrl("http://nekos.life/")
                            .WithIconUrl("https://i.imgur.com/a36AMkG.png")
                            .WithName($"Nekos Life - NSFW IMG Database {nekotitle["data"]["response"]["text"]}"))
                        .WithImageUrl($"{nekoimg["data"]["response"]["url"]}")).ConfigureAwait(false);
                else if (img_format.Contains("gif") && gif_cat.Contains(category))
                    await ctx.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                        .WithAuthor(eab => eab.WithUrl("http://nekos.life/")
                            .WithIconUrl("https://i.imgur.com/a36AMkG.png")
                            .WithName($"Nekos Life - NSFW GIF Database {nekotitle["data"]["response"]["text"]}"))
                        .WithImageUrl($"{nekoimg["data"]["response"]["url"]}")).ConfigureAwait(false);
                else if (img_format.Contains("img") && gif_cat.Contains(category))
                    await ctx.Channel.EmbedAsync(new EmbedBuilder().WithErrorColor()
                    .WithAuthor(eab => eab.WithUrl("http://nekos.life/")
                        .WithIconUrl("https://i.imgur.com/a36AMkG.png")
                        .WithName($"Nekos Life - Invalid NSFW IMG Category"))
                    .WithDescription("Seems the image category you was looking for could not be found. Please use the categories listed below.")
                    .AddField(fb => fb.WithName("NSFW IMG Categories").WithValue("`classic_lewd`, `piersing_lewd`, `shinobu_lewd`, `feet_lewd`, `keta_avatar`, `piersing_ero`, `yuri_ero`, `solo_lewd`, `pantyhose_lewd`, `kemonomimi_lewd`, `cosplay_lewd`, `peeing_lewd`, `ahegao_avatar`, `wallpaper_lewd`, `ero_wallpaper_ero`, `blowjob_lewd`, `holo_avatar`, `neko_lewd`, `futanari_lewd`, `kitsune_ero`, `trap_lewd`, `keta_lewd`, `neko_ero`, `pantyhose_ero`, `cum_lewd`, `anal_lewd`, `smallboobs_lewd`, `all_tags_lewd`, `yuri_lewd`, `kemonomimi_ero`, `anus_lewd`, `holo_ero`, `all_tags_ero`, `kitsune_lewd`, `pussy_lewd`, `feet_ero`, `yiff_lewd`, `hplay_ero`, `bdsm_lewd`, `femdom_lewd`, `holo_lewd`, `shinobu_ero`, `tits_lewd`").WithIsInline(false))).ConfigureAwait(false);
                else if (img_format.Contains("gif") && img_cat.Contains(category))
                    await ctx.Channel.EmbedAsync(new EmbedBuilder().WithErrorColor()
                    .WithAuthor(eab => eab.WithUrl("http://nekos.life/")
                        .WithIconUrl("https://i.imgur.com/a36AMkG.png")
                        .WithName($"Nekos Life - Invalid NSFW GIF Category"))
                    .WithDescription("Seems the gif category you was looking for could not be found. Please use the categories listed below.")
                    .AddField(fb => fb.WithName("NSFW GIF Categories").WithValue("`blow_job`, `pussy_wank`, `classic`, `kuni`, `tits`, `pussy`, `cum`, `spank`, `feet`, `all_tags`, `yuri`, `anal`, `neko`, `girls_solo`, `yiff`").WithIsInline(false))).ConfigureAwait(false);
                else
                    await ctx.Channel.EmbedAsync(new EmbedBuilder().WithErrorColor()
                    .WithAuthor(eab => eab.WithUrl("http://nekos.life/")
                        .WithIconUrl("https://i.imgur.com/a36AMkG.png")
                        .WithName($"Nekos Life - Invalid NSFW Image Type or Category"))
                    .WithDescription("Seems the image type or category you was looking for could not be found. Please use the image type or categories listed below.")
                    .AddField(fb => fb.WithName("NSFW IMG Types").WithValue("`img`, `gif`").WithIsInline(false))
                    .AddField(fb => fb.WithName("NSFW IMG Categories").WithValue("`classic_lewd`, `piersing_lewd`, `shinobu_lewd`, `feet_lewd`, `keta_avatar`, `piersing_ero`, `yuri_ero`, `solo_lewd`, `pantyhose_lewd`, `kemonomimi_lewd`, `cosplay_lewd`, `peeing_lewd`, `ahegao_avatar`, `wallpaper_lewd`, `ero_wallpaper_ero`, `blowjob_lewd`, `holo_avatar`, `neko_lewd`, `futanari_lewd`, `kitsune_ero`, `trap_lewd`, `keta_lewd`, `neko_ero`, `pantyhose_ero`, `cum_lewd`, `anal_lewd`, `smallboobs_lewd`, `all_tags_lewd`, `yuri_lewd`, `kemonomimi_ero`, `anus_lewd`, `holo_ero`, `all_tags_ero`, `kitsune_lewd`, `pussy_lewd`, `feet_ero`, `yiff_lewd`, `hplay_ero`, `bdsm_lewd`, `femdom_lewd`, `holo_lewd`, `shinobu_ero`, `tits_lewd`").WithIsInline(false))
                    .AddField(fb => fb.WithName("NSFW GIF Categories").WithValue("`blow_job`, `pussy_wank`, `classic`, `kuni`, `tits`, `pussy`, `cum`, `spank`, `feet`, `all_tags`, `yuri`, `anal`, `neko`, `girls_solo`, `yiff`").WithIsInline(false))).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await ctx.Channel.SendErrorAsync(ex.Message).ConfigureAwait(false);
            }
        }

        [WizBotCommand, Usage, Description, Aliases]
        [RequireNsfw]
        [RequireContext(ContextType.Guild)]
        [UserPerm(ChannelPerm.ManageMessages)]
        public async Task AutoHentai(int interval = 0, string tags = null)
        {
            Timer t;

            if (interval == 0)
            {
                if (!_service.AutoHentaiTimers.TryRemove(ctx.Channel.Id, out t)) return;

                t.Change(Timeout.Infinite, Timeout.Infinite); //proper way to disable the timer
                await ReplyConfirmLocalizedAsync("stopped").ConfigureAwait(false);
                return;
            }

            if (interval < 20)
                return;

            var tagsArr = tags?.Split('|');

            t = new Timer(async (state) =>
            {
                try
                {
                    if (tagsArr == null || tagsArr.Length == 0)
                        await InternalHentai(ctx.Channel, null).ConfigureAwait(false);
                    else
                        await InternalHentai(ctx.Channel, tagsArr[new WizBotRandom().Next(0, tagsArr.Length)]).ConfigureAwait(false);
                }
                catch
                {
                    // ignored
                }
            }, null, interval * 1000, interval * 1000);

            _service.AutoHentaiTimers.AddOrUpdate(ctx.Channel.Id, t, (key, old) =>
            {
                old.Change(Timeout.Infinite, Timeout.Infinite);
                return t;
            });

            await ReplyConfirmLocalizedAsync("autohentai_started",
                interval,
                string.Join(", ", tagsArr)).ConfigureAwait(false);
        }

        [WizBotCommand, Usage, Description, Aliases]
        [RequireNsfw]
        [RequireContext(ContextType.Guild)]
        [UserPerm(ChannelPerm.ManageMessages)]
        public async Task AutoBoobs(int interval = 0)
        {
            Timer t;

            if (interval == 0)
            {
                if (!_service.AutoBoobTimers.TryRemove(ctx.Channel.Id, out t)) return;

                t.Change(Timeout.Infinite, Timeout.Infinite); //proper way to disable the timer
                await ReplyConfirmLocalizedAsync("stopped").ConfigureAwait(false);
                return;
            }

            if (interval < 20)
                return;

            t = new Timer(async (state) =>
            {
                try
                {
                    await InternalBoobs(ctx.Channel).ConfigureAwait(false);
                }
                catch
                {
                    // ignored
                }
            }, null, interval * 1000, interval * 1000);

            _service.AutoBoobTimers.AddOrUpdate(ctx.Channel.Id, t, (key, old) =>
            {
                old.Change(Timeout.Infinite, Timeout.Infinite);
                return t;
            });

            await ReplyConfirmLocalizedAsync("started", interval).ConfigureAwait(false);
        }

        [WizBotCommand, Usage, Description, Aliases]
        [RequireNsfw(Group = "nsfw_or_dm"), RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
        [UserPerm(ChannelPerm.ManageMessages)]
        public async Task AutoButts(int interval = 0)
        {
            Timer t;

            if (interval == 0)
            {
                if (!_service.AutoButtTimers.TryRemove(ctx.Channel.Id, out t)) return;

                t.Change(Timeout.Infinite, Timeout.Infinite); //proper way to disable the timer
                await ReplyConfirmLocalizedAsync("stopped").ConfigureAwait(false);
                return;
            }

            if (interval < 20)
                return;

            t = new Timer(async (state) =>
            {
                try
                {
                    await InternalButts(ctx.Channel).ConfigureAwait(false);
                }
                catch
                {
                    // ignored
                }
            }, null, interval * 1000, interval * 1000);

            _service.AutoButtTimers.AddOrUpdate(ctx.Channel.Id, t, (key, old) =>
            {
                old.Change(Timeout.Infinite, Timeout.Infinite);
                return t;
            });

            await ReplyConfirmLocalizedAsync("started", interval).ConfigureAwait(false);
        }

        [WizBotCommand, Usage, Description, Aliases]
        [RequireNsfw(Group = "nsfw_or_dm"), RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
        public Task Hentai([Leftover] string tag = null) =>
            InternalHentai(ctx.Channel, tag);

        [WizBotCommand, Usage, Description, Aliases]
        [RequireNsfw(Group = "nsfw_or_dm"), RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
        public async Task HentaiBomb([Leftover] string tag = null)
        {
            if (!_hentaiBombBlacklist.Add(ctx.Guild?.Id ?? ctx.User.Id))
                return;
            try
            {
                var images = await Task.WhenAll(_service.DapiSearch(tag, DapiSearchType.Gelbooru, ctx.Guild?.Id, true),
                                                _service.DapiSearch(tag, DapiSearchType.Danbooru, ctx.Guild?.Id, true),
                                                _service.DapiSearch(tag, DapiSearchType.Konachan, ctx.Guild?.Id, true),
                                                _service.DapiSearch(tag, DapiSearchType.Yandere, ctx.Guild?.Id, true)).ConfigureAwait(false);

                var linksEnum = images?.Where(l => l != null).ToArray();
                if (images == null || !linksEnum.Any())
                {
                    await ReplyErrorLocalizedAsync("not_found").ConfigureAwait(false);
                    return;
                }

                await ctx.Channel.SendMessageAsync(string.Join("\n\n", linksEnum.Select(x => x.FileUrl))).ConfigureAwait(false);
            }
            finally
            {
                _hentaiBombBlacklist.TryRemove(ctx.Guild?.Id ?? ctx.User.Id);
            }
        }

        [WizBotCommand, Usage, Description, Aliases]
        [RequireNsfw(Group = "nsfw_or_dm"), RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
        public Task Yandere([Leftover] string tag = null)
            => InternalDapiCommand(tag, DapiSearchType.Yandere, false);

        [WizBotCommand, Usage, Description, Aliases]
        [RequireNsfw(Group = "nsfw_or_dm"), RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
        public Task Konachan([Leftover] string tag = null)
            => InternalDapiCommand(tag, DapiSearchType.Konachan, false);

        [WizBotCommand, Usage, Description, Aliases]
        [RequireNsfw(Group = "nsfw_or_dm"), RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
        public Task E621([Leftover] string tag = null)
            => InternalDapiCommand(tag, DapiSearchType.E621, false);

        [WizBotCommand, Usage, Description, Aliases]
        [RequireNsfw(Group = "nsfw_or_dm"), RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
        public Task Rule34([Leftover] string tag = null)
            => InternalDapiCommand(tag, DapiSearchType.Rule34, false);

        [WizBotCommand, Usage, Description, Aliases]
        [RequireNsfw(Group = "nsfw_or_dm"), RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
        public Task Danbooru([Leftover] string tag = null)
            => InternalDapiCommand(tag, DapiSearchType.Danbooru, false);

        [WizBotCommand, Usage, Description, Aliases]
        [RequireNsfw(Group = "nsfw_or_dm"), RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
        public Task Gelbooru([Leftover] string tag = null)
            => InternalDapiCommand(tag, DapiSearchType.Gelbooru, false);

        [WizBotCommand, Usage, Description, Aliases]
        [RequireNsfw(Group = "nsfw_or_dm"), RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
        public Task Derpibooru([Leftover] string tag = null)
            => InternalDapiCommand(tag, DapiSearchType.Derpibooru, false);

        [WizBotCommand, Usage, Description, Aliases]
        [RequireNsfw(Group = "nsfw_or_dm"), RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
        public async Task Boobs()
        {
            try
            {
                JToken obj;
                using (var http = _httpFactory.CreateClient())
                {
                    obj = JArray.Parse(await http.GetStringAsync($"http://api.oboobs.ru/boobs/{new WizBotRandom().Next(0, 12000)}").ConfigureAwait(false))[0];
                }
                await ctx.Channel.SendMessageAsync($"http://media.oboobs.ru/{obj["preview"]}").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await ctx.Channel.SendErrorAsync(ex.Message).ConfigureAwait(false);
            }
        }

        [WizBotCommand, Usage, Description, Aliases]
        [RequireNsfw(Group = "nsfw_or_dm"), RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
        public async Task Butts()
        {
            try
            {
                JToken obj;
                using (var http = _httpFactory.CreateClient())
                {
                    obj = JArray.Parse(await http.GetStringAsync($"http://api.obutts.ru/butts/{new WizBotRandom().Next(0, 6100)}").ConfigureAwait(false))[0];
                }
                await ctx.Channel.SendMessageAsync($"http://media.obutts.ru/{obj["preview"]}").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await ctx.Channel.SendErrorAsync(ex.Message).ConfigureAwait(false);
            }
        }

        [WizBotCommand, Usage, Description, Aliases]
        [RequireNsfw(Group = "nsfw_or_dm"), RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
        public async Task Neko(string format = "img", [Remainder] string category = "neko_lewd")
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
                    await ctx.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                        .WithAuthor(eab => eab.WithUrl("http://nekos.life/")
                            .WithIconUrl("https://i.imgur.com/a36AMkG.png")
                            .WithName($"Nekos Life - NSFW IMG Database {nekotitle["data"]["response"]["text"]}"))
                        .WithImageUrl($"{nekoimg["data"]["response"]["url"]}")).ConfigureAwait(false);
                else if (img_format.Contains("gif") && gif_cat.Contains(category))
                    await ctx.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                        .WithAuthor(eab => eab.WithUrl("http://nekos.life/")
                            .WithIconUrl("https://i.imgur.com/a36AMkG.png")
                            .WithName($"Nekos Life - NSFW GIF Database {nekotitle["data"]["response"]["text"]}"))
                        .WithImageUrl($"{nekoimg["data"]["response"]["url"]}")).ConfigureAwait(false);
                else if (img_format.Contains("img") && gif_cat.Contains(category))
                    await ctx.Channel.EmbedAsync(new EmbedBuilder().WithErrorColor()
                    .WithAuthor(eab => eab.WithUrl("http://nekos.life/")
                        .WithIconUrl("https://i.imgur.com/a36AMkG.png")
                        .WithName($"Nekos Life - Invalid NSFW IMG Category"))
                    .WithDescription("Seems the image category you was looking for could not be found. Please use the categories listed below.")
                    .AddField(fb => fb.WithName("NSFW IMG Categories").WithValue("`classic_lewd`, `piersing_lewd`, `shinobu_lewd`, `feet_lewd`, `keta_avatar`, `piersing_ero`, `yuri_ero`, `solo_lewd`, `pantyhose_lewd`, `kemonomimi_lewd`, `cosplay_lewd`, `peeing_lewd`, `ahegao_avatar`, `wallpaper_lewd`, `ero_wallpaper_ero`, `blowjob_lewd`, `holo_avatar`, `neko_lewd`, `futanari_lewd`, `kitsune_ero`, `trap_lewd`, `keta_lewd`, `neko_ero`, `pantyhose_ero`, `cum_lewd`, `anal_lewd`, `smallboobs_lewd`, `all_tags_lewd`, `yuri_lewd`, `kemonomimi_ero`, `anus_lewd`, `holo_ero`, `all_tags_ero`, `kitsune_lewd`, `pussy_lewd`, `feet_ero`, `yiff_lewd`, `hplay_ero`, `bdsm_lewd`, `femdom_lewd`, `holo_lewd`, `shinobu_ero`, `tits_lewd`").WithIsInline(false))).ConfigureAwait(false);
                else if (img_format.Contains("gif") && img_cat.Contains(category))
                    await ctx.Channel.EmbedAsync(new EmbedBuilder().WithErrorColor()
                    .WithAuthor(eab => eab.WithUrl("http://nekos.life/")
                        .WithIconUrl("https://i.imgur.com/a36AMkG.png")
                        .WithName($"Nekos Life - Invalid NSFW GIF Category"))
                    .WithDescription("Seems the gif category you was looking for could not be found. Please use the categories listed below.")
                    .AddField(fb => fb.WithName("NSFW GIF Categories").WithValue("`blow_job`, `pussy_wank`, `classic`, `kuni`, `tits`, `pussy`, `cum`, `spank`, `feet`, `all_tags`, `yuri`, `anal`, `neko`, `girls_solo`, `yiff`").WithIsInline(false))).ConfigureAwait(false);
                else
                    await ctx.Channel.EmbedAsync(new EmbedBuilder().WithErrorColor()
                    .WithAuthor(eab => eab.WithUrl("http://nekos.life/")
                        .WithIconUrl("https://i.imgur.com/a36AMkG.png")
                        .WithName($"Nekos Life - Invalid NSFW Image Type or Category"))
                    .WithDescription("Seems the image type or category you was looking for could not be found. Please use the image type or categories listed below.")
                    .AddField(fb => fb.WithName("NSFW IMG Types").WithValue("`img`, `gif`").WithIsInline(false))
                    .AddField(fb => fb.WithName("NSFW IMG Categories").WithValue("`classic_lewd`, `piersing_lewd`, `shinobu_lewd`, `feet_lewd`, `keta_avatar`, `piersing_ero`, `yuri_ero`, `solo_lewd`, `pantyhose_lewd`, `kemonomimi_lewd`, `cosplay_lewd`, `peeing_lewd`, `ahegao_avatar`, `wallpaper_lewd`, `ero_wallpaper_ero`, `blowjob_lewd`, `holo_avatar`, `neko_lewd`, `futanari_lewd`, `kitsune_ero`, `trap_lewd`, `keta_lewd`, `neko_ero`, `pantyhose_ero`, `cum_lewd`, `anal_lewd`, `smallboobs_lewd`, `all_tags_lewd`, `yuri_lewd`, `kemonomimi_ero`, `anus_lewd`, `holo_ero`, `all_tags_ero`, `kitsune_lewd`, `pussy_lewd`, `feet_ero`, `yiff_lewd`, `hplay_ero`, `bdsm_lewd`, `femdom_lewd`, `holo_lewd`, `shinobu_ero`, `tits_lewd`").WithIsInline(false))
                    .AddField(fb => fb.WithName("NSFW GIF Categories").WithValue("`blow_job`, `pussy_wank`, `classic`, `kuni`, `tits`, `pussy`, `cum`, `spank`, `feet`, `all_tags`, `yuri`, `anal`, `neko`, `girls_solo`, `yiff`").WithIsInline(false))).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await ctx.Channel.SendErrorAsync(ex.Message).ConfigureAwait(false);
            }
        }

        [WizBotCommand, Usage, Description, Aliases]
        [RequireNsfw(Group = "nsfw_or_dm"), RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
        public async Task Nudes()
        {
            try
            {
                await ctx.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                        .WithImageUrl($"http://wizbot.cc/assets/wizbot/nsfw/wiz_{new WizBotRandom().Next(1, 18)}.jpg")).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await ctx.Channel.SendErrorAsync(ex.Message).ConfigureAwait(false);
            }
        }

        [WizBotCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task NsfwTagBlacklist([Leftover] string tag = null)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                var blTags = _service.GetBlacklistedTags(ctx.Guild.Id);
                await ctx.Channel.SendConfirmAsync(GetText("blacklisted_tag_list"),
                    blTags.Any()
                    ? string.Join(", ", blTags)
                    : "-").ConfigureAwait(false);
            }
            else
            {
                tag = tag.Trim().ToLowerInvariant();
                var added = _service.ToggleBlacklistedTag(ctx.Guild.Id, tag);

                if (added)
                    await ReplyConfirmLocalizedAsync("blacklisted_tag_add", tag).ConfigureAwait(false);
                else
                    await ReplyConfirmLocalizedAsync("blacklisted_tag_remove", tag).ConfigureAwait(false);
            }
        }

        [WizBotCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [AdminOnly]
        public Task NsfwClearCache()
        {
            _service.ClearCache();
            return ctx.Channel.SendConfirmAsync("👌");
        }

        public async Task InternalDapiCommand(string tag, DapiSearchType type, bool forceExplicit)
        {
            ImageCacherObject imgObj;

            imgObj = await _service.DapiSearch(tag, type, ctx.Guild?.Id, forceExplicit).ConfigureAwait(false);

            if (imgObj == null)
                await ReplyErrorLocalizedAsync("not_found").ConfigureAwait(false);
            else
            {
                var embed = new EmbedBuilder().WithOkColor()
                    .WithDescription($"{ctx.User} [{tag ?? "url"}]({imgObj}) ")
                    .WithFooter(efb => efb.WithText(type.ToString()));

                if (Uri.IsWellFormedUriString(imgObj.FileUrl, UriKind.Absolute))
                    embed.WithImageUrl(imgObj.FileUrl);
                else
                    _log.Error($"Image link from {type} is not a proper Url: {imgObj.FileUrl}");

                await ctx.Channel.EmbedAsync(embed).ConfigureAwait(false);
            }
        }
    }
}