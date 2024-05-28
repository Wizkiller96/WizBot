﻿#nullable disable
using AngleSharp;
using AngleSharp.Html.Dom;
using WizBot.Modules.Searches.Services;

namespace WizBot.Modules.Searches;

public partial class Searches
{
    [Group]
    public partial class AnimeSearchCommands : WizBotModule<AnimeSearchService>
    {
        // [WizBotCommand, Aliases]
        // public async Task Novel([Leftover] string query)
        // {
        //     if (string.IsNullOrWhiteSpace(query))
        //         return;
        //
        //     var novelData = await _service.GetNovelData(query);
        //
        //     if (novelData is null)
        //     {
        //         await Response().Error(strs.failed_finding_novel).SendAsync();
        //         return;
        //     }
        //
        //     var embed = _sender.CreateEmbed()
        //         .WithOkColor()
        //         .WithDescription(novelData.Description.Replace("<br>", Environment.NewLine, StringComparison.InvariantCulture))
        //         .WithTitle(novelData.Title)
        //         .WithUrl(novelData.Link)
        //         .WithImageUrl(novelData.ImageUrl)
        //         .AddField(GetText(strs.authors), string.Join("\n", novelData.Authors), true)
        //         .AddField(GetText(strs.status), novelData.Status, true)
        //         .AddField(GetText(strs.genres), string.Join(" ", novelData.Genres.Any() ? novelData.Genres : new[] { "none" }), true)
        //         .WithFooter($"{GetText(strs.score)} {novelData.Score}");
        //     
        //     await Response().Embed(embed).SendAsync();
        // }

        [Cmd]
        [Priority(0)]
        public async Task Mal([Leftover] string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return;

            var fullQueryLink = "https://myanimelist.net/profile/" + name;

            var config = Configuration.Default.WithDefaultLoader();
            using var document = await BrowsingContext.New(config).OpenAsync(fullQueryLink);
            var imageElem =
                document.QuerySelector(
                    "body > div#myanimelist > div.wrapper > div#contentWrapper > div#content > div.content-container > div.container-left > div.user-profile > div.user-image > img");
            var imageUrl = ((IHtmlImageElement)imageElem)?.Source
                           ?? "http://icecream.me/uploads/870b03f36b59cc16ebfe314ef2dde781.png";

            var stats = document
                        .QuerySelectorAll(
                            "body > div#myanimelist > div.wrapper > div#contentWrapper > div#content > div.content-container > div.container-right > div#statistics > div.user-statistics-stats > div.stats > div.clearfix > ul.stats-status > li > span")
                        .Select(x => x.InnerHtml)
                        .ToList();

            var favorites = document.QuerySelectorAll("div.user-favorites > div.di-tc");

            var favAnime = GetText(strs.anime_no_fav);
            if (favorites.Length > 0 && favorites[0].QuerySelector("p") is null)
            {
                favAnime = string.Join("\n",
                    favorites[0]
                        .QuerySelectorAll("ul > li > div.di-tc.va-t > a")
                        .Shuffle()
                        .Take(3)
                        .Select(x =>
                        {
                            var elem = (IHtmlAnchorElement)x;
                            return $"[{elem.InnerHtml}]({elem.Href})";
                        }));
            }

            var info = document.QuerySelectorAll("ul.user-status:nth-child(3) > li.clearfix")
                               .Select(x => Tuple.Create(x.Children[0].InnerHtml, x.Children[1].InnerHtml))
                               .ToList();

            var daysAndMean = document.QuerySelectorAll("div.anime:nth-child(1) > div:nth-child(2) > div")
                                      .Select(x => x.TextContent.Split(':').Select(y => y.Trim()).ToArray())
                                      .ToArray();

            var embed = _sender.CreateEmbed()
                           .WithOkColor()
                           .WithTitle(GetText(strs.mal_profile(name)))
                           .AddField("💚 " + GetText(strs.watching), stats[0], true)
                           .AddField("💙 " + GetText(strs.completed), stats[1], true);
            if (info.Count < 3)
                embed.AddField("💛 " + GetText(strs.on_hold), stats[2], true);
            embed.AddField("💔 " + GetText(strs.dropped), stats[3], true)
                 .AddField("⚪ " + GetText(strs.plan_to_watch), stats[4], true)
                 .AddField("🕐 " + daysAndMean[0][0], daysAndMean[0][1], true)
                 .AddField("📊 " + daysAndMean[1][0], daysAndMean[1][1], true)
                 .AddField(MalInfoToEmoji(info[0].Item1) + " " + info[0].Item1, info[0].Item2.TrimTo(20), true)
                 .AddField(MalInfoToEmoji(info[1].Item1) + " " + info[1].Item1, info[1].Item2.TrimTo(20), true);
            if (info.Count > 2)
                embed.AddField(MalInfoToEmoji(info[2].Item1) + " " + info[2].Item1, info[2].Item2.TrimTo(20), true);

            embed.WithDescription($@"
** https://myanimelist.net/animelist/{name} **

**{GetText(strs.top_3_fav_anime)}**
{favAnime}")
                 .WithUrl(fullQueryLink)
                 .WithImageUrl(imageUrl);

            await Response().Embed(embed).SendAsync();
        }

        private static string MalInfoToEmoji(string info)
        {
            info = info.Trim().ToLowerInvariant();
            switch (info)
            {
                case "gender":
                    return "🚁";
                case "location":
                    return "🗺";
                case "last online":
                    return "👥";
                case "birthday":
                    return "📆";
                default:
                    return "❔";
            }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [Priority(1)]
        public Task Mal(IGuildUser usr)
            => Mal(usr.Username);

        [Cmd]
        public async Task Anime([Leftover] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return;

            var animeData = await _service.GetAnimeData(query);

            if (animeData is null)
            {
                await Response().Error(strs.failed_finding_anime).SendAsync();
                return;
            }

            var embed = _sender.CreateEmbed()
                           .WithOkColor()
                           .WithDescription(animeData.Synopsis.Replace("<br>",
                               Environment.NewLine,
                               StringComparison.InvariantCulture))
                           .WithTitle(animeData.TitleEnglish)
                           .WithUrl(animeData.Link)
                           .WithImageUrl(animeData.ImageUrlLarge)
                           .AddField(GetText(strs.episodes), animeData.TotalEpisodes.ToString(), true)
                           .AddField(GetText(strs.status), animeData.AiringStatus, true)
                           .AddField(GetText(strs.genres),
                               string.Join(",\n", animeData.Genres.Any() ? animeData.Genres : ["none"]),
                               true)
                           .WithFooter($"{GetText(strs.score)} {animeData.AverageScore} / 100");
            await Response().Embed(embed).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Manga([Leftover] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return;

            var mangaData = await _service.GetMangaData(query);

            if (mangaData is null)
            {
                await Response().Error(strs.failed_finding_manga).SendAsync();
                return;
            }

            var embed = _sender.CreateEmbed()
                           .WithOkColor()
                           .WithDescription(mangaData.Synopsis.Replace("<br>",
                               Environment.NewLine,
                               StringComparison.InvariantCulture))
                           .WithTitle(mangaData.TitleEnglish)
                           .WithUrl(mangaData.Link)
                           .WithImageUrl(mangaData.ImageUrlLge)
                           .AddField(GetText(strs.chapters), mangaData.TotalChapters.ToString(), true)
                           .AddField(GetText(strs.status), mangaData.PublishingStatus, true)
                           .AddField(GetText(strs.genres),
                               string.Join(",\n", mangaData.Genres.Any() ? mangaData.Genres : ["none"]),
                               true)
                           .WithFooter($"{GetText(strs.score)} {mangaData.AverageScore} / 100");

            await Response().Embed(embed).SendAsync();
        }
    }
}