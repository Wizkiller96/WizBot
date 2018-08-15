using Discord;
using Discord.Commands;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Threading.Tasks;
using WizBot.Extensions;
using System.Threading;
using WizBot.Common;
using WizBot.Common.Attributes;
using WizBot.Common.Collections;
using WizBot.Modules.Searches.Common;
using WizBot.Modules.Searches.Services;
using WizBot.Modules.Searches.Exceptions;
using System.Net.Http;

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
                try
                {
                    // get the image
                    img = await _service.DapiSearch(tag, type, Context.Guild?.Id, true).ConfigureAwait(false);
                }
                catch (TagBlacklistedException)
                {
                    await ReplyErrorLocalized("blacklisted_tag").ConfigureAwait(false);
                    return;
                }
                // if i can't find the image and i ran out of providers
                // return the error
                if (img == null && !listOfProviders.Any())
                {
                    await ReplyErrorLocalized("not_found").ConfigureAwait(false);
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
                await Context.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                    .WithImageUrl($"http://media.oboobs.ru/{obj["preview"]}")).ConfigureAwait(false);
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
                await Context.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                    .WithImageUrl($"http://media.obutts.ru/{obj["preview"]}")).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await Channel.SendErrorAsync(ex.Message).ConfigureAwait(false);
            }
        }

        private async Task InternalNeko(IMessageChannel Channel, string category)
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
                await Context.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                    .WithAuthor(eab => eab.WithUrl("http://nekos.life/")
                        .WithIconUrl("https://i.imgur.com/a36AMkG.png")
                        .WithName($"Nekos Life - Database {nekotitle["cat"]}"))
                    .WithImageUrl($"{nekoimg["url"]}")).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await Channel.SendErrorAsync(ex.Message).ConfigureAwait(false);
            }
        }

        [WizBotCommand, Usage, Description, Aliases]
        [RequireNsfw]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(ChannelPermission.ManageMessages)]
        public async Task AutoHentai(int interval = 0, string tags = null)
        {
            Timer t;

            if (interval == 0)
            {
                if (!_service.AutoHentaiTimers.TryRemove(Context.Channel.Id, out t)) return;

                t.Change(Timeout.Infinite, Timeout.Infinite); //proper way to disable the timer
                await ReplyConfirmLocalized("stopped").ConfigureAwait(false);
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
                        await InternalHentai(Context.Channel, null).ConfigureAwait(false);
                    else
                        await InternalHentai(Context.Channel, tagsArr[new WizBotRandom().Next(0, tagsArr.Length)]).ConfigureAwait(false);
                }
                catch
                {
                    // ignored
                }
            }, null, interval * 1000, interval * 1000);

            _service.AutoHentaiTimers.AddOrUpdate(Context.Channel.Id, t, (key, old) =>
            {
                old.Change(Timeout.Infinite, Timeout.Infinite);
                return t;
            });

            await ReplyConfirmLocalized("autohentai_started",
                interval,
                string.Join(", ", tagsArr)).ConfigureAwait(false);
        }

        [WizBotCommand, Usage, Description, Aliases]
        [RequireNsfw]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(ChannelPermission.ManageMessages)]
        public async Task AutoBoobs(int interval = 0)
        {
            Timer t;

            if (interval == 0)
            {
                if (!_service.AutoBoobTimers.TryRemove(Context.Channel.Id, out t)) return;

                t.Change(Timeout.Infinite, Timeout.Infinite); //proper way to disable the timer
                await ReplyConfirmLocalized("stopped").ConfigureAwait(false);
                return;
            }

            if (interval < 20)
                return;

            t = new Timer(async (state) =>
            {
                try
                {
                    await InternalBoobs(Context.Channel).ConfigureAwait(false);
                }
                catch
                {
                    // ignored
                }
            }, null, interval * 1000, interval * 1000);

            _service.AutoBoobTimers.AddOrUpdate(Context.Channel.Id, t, (key, old) =>
            {
                old.Change(Timeout.Infinite, Timeout.Infinite);
                return t;
            });

            await ReplyConfirmLocalized("started", interval).ConfigureAwait(false);
        }

        [WizBotCommand, Usage, Description, Aliases]
        [RequireNsfw(Group = "nsfw_or_dm"), RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
        [RequireUserPermission(ChannelPermission.ManageMessages)]
        public async Task AutoButts(int interval = 0)
        {
            Timer t;

            if (interval == 0)
            {
                if (!_service.AutoButtTimers.TryRemove(Context.Channel.Id, out t)) return;

                t.Change(Timeout.Infinite, Timeout.Infinite); //proper way to disable the timer
                await ReplyConfirmLocalized("stopped").ConfigureAwait(false);
                return;
            }

            if (interval < 20)
                return;

            t = new Timer(async (state) =>
            {
                try
                {
                    await InternalButts(Context.Channel).ConfigureAwait(false);
                }
                catch
                {
                    // ignored
                }
            }, null, interval * 1000, interval * 1000);

            _service.AutoButtTimers.AddOrUpdate(Context.Channel.Id, t, (key, old) =>
            {
                old.Change(Timeout.Infinite, Timeout.Infinite);
                return t;
            });

            await ReplyConfirmLocalized("started", interval).ConfigureAwait(false);
        }

        [WizBotCommand, Usage, Description, Aliases]
        [RequireNsfw(Group = "nsfw_or_dm"), RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
        public Task Hentai([Remainder] string tag = null) =>
            InternalHentai(Context.Channel, tag);

        [WizBotCommand, Usage, Description, Aliases]
        [RequireNsfw(Group = "nsfw_or_dm"), RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
        public async Task HentaiBomb([Remainder] string tag = null)
        {
            if (!_hentaiBombBlacklist.Add(Context.Guild?.Id ?? Context.User.Id))
                return;
            try
            {
                var images = await Task.WhenAll(_service.DapiSearch(tag, DapiSearchType.Gelbooru, Context.Guild?.Id, true),
                                                _service.DapiSearch(tag, DapiSearchType.Danbooru, Context.Guild?.Id, true),
                                                _service.DapiSearch(tag, DapiSearchType.Konachan, Context.Guild?.Id, true),
                                                _service.DapiSearch(tag, DapiSearchType.Yandere, Context.Guild?.Id, true)).ConfigureAwait(false);

                var linksEnum = images?.Where(l => l != null).ToArray();
                if (images == null || !linksEnum.Any())
                {
                    await ReplyErrorLocalized("not_found").ConfigureAwait(false);
                    return;
                }

                await Context.Channel.SendMessageAsync(string.Join("\n\n", linksEnum.Select(x => x.FileUrl))).ConfigureAwait(false);
            }
            finally
            {
                _hentaiBombBlacklist.TryRemove(Context.Guild?.Id ?? Context.User.Id);
            }
        }

        [WizBotCommand, Usage, Description, Aliases]
        [RequireNsfw(Group = "nsfw_or_dm"), RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
        public Task Yandere([Remainder] string tag = null)
            => InternalDapiCommand(tag, DapiSearchType.Yandere, false);

        [WizBotCommand, Usage, Description, Aliases]
        [RequireNsfw(Group = "nsfw_or_dm"), RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
        public Task Konachan([Remainder] string tag = null)
            => InternalDapiCommand(tag, DapiSearchType.Konachan, false);

        [WizBotCommand, Usage, Description, Aliases]
        [RequireNsfw(Group = "nsfw_or_dm"), RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
        public Task E621([Remainder] string tag = null)
            => InternalDapiCommand(tag, DapiSearchType.E621, false);

        [WizBotCommand, Usage, Description, Aliases]
        [RequireNsfw(Group = "nsfw_or_dm"), RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
        public Task Rule34([Remainder] string tag = null)
            => InternalDapiCommand(tag, DapiSearchType.Rule34, false);

        [WizBotCommand, Usage, Description, Aliases]
        [RequireNsfw(Group = "nsfw_or_dm"), RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
        public Task Danbooru([Remainder] string tag = null)
            => InternalDapiCommand(tag, DapiSearchType.Danbooru, false);

        [WizBotCommand, Usage, Description, Aliases]
        [RequireNsfw(Group = "nsfw_or_dm"), RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
        public Task Gelbooru([Remainder] string tag = null)
            => InternalDapiCommand(tag, DapiSearchType.Gelbooru, false);

        [WizBotCommand, Usage, Description, Aliases]
        [RequireNsfw(Group = "nsfw_or_dm"), RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
        public Task Derpibooru([Remainder] string tag = null)
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
                    obj = JArray.Parse(await http.GetStringAsync($"http://api.oboobs.ru/boobs/{new WizBotRandom().Next(0, 10330)}").ConfigureAwait(false))[0];
                }
                await Context.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                    .WithImageUrl($"http://media.oboobs.ru/{obj["preview"]}")).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await Context.Channel.SendErrorAsync(ex.Message).ConfigureAwait(false);
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
                    obj = JArray.Parse(await http.GetStringAsync($"http://api.obutts.ru/butts/{new WizBotRandom().Next(0, 4335)}").ConfigureAwait(false))[0];
                }
                await Context.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                    .WithImageUrl($"http://media.obutts.ru/{obj["preview"]}")).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await Context.Channel.SendErrorAsync(ex.Message).ConfigureAwait(false);
            }
        }

        [WizBotCommand, Usage, Description, Aliases]
        [RequireNsfw(Group = "nsfw_or_dm"), RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
        public async Task Neko([Remainder] string category = "lewd")
        {
            if (string.IsNullOrWhiteSpace(category))
                return;

            try
            {
                JToken nekotitle;
                JToken nekoimg;
                using (var http = _httpFactory.CreateClient())
                {
                    nekotitle = JObject.Parse(await http.GetStringAsync($"https://nekos.life/api/v2/cat").ConfigureAwait(false));
                    nekoimg = JObject.Parse(await http.GetStringAsync($"https://nekos.life/api/v2/img/{category}").ConfigureAwait(false));
                }
                await Context.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                    .WithAuthor(eab => eab.WithUrl("http://nekos.life/")
                        .WithIconUrl("https://i.imgur.com/a36AMkG.png")
                        .WithName($"Nekos Life - Database {nekotitle["cat"]}"))
                    .WithImageUrl($"{nekoimg["url"]}")).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await Context.Channel.SendErrorAsync(ex.Message).ConfigureAwait(false);
            }
        }

        [WizBotCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task NsfwTagBlacklist([Remainder] string tag = null)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                var blTags = _service.GetBlacklistedTags(Context.Guild.Id);
                await Context.Channel.SendConfirmAsync(GetText("blacklisted_tag_list"),
                    blTags.Any()
                    ? string.Join(", ", blTags)
                    : "-").ConfigureAwait(false);
            }
            else
            {
                tag = tag.Trim().ToLowerInvariant();
                var added = _service.ToggleBlacklistedTag(Context.Guild.Id, tag);

                if (added)
                    await ReplyConfirmLocalized("blacklisted_tag_add", tag).ConfigureAwait(false);
                else
                    await ReplyConfirmLocalized("blacklisted_tag_remove", tag).ConfigureAwait(false);
            }
        }

        [WizBotCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [AdminOnly]
        public Task NsfwClearCache()
        {
            _service.ClearCache();
            return Context.Channel.SendConfirmAsync("👌");
        }

        public async Task InternalDapiCommand(string tag, DapiSearchType type, bool forceExplicit)
        {
            ImageCacherObject imgObj;
            try
            {
                imgObj = await _service.DapiSearch(tag, type, Context.Guild?.Id, forceExplicit).ConfigureAwait(false);
            }
            catch (TagBlacklistedException)
            {
                await ReplyErrorLocalized("blacklisted_tag").ConfigureAwait(false);
                return;
            }

            if (imgObj == null)
                await ReplyErrorLocalized("not_found").ConfigureAwait(false);
            else
            {
                var embed = new EmbedBuilder().WithOkColor()
                    .WithDescription($"{Context.User} [{tag ?? "url"}]({imgObj}) ")
                    .WithFooter(efb => efb.WithText(type.ToString()));

                if (Uri.IsWellFormedUriString(imgObj.FileUrl, UriKind.Absolute))
                    embed.WithImageUrl(imgObj.FileUrl);
                else
                    _log.Error($"Image link from {type} is not a proper Url: {imgObj.FileUrl}");

                await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
            }
        }
    }
}
