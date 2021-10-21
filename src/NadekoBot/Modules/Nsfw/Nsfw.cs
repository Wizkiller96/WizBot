using Discord;
using Discord.Commands;
using NadekoBot.Common;
using NadekoBot.Common.Attributes;
using NadekoBot.Common.Collections;
using NadekoBot.Extensions;
using NadekoBot.Modules.Searches.Common;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Serilog;

namespace NadekoBot.Modules.Nsfw
{
    [NoPublicBot]
    public class NSFW : NadekoModule<ISearchImagesService>
    {
        private static readonly ConcurrentHashSet<ulong> _hentaiBombBlacklist = new ConcurrentHashSet<ulong>();
        private readonly IHttpClientFactory _httpFactory;
        private readonly NadekoRandom _rng;

        public NSFW(IHttpClientFactory factory)
        {
            _httpFactory = factory;
            _rng = new NadekoRandom();
        }

        private async Task InternalBoobs()
        {
            try
            {
                JToken obj;
                using (var http = _httpFactory.CreateClient())
                {
                    obj = JArray.Parse(await http
                        .GetStringAsync($"http://api.oboobs.ru/boobs/{new NadekoRandom().Next(0, 10330)}")
                        .ConfigureAwait(false))[0];
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
                    obj = JArray.Parse(await http
                        .GetStringAsync($"http://api.obutts.ru/butts/{new NadekoRandom().Next(0, 4335)}")
                        .ConfigureAwait(false))[0];
                }

                await Channel.SendMessageAsync($"http://media.obutts.ru/{obj["preview"]}").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await SendErrorAsync(ex.Message).ConfigureAwait(false);
            }
        }

        [NadekoCommand, Aliases]
        [RequireNsfw]
        [RequireContext(ContextType.Guild)]
        [UserPerm(ChannelPerm.ManageMessages)]
        public async Task AutoHentai(int interval = 0, [Leftover] string tags = null)
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

            t = new Timer(async (state) =>
            {
                try
                {
                    if (tags is null || tags.Length == 0)
                        await InternalDapiCommand(null, true, _service.Hentai).ConfigureAwait(false);
                    else
                    {
                        var groups = tags.Split('|');
                        var group = groups[_rng.Next(0, groups.Length)];
                        await InternalDapiCommand(group.Split(' '), true, _service.Hentai).ConfigureAwait(false);
                    }
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
                string.Join(", ", tags)));
        }

        [NadekoCommand, Aliases]
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

        [NadekoCommand, Aliases]
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

        [NadekoCommand, Aliases]
        [RequireNsfw(Group = "nsfw_or_dm"), RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
        public Task Hentai(params string[] tags) 
            => InternalDapiCommand(tags, true, _service.Hentai);

        [NadekoCommand, Aliases]
        [RequireNsfw(Group = "nsfw_or_dm"), RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
        public async Task HentaiBomb(params string[] tags)
        {
            if (!_hentaiBombBlacklist.Add(ctx.Guild?.Id ?? ctx.User.Id))
                return;
            try
            {
                var images = await Task.WhenAll(_service.Yandere(ctx.Guild?.Id, true, tags),
                    _service.Danbooru(ctx.Guild?.Id, true, tags),
                    _service.Konachan(ctx.Guild?.Id, true, tags),
                    _service.Gelbooru(ctx.Guild?.Id, true, tags));

                var linksEnum = images?.Where(l => l != null).ToArray();
                if (images is null || !linksEnum.Any())
                {
                    await ReplyErrorLocalizedAsync(strs.no_results).ConfigureAwait(false);
                    return;
                }

                await ctx.Channel.SendMessageAsync(string.Join("\n\n", linksEnum.Select(x => x.Url)))
                    .ConfigureAwait(false);
            }
            finally
            {
                _hentaiBombBlacklist.TryRemove(ctx.Guild?.Id ?? ctx.User.Id);
            }
        }

        [NadekoCommand, Aliases]
        [RequireNsfw(Group = "nsfw_or_dm"), RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
        public Task Yandere(params string[] tags)
            => InternalDapiCommand(tags, false, _service.Yandere);

        [NadekoCommand, Aliases]
        [RequireNsfw(Group = "nsfw_or_dm"), RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
        public Task Konachan(params string[] tags)
            => InternalDapiCommand(tags, false, _service.Konachan);

        [NadekoCommand, Aliases]
        [RequireNsfw(Group = "nsfw_or_dm"), RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
        public Task Sankaku(params string[] tags)
            => InternalDapiCommand(tags, false, _service.Sankaku);

        [NadekoCommand, Aliases]
        [RequireNsfw(Group = "nsfw_or_dm"), RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
        public Task E621(params string[] tags)
            => InternalDapiCommand(tags, false, _service.E621);

        [NadekoCommand, Aliases]
        [RequireNsfw(Group = "nsfw_or_dm"), RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
        public Task Rule34(params string[] tags)
            => InternalDapiCommand(tags, false, _service.Rule34);

        [NadekoCommand, Aliases]
        [RequireNsfw(Group = "nsfw_or_dm"), RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
        public Task Danbooru(params string[] tags)
            => InternalDapiCommand(tags, false, _service.Danbooru);

        [NadekoCommand, Aliases]
        [RequireNsfw(Group = "nsfw_or_dm"), RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
        public Task Gelbooru(params string[] tags)
            => InternalDapiCommand(tags, false, _service.Gelbooru);

        [NadekoCommand, Aliases]
        [RequireNsfw(Group = "nsfw_or_dm"), RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
        public Task Derpibooru(params string[] tags)
            => InternalDapiCommand(tags, false, _service.DerpiBooru);

        [NadekoCommand, Aliases]
        [RequireNsfw(Group = "nsfw_or_dm"), RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
        public Task Safebooru(params string[] tags)
            => InternalDapiCommand(tags, false, _service.SafeBooru);

        [NadekoCommand, Aliases]
        [RequireNsfw(Group = "nsfw_or_dm"), RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
        public async Task Boobs()
        {
            try
            {
                JToken obj;
                using (var http = _httpFactory.CreateClient())
                {
                    obj = JArray.Parse(await http
                        .GetStringAsync($"http://api.oboobs.ru/boobs/{new NadekoRandom().Next(0, 12000)}")
                        .ConfigureAwait(false))[0];
                }

                await ctx.Channel.SendMessageAsync($"http://media.oboobs.ru/{obj["preview"]}").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await SendErrorAsync(ex.Message).ConfigureAwait(false);
            }
        }

        [NadekoCommand, Aliases]
        [RequireNsfw(Group = "nsfw_or_dm"), RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
        public async Task Butts()
        {
            try
            {
                JToken obj;
                using (var http = _httpFactory.CreateClient())
                {
                    obj = JArray.Parse(await http
                        .GetStringAsync($"http://api.obutts.ru/butts/{new NadekoRandom().Next(0, 6100)}")
                        .ConfigureAwait(false))[0];
                }

                await ctx.Channel.SendMessageAsync($"http://media.obutts.ru/{obj["preview"]}").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await SendErrorAsync(ex.Message).ConfigureAwait(false);
            }
        }

        [NadekoCommand, Aliases]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task NsfwTagBlacklist([Leftover] string tag = null)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                var blTags = await _service.GetBlacklistedTags(ctx.Guild.Id);
                await SendConfirmAsync(GetText(strs.blacklisted_tag_list),
                    blTags.Any()
                        ? string.Join(", ", blTags)
                        : "-").ConfigureAwait(false);
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

        [NadekoCommand, Aliases]
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

        [NadekoCommand, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireNsfw(Group = "nsfw_or_dm"), RequireContext(ContextType.DM, Group = "nsfw_or_dm")]
        [Priority(0)]
        public async Task Nhentai([Leftover] string query)
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

        private async Task InternalDapiCommand(string[] tags,
            bool forceExplicit,
            Func<ulong?, bool, string[], Task<UrlReply>> func)
        {
            var data = await func(ctx.Guild?.Id, forceExplicit, tags);
            
            if (data is null || !string.IsNullOrWhiteSpace(data.Error))
            {
                await ReplyErrorLocalizedAsync(strs.no_results);
                return;
            }
            await ctx.Channel.EmbedAsync(_eb
                .Create(ctx)
                .WithOkColor()
                .WithImageUrl(data.Url)
                .WithDescription($"[link]({data.Url})")
                .WithFooter($"{data.Rating} ({data.Provider}) | {string.Join(" | ", data.Tags.Where(x => !string.IsNullOrWhiteSpace(x)).Take(5))}"));
        }
    }
}