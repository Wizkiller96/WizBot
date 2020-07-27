using Discord;
using Discord.Commands;
using WizBot.Extensions;
using WizBot.Core.Services;
using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WizBot.Common.Attributes;

namespace WizBot.Modules.Searches
{
    public partial class Searches
    {
        [Group]
        public class OsuCommands : WizBotSubmodule
        {
            private readonly IBotCredentials _creds;
            private readonly IHttpClientFactory _httpFactory;

            public OsuCommands(IBotCredentials creds, IHttpClientFactory factory)
            {
                _creds = creds;
                _httpFactory = factory;
            }

            [WizBotCommand, Usage, Description, Aliases]
            public async Task Osu(string user, [Leftover] string mode = null)
            {
                if (string.IsNullOrWhiteSpace(user))
                    return;

                using (var http = _httpFactory.CreateClient())
                {
                    var modeNumber = string.IsNullOrWhiteSpace(mode)
                        ? 0
                        : ResolveGameMode(mode);

                    try
                    {
                        if (string.IsNullOrWhiteSpace(_creds.OsuApiKey))
                        {
                            await ReplyErrorLocalizedAsync("osu_api_key").ConfigureAwait(false);
                            return;
                        }

                        var smode = ResolveGameMode(modeNumber);
                        var result = await http
                            .GetStringAsync($"https://osu.ppy.sh/api/get_user?k={_creds.OsuApiKey}&u={user}&m={modeNumber}")
                            .ConfigureAwait(false);
                        var obj = JArray.Parse(result)[0];
                        var userId = $"{obj["user_id"]}";

                        await ctx.Channel.EmbedAsync(new EmbedBuilder()
                            .WithOkColor()
                            .WithTitle($"osu! {smode} profile for {user}")
                            .WithThumbnailUrl($"https://a.ppy.sh/{userId}")
                            .WithDescription($"URL: https://osu.ppy.sh/u/{userId}")
                            .AddField("Official Rank", $"#{obj["pp_rank"]}", true)
                            .AddField("Country Rank", $"#{obj["pp_country_rank"]}", true)
                            .AddField("Total PP",
                                Math.Round(double.Parse($"{obj["pp_raw"]}", CultureInfo.InvariantCulture)), true)
                            .AddField("Accuracy",
                                Math.Round(double.Parse($"{obj["accuracy"]}", CultureInfo.InvariantCulture), 2) + "%",
                                true)
                            .AddField("Playcount", $"{obj["playcount"]}", true)
                            .AddField("Level",
                                Math.Floor(double.Parse($"{obj["level"]}", CultureInfo.InvariantCulture)), true)
                        );

                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        await ReplyErrorLocalizedAsync("osu_user_not_found").ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        await ReplyErrorLocalizedAsync("osu_failed").ConfigureAwait(false);
                        _log.Warn(ex);
                    }
                }
            }

            [WizBotCommand, Usage, Description, Aliases]
            public async Task Gatari(string user, [Leftover] string mode = null)
            {
                using (var http = _httpFactory.CreateClient())
                {
                    var modeNumber = string.IsNullOrWhiteSpace(mode)
                        ? 0
                        : ResolveGameMode(mode);

                    var modeStr = ResolveGameMode(modeNumber);
                    var resString = await http.GetStringAsync($"https://api.gatari.pw/user/stats?u={user}&mode={modeNumber}")
                        .ConfigureAwait(false);
                    var userStats = JToken.Parse(resString)["stats"];
                    var userId = userStats?["id"]?.ToString();
                    if (string.IsNullOrWhiteSpace(userId))
                    {
                        await ReplyErrorLocalizedAsync("osu_user_not_found").ConfigureAwait(false);
                        return;
                    }

                    resString = await http.GetStringAsync($"https://api.gatari.pw/users/get?u={user}")
                        .ConfigureAwait(false);
                    var userData = JToken.Parse(resString);
                    var embed = new EmbedBuilder()
                        .WithOkColor()
                        .WithTitle($"osu!Gatari {modeStr} profile for {user}")
                        .WithThumbnailUrl($"https://a.gatari.pw/{userId}")
                        .WithDescription($"URL: https://osu.gatari.pw/u/{userId}")
                        .AddField("Official Rank", $"#{userStats["rank"]}", true)
                        .AddField("Country Rank", $"#{userStats["country_rank"]} ({userData["users"][0]["country"]})", true)
                        .AddField("Total PP", $"{userStats["pp"]}", true)
                        .AddField("Accuracy",
                            Math.Round(
                                Math.Round(double.Parse($"{userStats["avg_accuracy"]}", CultureInfo.InvariantCulture), 2) /
                                1000000000000, 2) + "%", true)
                        .AddField("Playcount", $"{userStats["playcount"]}", true)
                        .AddField("Level", $"{userStats["level"]}", true);

                    await ctx.Channel.EmbedAsync(embed);
                }
            }

            [WizBotCommand, Usage, Description, Aliases]
            public async Task Osub([Leftover] string map)
            {
                if (string.IsNullOrWhiteSpace(_creds.OsuApiKey))
                {
                    await ReplyErrorLocalizedAsync("osu_api_key").ConfigureAwait(false);
                    return;
                }

                if (string.IsNullOrWhiteSpace(map))
                    return;

                try
                {
                    using (var http = _httpFactory.CreateClient())
                    {
                        var mapId = ResolveMap(map);
                        var reqString = $"https://osu.ppy.sh/api/get_beatmaps?k={_creds.OsuApiKey}&{mapId}";
                        var obj = JArray.Parse(await http.GetStringAsync(reqString).ConfigureAwait(false))[0];
                        var sb = new System.Text.StringBuilder();
                        var starRating =
                            Math.Round(double.Parse($"{obj["difficultyrating"]}", CultureInfo.InvariantCulture), 2);
                        var time = TimeSpan.FromSeconds(double.Parse($"{obj["total_length"]}")).ToString(@"mm\:ss");
                        sb.AppendLine(
                            $"{obj["artist"]} - {obj["title"]}, mapped by {obj["creator"]}. https://osu.ppy.sh/s/{obj["beatmapset_id"]}");
                        sb.AppendLine(
                            $"{starRating} stars, {obj["bpm"]} BPM | AR{obj["diff_approach"]}, CS{obj["diff_size"]}, OD{obj["diff_overall"]} | Length: {time}");
                        await ctx.Channel.SendMessageAsync(sb.ToString()).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    await ReplyErrorLocalizedAsync("something_went_wrong").ConfigureAwait(false);
                    _log.Warn(ex);
                }
            }

            [WizBotCommand, Usage, Description, Aliases]
            public async Task Osu5(string user, [Leftover] string mode = null)
            {
                var channel = (ITextChannel)ctx.Channel;
                if (string.IsNullOrWhiteSpace(_creds.OsuApiKey))
                {
                    await channel.SendErrorAsync("An osu! API key is required.").ConfigureAwait(false);
                    return;
                }

                if (string.IsNullOrWhiteSpace(user))
                {
                    await channel.SendErrorAsync("Please provide a username.").ConfigureAwait(false);
                    return;
                }

                using (var http = _httpFactory.CreateClient())
                {
                    try
                    {
                        var m = 0;
                        if (!string.IsNullOrWhiteSpace(mode))
                        {
                            m = ResolveGameMode(mode);
                        }

                        var reqString = $"https://osu.ppy.sh/api/get_user_best" +
                                        $"?k={_creds.OsuApiKey}" +
                                        $"&u={Uri.EscapeDataString(user)}" +
                                        $"&type=string" +
                                        $"&limit=5" +
                                        $"&m={m}";

                        var obj = JArray.Parse(await http.GetStringAsync(reqString).ConfigureAwait(false));
                        var sb = new StringBuilder($"`Top 5 plays for {user}:`\n```xl" + Environment.NewLine);
                        foreach (var item in obj)
                        {
                            var mapReqString = $"https://osu.ppy.sh/api/get_beatmaps" +
                                               $"?k={_creds.OsuApiKey}" +
                                               $"&b={item["beatmap_id"]}";

                            var map = JArray.Parse(await http.GetStringAsync(mapReqString).ConfigureAwait(false))[0];
                            var pp = Math.Round(double.Parse($"{item["pp"]}", CultureInfo.InvariantCulture), 2);
                            var acc = CalculateAcc(item, m);
                            var mods = ResolveMods(int.Parse($"{item["enabled_mods"]}"));
                            sb.AppendLine(mods != "+"
                                ? $"{pp + "pp",-7} | {acc + "%",-7} | {map["artist"] + "-" + map["title"] + " (" + map["version"] + ")",-40} | **{mods,-10}** | /b/{item["beatmap_id"]}"
                                : $"{pp + "pp",-7} | {acc + "%",-7} | {map["artist"] + "-" + map["title"] + " (" + map["version"] + ")",-40}  | /b/{item["beatmap_id"]}");
                        }

                        sb.Append("```");
                        await channel.SendMessageAsync(sb.ToString()).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        await ReplyErrorLocalizedAsync("something_went_wrong").ConfigureAwait(false);
                        _log.Warn(ex);
                    }
                }
            }

            //https://osu.ppy.sh/wiki/Accuracy
            private static double CalculateAcc(JToken play, int mode)
            {
                if (mode == 0)
                {
                    var hitPoints = double.Parse($"{play["count50"]}") * 50 +
                                    double.Parse($"{play["count100"]}") * 100 +
                                    double.Parse($"{play["count300"]}") * 300;
                    var totalHits = double.Parse($"{play["count50"]}") + double.Parse($"{play["count100"]}") +
                                    double.Parse($"{play["count300"]}") + double.Parse($"{play["countmiss"]}");
                    totalHits *= 300;
                    return Math.Round(hitPoints / totalHits * 100, 2);
                }
                else if (mode == 1)
                {
                    var hitPoints = double.Parse($"{play["countmiss"]}") * 0 +
                                    double.Parse($"{play["count100"]}") * 0.5 + double.Parse($"{play["count300"]}") * 1;
                    var totalHits = double.Parse($"{play["countmiss"]}") + double.Parse($"{play["count100"]}") +
                                    double.Parse($"{play["count300"]}");
                    hitPoints *= 300;
                    totalHits *= 300;
                    return Math.Round(hitPoints / totalHits * 100, 2);
                }
                else if (mode == 2)
                {
                    var fruitsCaught = double.Parse($"{play["count50"]}") + double.Parse($"{play["count100"]}") +
                                       double.Parse($"{play["count300"]}");
                    var totalFruits = double.Parse($"{play["countmiss"]}") + double.Parse($"{play["count50"]}") +
                                      double.Parse($"{play["count100"]}") + double.Parse($"{play["count300"]}") +
                                      double.Parse($"{play["countkatu"]}");
                    return Math.Round(fruitsCaught / totalFruits * 100, 2);
                }
                else
                {
                    var hitPoints = double.Parse($"{play["count50"]}") * 50 +
                                    double.Parse($"{play["count100"]}") * 100 +
                                    double.Parse($"{play["countkatu"]}") * 200 + (double.Parse($"{play["count300"]}") +
                                        double.Parse($"{play["countgeki"]}")) * 300;
                    var totalHits = double.Parse($"{play["countmiss"]}") + double.Parse($"{play["count50"]}") +
                                    double.Parse($"{play["count100"]}") + double.Parse($"{play["countkatu"]}") +
                                    double.Parse($"{play["count300"]}") + double.Parse($"{play["countgeki"]}");
                    totalHits *= 300;
                    return Math.Round(hitPoints / totalHits * 100, 2);
                }
            }

            private static string ResolveMap(string mapLink)
            {
                var s = new Regex(@"osu.ppy.sh\/s\/", RegexOptions.IgnoreCase).Match(mapLink);
                var b = new Regex(@"osu.ppy.sh\/b\/", RegexOptions.IgnoreCase).Match(mapLink);
                var p = new Regex(@"osu.ppy.sh\/p\/", RegexOptions.IgnoreCase).Match(mapLink);
                var m = new Regex(@"&m=", RegexOptions.IgnoreCase).Match(mapLink);
                if (s.Success)
                {
                    var mapId = mapLink.Substring(mapLink.IndexOf("/s/", StringComparison.InvariantCulture) + 3);
                    return $"s={mapId}";
                }
                else if (b.Success)
                {
                    if (m.Success)
                        return
                            $"b={mapLink.Substring(mapLink.IndexOf("/b/", StringComparison.InvariantCulture) + 3, mapLink.IndexOf("&m", StringComparison.InvariantCulture) - (mapLink.IndexOf("/b/", StringComparison.InvariantCulture + 3)))}";
                    else
                        return $"b={mapLink.Substring(mapLink.IndexOf("/b/", StringComparison.InvariantCulture) + 3)}";
                }
                else if (p.Success)
                {
                    if (m.Success)
                        return
                            $"b={mapLink.Substring(mapLink.IndexOf("?b=", StringComparison.InvariantCulture) + 3, mapLink.IndexOf("&m", StringComparison.InvariantCulture) - (mapLink.IndexOf("?b=", StringComparison.InvariantCulture) + 3))}";
                    else
                        return $"b={mapLink.Substring(mapLink.IndexOf("?b=", StringComparison.InvariantCulture) + 3)}";
                }
                else
                {
                    return $"s={mapLink}"; //just a default incase an ID number was provided by itself (non-url)?
                }
            }

            private static int ResolveGameMode(string mode)
            {
                switch (mode.ToUpperInvariant())
                {
                    case "STD":
                    case "STANDARD":
                        return 0;
                    case "TAIKO":
                        return 1;
                    case "CTB":
                    case "CATCHTHEBEAT":
                        return 2;
                    case "MANIA":
                    case "OSU!MANIA":
                        return 3;
                    default:
                        return 0;
                }
            }

            private static string ResolveGameMode(int mode)
            {
                switch (mode)
                {
                    case 0:
                        return "Standard";
                    case 1:
                        return "Taiko";
                    case 2:
                        return "Catch";
                    case 3:
                        return "Mania";
                    default:
                        return "Standard";
                }
            }

            //https://github.com/ppy/osu-api/wiki#mods
            private static string ResolveMods(int mods)
            {
                var modString = $"+";

                if (IsBitSet(mods, 0))
                    modString += "NF";
                if (IsBitSet(mods, 1))
                    modString += "EZ";
                if (IsBitSet(mods, 8))
                    modString += "HT";

                if (IsBitSet(mods, 3))
                    modString += "HD";
                if (IsBitSet(mods, 4))
                    modString += "HR";
                if (IsBitSet(mods, 6) && !IsBitSet(mods, 9))
                    modString += "DT";
                if (IsBitSet(mods, 9))
                    modString += "NC";
                if (IsBitSet(mods, 10))
                    modString += "FL";

                if (IsBitSet(mods, 5))
                    modString += "SD";
                if (IsBitSet(mods, 14))
                    modString += "PF";

                if (IsBitSet(mods, 7))
                    modString += "RX";
                if (IsBitSet(mods, 11))
                    modString += "AT";
                if (IsBitSet(mods, 12))
                    modString += "SO";
                return modString;
            }

            private static bool IsBitSet(int mods, int pos) =>
                (mods & (1 << pos)) != 0;
        }
    }
}
