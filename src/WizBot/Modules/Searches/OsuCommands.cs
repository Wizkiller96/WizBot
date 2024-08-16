#nullable disable
using WizBot.Modules.Searches.Common;
using Newtonsoft.Json;

namespace WizBot.Modules.Searches;

public partial class Searches
{
    [Group]
    public partial class OsuCommands : WizBotModule<OsuService>
    {
        private readonly IBotCredentials _creds;
        private readonly IHttpClientFactory _httpFactory;

        public OsuCommands(IBotCredentials creds, IHttpClientFactory factory)
        {
            _creds = creds;
            _httpFactory = factory;
        }

        [Cmd]
        public async Task Osu(string user, [Leftover] string mode = null)
        {
            if (string.IsNullOrWhiteSpace(user))
                return;

            try
            {
                if (string.IsNullOrWhiteSpace(_creds.OsuApiKey))
                {
                    await Response().Error(strs.osu_api_key).SendAsync();
                    return;
                }

                var obj = await _service.GetOsuData(user, mode);
                if (obj is null)
                {
                    await Response().Error(strs.osu_user_not_found).SendAsync();
                    return;
                }

                var userId = obj.UserId;
                var smode = OsuService.ResolveGameMode(obj.ModeNumber);


                await Response()
                      .Embed(_sender.CreateEmbed()
                                    .WithOkColor()
                                    .WithTitle($"osu! {smode} profile for {user}")
                                    .WithThumbnailUrl($"https://a.ppy.sh/{userId}")
                                    .WithDescription($"https://osu.ppy.sh/u/{userId}")
                                    .AddField("Official Rank", $"#{obj.PpRank}", true)
                                    .AddField("Country Rank",
                                        $"#{obj.PpCountryRank} :flag_{obj.Country.ToLower()}:",
                                        true)
                                    .AddField("Total PP", Math.Round(obj.PpRaw, 2), true)
                                    .AddField("Accuracy", Math.Round(obj.Accuracy, 2) + "%", true)
                                    .AddField("Playcount", obj.Playcount, true)
                                    .AddField("Level", Math.Round(obj.Level), true))
                      .SendAsync();
            }
            catch (Exception ex)
            {
                await Response().Error(strs.osu_failed).SendAsync();
                Log.Warning(ex, "Osu command failed");
            }
        }

        [Cmd]
        public async Task Gatari(string user, [Leftover] string mode = null)
        {
            var modeNumber = OsuService.ResolveGameMode(mode);
            var modeStr = OsuService.ResolveGameMode(modeNumber);
            var (userData, userStats) = await _service.GetGatariDataAsync(user, mode);
            if (userStats is null)
            {
                await Response().Error(strs.osu_user_not_found).SendAsync();
                return;
            }

            var embed = _sender.CreateEmbed()
                               .WithOkColor()
                               .WithTitle($"osu!Gatari {modeStr} profile for {user}")
                               .WithThumbnailUrl($"https://a.gatari.pw/{userStats.Id}")
                               .WithDescription($"https://osu.gatari.pw/u/{userStats.Id}")
                               .AddField("Official Rank", $"#{userStats.Rank}", true)
                               .AddField("Country Rank",
                                   $"#{userStats.CountryRank} :flag_{userData.Country.ToLower()}:",
                                   true)
                               .AddField("Total PP", userStats.Pp, true)
                               .AddField("Accuracy", $"{Math.Round(userStats.AvgAccuracy, 2)}%", true)
                               .AddField("Playcount", userStats.Playcount, true)
                               .AddField("Level", userStats.Level, true);

            await Response().Embed(embed).SendAsync();
        }

        [Cmd]
        public async Task Osu5(string user, [Leftover] string mode = null)
        {
            if (string.IsNullOrWhiteSpace(_creds.OsuApiKey))
            {
                await Response().Error("An osu! API key is required.").SendAsync();
                return;
            }

            if (string.IsNullOrWhiteSpace(user))
            {
                await Response().Error("Please provide a username.").SendAsync();
                return;
            }

            using var http = _httpFactory.CreateClient();
            var m = 0;
            if (!string.IsNullOrWhiteSpace(mode))
                m = OsuService.ResolveGameMode(mode);

            var reqString = "https://osu.ppy.sh/api/get_user_best"
                            + $"?k={_creds.OsuApiKey}"
                            + $"&u={Uri.EscapeDataString(user)}"
                            + "&type=string"
                            + "&limit=5"
                            + $"&m={m}";

            var resString = await http.GetStringAsync(reqString);
            var obj = JsonConvert.DeserializeObject<List<OsuUserBests>>(resString);

            var mapTasks = obj.Select(async item =>
            {
                var mapReqString = "https://osu.ppy.sh/api/get_beatmaps"
                                   + $"?k={_creds.OsuApiKey}"
                                   + $"&b={item.BeatmapId}";

                var mapResString = await http.GetStringAsync(mapReqString);
                var map = JsonConvert.DeserializeObject<List<OsuMapData>>(mapResString).FirstOrDefault();
                if (map is null)
                    return default;
                var pp = Math.Round(item.Pp, 2);
                var acc = CalculateAcc(item, m);
                var mods = ResolveMods(item.EnabledMods);

                var title = $"{map.Artist}-{map.Title} ({map.Version})";
                var desc = $@"[/b/{item.BeatmapId}](https://osu.ppy.sh/b/{item.BeatmapId})
{pp + "pp",-7} | {acc + "%",-7}
";
                if (mods != "+")
                    desc += Format.Bold(mods);

                return (title, desc);
            });

            var eb = _sender.CreateEmbed().WithOkColor().WithTitle($"Top 5 plays for {user}");

            var mapData = await mapTasks.WhenAll();
            foreach (var (title, desc) in mapData.Where(x => x != default))
                eb.AddField(title, desc);

            await Response().Embed(eb).SendAsync();
        }

        //https://osu.ppy.sh/wiki/Accuracy
        private static double CalculateAcc(OsuUserBests play, int mode)
        {
            double hitPoints;
            double totalHits;
            if (mode == 0)
            {
                hitPoints = (play.Count50 * 50) + (play.Count100 * 100) + (play.Count300 * 300);
                totalHits = play.Count50 + play.Count100 + play.Count300 + play.Countmiss;
                totalHits *= 300;
            }
            else if (mode == 1)
            {
                hitPoints = (play.Countmiss * 0) + (play.Count100 * 0.5) + play.Count300;
                totalHits = (play.Countmiss + play.Count100 + play.Count300) * 300;
                hitPoints *= 300;
            }
            else if (mode == 2)
            {
                hitPoints = play.Count50 + play.Count100 + play.Count300;
                totalHits = play.Countmiss + play.Count50 + play.Count100 + play.Count300 + play.Countkatu;
            }
            else
            {
                hitPoints = (play.Count50 * 50)
                            + (play.Count100 * 100)
                            + (play.Countkatu * 200)
                            + ((play.Count300 + play.Countgeki) * 300);

                totalHits = (play.Countmiss
                             + play.Count50
                             + play.Count100
                             + play.Countkatu
                             + play.Count300
                             + play.Countgeki)
                            * 300;
            }


            return Math.Round(hitPoints / totalHits * 100, 2);
        }


        //https://github.com/ppy/osu-api/wiki#mods
        private static string ResolveMods(int mods)
        {
            var modString = "+";

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

        private static bool IsBitSet(int mods, int pos)
            => (mods & (1 << pos)) != 0;
    }
}