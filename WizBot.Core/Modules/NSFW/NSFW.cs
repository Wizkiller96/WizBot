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
using WizBot.Modules.NSFW.Exceptions;
using WizBot.Modules.Searches.Exceptions;

namespace WizBot.Modules.NSFW
{
    // thanks to halitalf for adding autoboob and autobutt features :D
    public class NSFW : WizBotTopLevelModule<SearchesService>
    {
        private static readonly ConcurrentHashSet<ulong> _hentaiBombBlacklist = new ConcurrentHashSet<ulong>();

        private async Task InternalHentai(IMessageChannel channel, string tag, bool noError)
        {
            var rng = new WizBotRandom();
            var arr = Enum.GetValues(typeof(DapiSearchType));
            var type = (DapiSearchType)arr.GetValue(new WizBotRandom().Next(2, arr.Length));
            ImageCacherObject img;
            try
            {
                img = await _service.DapiSearch(tag, type, Context.Guild?.Id, true).ConfigureAwait(false);
            }
            catch (TagBlacklistedException)
            {
                await ReplyErrorLocalized("blacklisted_tag").ConfigureAwait(false);
                return;
            }

            if (img == null)
            {
                if (!noError)
                    await ReplyErrorLocalized("not_found").ConfigureAwait(false);
                return;
            }

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
                obj = JArray.Parse(await _service.Http.GetStringAsync($"http://api.oboobs.ru/boobs/{new WizBotRandom().Next(0, 10330)}").ConfigureAwait(false))[0];
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
                obj = JArray.Parse(await _service.Http.GetStringAsync($"http://api.obutts.ru/butts/{new WizBotRandom().Next(0, 4335)}").ConfigureAwait(false))[0];
                await Channel.SendMessageAsync($"http://media.obutts.ru/{obj["preview"]}").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await Channel.SendErrorAsync(ex.Message).ConfigureAwait(false);
            }
        }

        [WizBotCommand, Usage, Description, Aliases]
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
                        await InternalHentai(Context.Channel, null, true).ConfigureAwait(false);
                    else
                        await InternalHentai(Context.Channel, tagsArr[new WizBotRandom().Next(0, tagsArr.Length)], true).ConfigureAwait(false);
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
        public Task Hentai([Remainder] string tag = null) =>
            InternalHentai(Context.Channel, tag, false);

        [WizBotCommand, Usage, Description, Aliases]
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
        public Task Yandere([Remainder] string tag = null)
            => InternalDapiCommand(tag, DapiSearchType.Yandere, false);

        [WizBotCommand, Usage, Description, Aliases]
        public Task Konachan([Remainder] string tag = null)
            => InternalDapiCommand(tag, DapiSearchType.Konachan, false);

        [WizBotCommand, Usage, Description, Aliases]
        public Task E621([Remainder] string tag = null)
            => InternalDapiCommand(tag, DapiSearchType.E621, false);

        [WizBotCommand, Usage, Description, Aliases]
        public Task Rule34([Remainder] string tag = null)
            => InternalDapiCommand(tag, DapiSearchType.Rule34, false);

        [WizBotCommand, Usage, Description, Aliases]
        public Task Danbooru([Remainder] string tag = null)
            => InternalDapiCommand(tag, DapiSearchType.Danbooru, false);

        [WizBotCommand, Usage, Description, Aliases]
        public Task Gelbooru([Remainder] string tag = null)
            => InternalDapiCommand(tag, DapiSearchType.Gelbooru, false);

        [WizBotCommand, Usage, Description, Aliases]
        public async Task Boobs()
        {
            try
            {
                JToken obj;
                obj = JArray.Parse(await _service.Http.GetStringAsync($"http://api.oboobs.ru/boobs/{new WizBotRandom().Next(0, 10330)}").ConfigureAwait(false))[0];
                await Context.Channel.SendMessageAsync($"http://media.oboobs.ru/{obj["preview"]}").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await Context.Channel.SendErrorAsync(ex.Message).ConfigureAwait(false);
            }
        }

        [WizBotCommand, Usage, Description, Aliases]
        public async Task Butts()
        {
            try
            {
                JToken obj;
                obj = JArray.Parse(await _service.Http.GetStringAsync($"http://api.obutts.ru/butts/{new WizBotRandom().Next(0, 4335)}").ConfigureAwait(false))[0];
                await Context.Channel.SendMessageAsync($"http://media.obutts.ru/{obj["preview"]}").ConfigureAwait(false);
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
        [OwnerOnly, AdminOnly]
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
