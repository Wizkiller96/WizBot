﻿using Discord;
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
using Serilog;

namespace WizBot.Modules.NSFW
{
    // thanks to halitalf for adding autoboob and autobutt features :D
    public class NSFW : WizBotModule<SearchesService>
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
                if (img is null && !listOfProviders.Any())
                {
                    await ReplyErrorLocalizedAsync(strs.no_results).ConfigureAwait(false);
                    return;
                }

            } while (img is null);

            await channel.EmbedAsync(_eb.Create().WithOkColor()
                .WithImageUrl(img.FileUrl)
                .WithDescription($"[{GetText(strs.tag)}: {tag}]({img})"))
                .ConfigureAwait(false);
        }

        private async Task InternalBoobs()
        {
            try
            {
                JToken obj;
                using (var http = _httpFactory.CreateClient())
                {
                    obj = JArray.Parse(await http.GetStringAsync($"http://api.oboobs.ru/boobs/{new WizBotRandom().Next(0, 10330)}").ConfigureAwait(false))[0];
                }
                await ctx.Channel.SendMessageAsync($"http://media.oboobs.ru/{obj["preview"]}").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await SendErrorAsync(ex.Message).ConfigureAwait(false);
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
                await SendErrorAsync(ex.Message).ConfigureAwait(false);
            }
        }

#if !GLOBAL_WIZBOT
        [WizBotCommand, Aliases]
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
                await ReplyConfirmLocalizedAsync(strs.stopped).ConfigureAwait(false);
                return;
            }

            if (interval < 20)
                return;

            var tagsArr = tags?.Split('|');

            t = new Timer(async (state) =>
            {
                try
                {
                    if (tagsArr is null || tagsArr.Length == 0)
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

            await ReplyConfirmLocalizedAsync(strs.autohentai_started(
                interval,
                string.Join(", ", tagsArr)));
        }

        [WizBotCommand, Aliases]
        [RequireNsfw]
        [RequireContext(ContextType.Guild)]
        [UserPerm(ChannelPerm.ManageMessages)]
        public async Task AutoBoobs(int interval = 0)
        {
            Timer t;

            if (interval == 0)
            {
                if (!_service.AutoBoobTimers.TryRemove(ctx.Channel.Id, out t)) return;

                t.Change(Timeout.Infinite, Timeout.Infinite);
                await ReplyConfirmLocalizedAsync(strs.stopped).ConfigureAwait(false);
                return;
            }

            if (interval < 20)
                return;

            t = new Timer(async (state) =>
            {
                try
                {
                    await InternalBoobs().ConfigureAwait(false);
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

            await ReplyConfirmLocalizedAsync(strs.started(interval));
        }

        [WizBotCommand, Aliases]
        [RequireNsfw(Group = "nsfw_or_dm"), RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
        [UserPerm(ChannelPerm.ManageMessages)]
        public async Task AutoButts(int interval = 0)
        {
            Timer t;

            if (interval == 0)
            {
                if (!_service.AutoButtTimers.TryRemove(ctx.Channel.Id, out t)) return;

                t.Change(Timeout.Infinite, Timeout.Infinite); //proper way to disable the timer
                await ReplyConfirmLocalizedAsync(strs.stopped).ConfigureAwait(false);
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

            await ReplyConfirmLocalizedAsync(strs.started(interval));
        }
#endif

        [WizBotCommand, Aliases]
        [RequireNsfw(Group = "nsfw_or_dm"), RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
        public Task Hentai([Leftover] string tag = null) =>
            InternalHentai(ctx.Channel, tag);

        [WizBotCommand, Aliases]
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
                if (images is null || !linksEnum.Any())
                {
                    await ReplyErrorLocalizedAsync(strs.no_results).ConfigureAwait(false);
                    return;
                }

                await ctx.Channel.SendMessageAsync(string.Join("\n\n", linksEnum.Select(x => x.FileUrl))).ConfigureAwait(false);
            }
            finally
            {
                _hentaiBombBlacklist.TryRemove(ctx.Guild?.Id ?? ctx.User.Id);
            }
        }

        [WizBotCommand, Aliases]
        [RequireNsfw(Group = "nsfw_or_dm"), RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
        public Task Yandere([Leftover] string tag = null)
            => InternalDapiCommand(tag, DapiSearchType.Yandere, false);

        [WizBotCommand, Aliases]
        [RequireNsfw(Group = "nsfw_or_dm"), RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
        public Task Konachan([Leftover] string tag = null)
            => InternalDapiCommand(tag, DapiSearchType.Konachan, false);
        
        [WizBotCommand, Aliases]
        [RequireNsfw(Group = "nsfw_or_dm"), RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
        public Task Sankaku([Leftover] string tag = null)
            => InternalDapiCommand(tag, DapiSearchType.Sankaku, false);

        [WizBotCommand, Aliases]
        [RequireNsfw(Group = "nsfw_or_dm"), RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
        public Task E621([Leftover] string tag = null)
            => InternalDapiCommand(tag, DapiSearchType.E621, false);

        [WizBotCommand, Aliases]
        [RequireNsfw(Group = "nsfw_or_dm"), RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
        public Task Rule34([Leftover] string tag = null)
            => InternalDapiCommand(tag, DapiSearchType.Rule34, false);

        [WizBotCommand, Aliases]
        [RequireNsfw(Group = "nsfw_or_dm"), RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
        public Task Danbooru([Leftover] string tag = null)
            => InternalDapiCommand(tag, DapiSearchType.Danbooru, false);

        [WizBotCommand, Aliases]
        [RequireNsfw(Group = "nsfw_or_dm"), RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
        public Task Gelbooru([Leftover] string tag = null)
            => InternalDapiCommand(tag, DapiSearchType.Gelbooru, false);

        [WizBotCommand, Aliases]
        [RequireNsfw(Group = "nsfw_or_dm"), RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
        public Task Derpibooru([Leftover] string tag = null)
            => InternalDapiCommand(tag, DapiSearchType.Derpibooru, false);

        [WizBotCommand, Aliases]
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
                await SendErrorAsync(ex.Message).ConfigureAwait(false);
            }
        }

        [WizBotCommand, Aliases]
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
                await SendErrorAsync(ex.Message).ConfigureAwait(false);
            }
        }

        [WizBotCommand, Aliases]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task NsfwTagBlacklist([Leftover] string tag = null)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                var blTags = _service.GetBlacklistedTags(ctx.Guild.Id);
                await SendConfirmAsync(GetText(strs.blacklisted_tag_list),
                    blTags.Any()
                    ? string.Join(", ", blTags)
                    : "-").ConfigureAwait(false);
            }
            else
            {
                tag = tag.Trim().ToLowerInvariant();
                var added = _service.ToggleBlacklistedTag(ctx.Guild.Id, tag);

                if (added)
                    await ReplyPendingLocalizedAsync(strs.blacklisted_tag_add(tag));
                else
                    await ReplyPendingLocalizedAsync(strs.blacklisted_tag_remove(tag));
            }
        }

        [WizBotCommand, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public Task NsfwClearCache()
        {
            _service.ClearCache();
            return ctx.OkAsync();
        }
        
        [WizBotCommand, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireNsfw(Group = "nsfw_or_dm"), RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
        [Priority(1)]
        public async Task Nhentai(uint id)
        {
            var g = await _service.GetNhentaiByIdAsync(id);

            if (g is null)
            {
                await ReplyErrorLocalizedAsync(strs.not_found);
                return;
            }

            await SendNhentaiGalleryInternalAsync(g);
        }
        
        [WizBotCommand, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireNsfw(Group = "nsfw_or_dm"), RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
        [Priority(0)]
        public async Task Nhentai([Leftover]string query)
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
            var tagString = g.Tags
                .Shuffle()
                .Select(tag => $"[{tag.Name}]({tag.Url})")
                .TakeWhile(tag => (count += tag.Length) < 1000)
                .JoinWith(" ");
            
            var embed = _eb.Create()
                .WithTitle(g.Title)
                .WithDescription(g.FullTitle)
                .WithImageUrl(g.Thumbnail)
                .WithUrl(g.Url)
                .AddField(GetText(strs.favorites), g.Likes, true)
                .AddField(GetText(strs.pages), g.PageCount, true)
                .AddField(GetText(strs.tags), tagString, true)
                .WithFooter(g.UploadedAt.ToString("f"))
                .WithOkColor();

            await ctx.Channel.EmbedAsync(embed);
        }

        public async Task InternalDapiCommand(string tag, DapiSearchType type, bool forceExplicit)
        {
            ImageCacherObject imgObj;

            imgObj = await _service.DapiSearch(tag, type, ctx.Guild?.Id, forceExplicit).ConfigureAwait(false);

            if (imgObj is null)
                await ReplyErrorLocalizedAsync(strs.no_results).ConfigureAwait(false);
            else
            {
                var embed = _eb.Create().WithOkColor()
                    .WithDescription($"{ctx.User} [{tag ?? "url"}]({imgObj}) ")
                    .WithFooter(type.ToString());

                if (Uri.IsWellFormedUriString(imgObj.FileUrl, UriKind.Absolute))
                    embed.WithImageUrl(imgObj.FileUrl);
                else
                    Log.Error($"Image link from {type} is not a proper Url: {imgObj.FileUrl}");

                await ctx.Channel.EmbedAsync(embed).ConfigureAwait(false);
            }
        }
    }
}