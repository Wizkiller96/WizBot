﻿using Discord;
using Discord.Commands;
using WizBot.Attributes;
using WizBot.Extensions;
using WizBot.Services;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

//todo drawing
namespace WizBot.Modules.Searches
{
    public partial class Searches
    {
        private class ChampionNameComparer : IEqualityComparer<JToken>
        {
            public bool Equals(JToken a, JToken b) => a["name"].ToString() == b["name"].ToString();

            public int GetHashCode(JToken obj) =>
                obj["name"].GetHashCode();
        }

        private static string[] trashTalk { get; } = { "Better ban your counters. You are going to carry the game anyway.",
                                                "Go with the flow. Don't think. Just ban one of these.",
                                                "DONT READ BELOW! Ban Urgot mid OP 100%. Im smurf Diamond 1.",
                                                "Ask your teammates what would they like to play, and ban that.",
                                                "If you consider playing teemo, do it. If you consider teemo, you deserve him.",
                                                "Doesn't matter what you ban really. Enemy will ban your main and you will lose." };


        [WizBotCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Lolban(IUserMessage umsg)
        {
            var channel = (ITextChannel)umsg.Channel;



            var showCount = 8;
            //http://api.champion.gg/stats/champs/mostBanned?api_key=YOUR_API_TOKEN&page=1&limit=2
            try
            {
                using (var http = new HttpClient())
                {
                    var data = JObject.Parse(await http.GetStringAsync($"http://api.champion.gg/stats/champs/mostBanned?" +
                                                    $"api_key={WizBot.Credentials.LoLApiKey}&page=1&" +
                                                    $"limit={showCount}")
                                                    .ConfigureAwait(false))["data"] as JArray;
                    var dataList = data.Distinct(new ChampionNameComparer()).Take(showCount).ToList();
                    var eb = new EmbedBuilder().WithColor(WizBot.OkColor).WithTitle(Format.Underline($"{dataList.Count} most banned champions"));
                    for (var i = 0; i < dataList.Count; i++)
                    {
                        var champ = dataList[i];
                        eb.AddField(efb => efb.WithName(champ["name"].ToString()).WithValue(champ["general"]["banRate"] + "%").WithIsInline(true));
                    }

                    await channel.EmbedAsync(eb.Build(), Format.Italics(trashTalk[new WizBotRandom().Next(0, trashTalk.Length)])).ConfigureAwait(false);
                }
            }
            catch (Exception)
            {
                await channel.SendMessageAsync("Something went wrong.").ConfigureAwait(false);
            }
        }
    }
}

//        private class CachedChampion
//        {
//            public System.IO.Stream ImageStream { get; set; }
//            public DateTime AddedAt { get; set; }
//            public string Name { get; set; }
//        }

//        
//        private static Dictionary<string, CachedChampion> CachedChampionImages = new Dictionary<string, CachedChampion>();

//        private System.Timers.Timer clearTimer { get; } = new System.Timers.Timer();
//        public LoLCommands(DiscordModule module) : base(module)
//        {
//            clearTimer.Interval = new TimeSpan(0, 10, 0).TotalMilliseconds;
//            clearTimer.Start();
//            clearTimer.Elapsed += (s, e) =>
//            {
//                try
//                {
//                    CachedChampionImages = CachedChampionImages
//                        .Where(kvp => DateTime.Now - kvp.Value.AddedAt > new TimeSpan(1, 0, 0))
//                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
//                }
//                catch { }
//            };
//        }

//        public Func<CommandEventArgs, Task> DoFunc()
//        {
//            throw new NotImplementedException();
//        }

//        private class MatchupModel
//        {
//            public int Games { get; set; }
//            public float WinRate { get; set; }
//            [Newtonsoft.Json.JsonProperty("key")]
//            public string Name { get; set; }
//            public float StatScore { get; set; }
//        }

//        public override void Init(CommandGroupBuilder cgb)
//        {
//            cgb.CreateCommand(Module.Prefix + "lolchamp")
//                  .Description($"Shows League Of Legends champion statistics. If there are spaces/apostrophes or in the name - omit them. Optional second parameter is a role. |`{Prefix}lolchamp Riven` or `{Prefix}lolchamp Annie sup`")
//                  .Parameter("champ", ParameterType.Required)
//                  .Parameter("position", ParameterType.Unparsed)
//                  .Do(async e =>
//                  {
//                      try
//                      {
//                          //get role
//                          var role = ResolvePos(position);
//                          var resolvedRole = role;
//                          var name = champ.Replace(" ", "").ToLower();
//                          CachedChampion champ = null;

//                          if (CachedChampionImages.TryGetValue(name + "_" + resolvedRole, out champ))
//                              if (champ != null)
//                              {
//                                  champ.ImageStream.Position = 0;
//                                  await e.Channel.SendFile("champ.png", champ.ImageStream).ConfigureAwait(false);
//                                  return;
//                              }
//                          var allData = JArray.Parse(await Classes.http.GetStringAsync($"http://api.champion.gg/champion/{name}?api_key={WizBot.Credentials.LOLAPIKey}").ConfigureAwait(false));
//                          JToken data = null;
//                          if (role != null)
//                          {
//                              for (var i = 0; i < allData.Count; i++)
//                              {
//                                  if (allData[i]["role"].ToString().Equals(role))
//                                  {
//                                      data = allData[i];
//                                      break;
//                                  }
//                              }
//                              if (data == null)
//                              {
//                                  await channel.SendMessageAsync("💢 Data for that role does not exist.").ConfigureAwait(false);
//                                  return;
//                              }
//                          }
//                          else
//                          {
//                              data = allData[0];
//                              role = allData[0]["role"].ToString();
//                              resolvedRole = ResolvePos(role);
//                          }
//                          if (CachedChampionImages.TryGetValue(name + "_" + resolvedRole, out champ))
//                              if (champ != null)
//                              {
//                                  champ.ImageStream.Position = 0;
//                                  await e.Channel.SendFile("champ.png", champ.ImageStream).ConfigureAwait(false);
//                                  return;
//                              }
//                          //name = data["title"].ToString();
//                          // get all possible roles, and "select" the shown one
//                          var roles = new string[allData.Count];
//                          for (var i = 0; i < allData.Count; i++)
//                          {
//                              roles[i] = allData[i]["role"].ToString();
//                              if (roles[i] == role)
//                                  roles[i] = ">" + roles[i] + "<";
//                          }
//                          var general = JArray.Parse(await http.GetStringAsync($"http://api.champion.gg/stats/" +
//                                                                                               $"champs/{name}?api_key={WizBot.Credentials.LOLAPIKey}")
//                                                                                                .ConfigureAwait(false))
//                                              .FirstOrDefault(jt => jt["role"].ToString() == role)?["general"];
//                          if (general == null)
//                          {
//                              Console.WriteLine("General is null.");
//                              return;
//                          }
//                          //get build data for this role
//                          var buildData = data["items"]["mostGames"]["items"];
//                          var items = new string[6];
//                          for (var i = 0; i < 6; i++)
//                          {
//                              items[i] = buildData[i]["id"].ToString();
//                          }

//                          //get matchup data to show counters and countered champions
//                          var matchupDataIE = data["matchups"].ToObject<List<MatchupModel>>();

//                          var matchupData = matchupDataIE.OrderBy(m => m.StatScore).ToArray();

//                          var countered = new[] { matchupData[0].Name, matchupData[1].Name, matchupData[2].Name };
//                          var counters = new[] { matchupData[matchupData.Length - 1].Name, matchupData[matchupData.Length - 2].Name, matchupData[matchupData.Length - 3].Name };

//                          //get runes data
//                          var runesJArray = data["runes"]["mostGames"]["runes"] as JArray;
//                          var runes = string.Join("\n", runesJArray.OrderBy(jt => int.Parse(jt["number"].ToString())).Select(jt => jt["number"].ToString() + "x" + jt["name"]));

//                          // get masteries data

//                          var masteries = (data["masteries"]["mostGames"]["masteries"] as JArray);

//                          //get skill order data<API_KEY>

//                          var orderArr = (data["skills"]["mostGames"]["order"] as JArray);

//                          var img = Image.FromFile("data/lol/bg.png");
//                          using (var g = Graphics.FromImage(img))
//                          {
//                              g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
//                              //g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
//                              const int margin = 5;
//                              const int imageSize = 75;
//                              var normalFont = new Font("Monaco", 8, FontStyle.Regular);
//                              var smallFont = new Font("Monaco", 7, FontStyle.Regular);
//                              //draw champ image
//                              var champName = data["key"].ToString().Replace(" ", "");

//                              g.DrawImage(GetImage(champName), new Rectangle(margin, margin, imageSize, imageSize));
//                              //draw champ name
//                              if (champName == "MonkeyKing")
//                                  champName = "Wukong";
//                              g.DrawString($"{champName}", new Font("Times New Roman", 24, FontStyle.Regular), Brushes.WhiteSmoke, margin + imageSize + margin, margin);
//                              //draw champ surname

//                              //draw skill order
//                              if (orderArr.Count != 0)
//                              {
//                                  float orderFormula = 120 / orderArr.Count;
//                                  const float orderVerticalSpacing = 10;
//                                  for (var i = 0; i < orderArr.Count; i++)
//                                  {
//                                      var orderX = margin + margin + imageSize + orderFormula * i + i;
//                                      float orderY = margin + 35;
//                                      var spellName = orderArr[i].ToString().ToLowerInvariant();

//                                      switch (spellName)
//                                      {
//                                          case "w":
//                                              orderY += orderVerticalSpacing;
//                                              break;
//                                          case "e":
//                                              orderY += orderVerticalSpacing * 2;
//                                              break;
//                                          case "r":
//                                              orderY += orderVerticalSpacing * 3;
//                                              break;
//                                          default:
//                                              break;
//                                      }

//                                      g.DrawString(spellName.ToUpperInvariant(), new Font("Monaco", 7), Brushes.LimeGreen, orderX, orderY);
//                                  }
//                              }
//                              //draw roles
//                              g.DrawString("Roles: " + string.Join(", ", roles), normalFont, Brushes.WhiteSmoke, margin, margin + imageSize + margin);

//                              //draw average stats
//                              g.DrawString(
//$@"    Average Stats

//Kills: {general["kills"]}       CS: {general["minionsKilled"]}
//Deaths: {general["deaths"]}   Win: {general["winPercent"]}%
//Assists: {general["assists"]}  Ban: {general["banRate"]}%
//", normalFont, Brushes.WhiteSmoke, img.Width - 150, margin);
//                              //draw masteries
//                              g.DrawString($"Masteries: {string.Join(" / ", masteries?.Select(jt => jt["total"]))}", normalFont, Brushes.WhiteSmoke, margin, margin + imageSize + margin + 20);
//                              //draw runes
//                              g.DrawString($"{runes}", smallFont, Brushes.WhiteSmoke, margin, margin + imageSize + margin + 40);
//                              //draw counters
//                              g.DrawString($"Best against", smallFont, Brushes.WhiteSmoke, margin, img.Height - imageSize + margin);
//                              var smallImgSize = 50;

//                              for (var i = 0; i < counters.Length; i++)
//                              {
//                                  g.DrawImage(GetImage(counters[i]),
//                                              new Rectangle(i * (smallImgSize + margin) + margin, img.Height - smallImgSize - margin,
//                                              smallImgSize,
//                                              smallImgSize));
//                              }
//                              //draw countered by
//                              g.DrawString($"Worst against", smallFont, Brushes.WhiteSmoke, img.Width - 3 * (smallImgSize + margin), img.Height - imageSize + margin);

//                              for (var i = 0; i < countered.Length; i++)
//                              {
//                                  var j = countered.Length - i;
//                                  g.DrawImage(GetImage(countered[i]),
//                                              new Rectangle(img.Width - (j * (smallImgSize + margin) + margin), img.Height - smallImgSize - margin,
//                                              smallImgSize,
//                                              smallImgSize));
//                              }
//                              //draw item build
//                              g.DrawString("Popular build", normalFont, Brushes.WhiteSmoke, img.Width - (3 * (smallImgSize + margin) + margin), 77);

//                              for (var i = 0; i < 6; i++)
//                              {
//                                  var inverseI = 5 - i;
//                                  var j = inverseI % 3 + 1;
//                                  var k = inverseI / 3;
//                                  g.DrawImage(GetImage(items[i], GetImageType.Item),
//                                              new Rectangle(img.Width - (j * (smallImgSize + margin) + margin), 92 + k * (smallImgSize + margin),
//                                              smallImgSize,
//                                              smallImgSize));
//                              }
//                          }
//                          var cachedChamp = new CachedChampion { AddedAt = DateTime.Now, ImageStream = img.ToStream(System.Drawing.Imaging.ImageFormat.Png), Name = name.ToLower() + "_" + resolvedRole };
//                          CachedChampionImages.Add(cachedChamp.Name, cachedChamp);
//                          await e.Channel.SendFile(data["title"] + "_stats.png", cachedChamp.ImageStream).ConfigureAwait(false);
//                      }
//                      catch (Exception ex)
//                      {
//                          Console.WriteLine(ex);
//                          await channel.SendMessageAsync("💢 Failed retreiving data for that champion.").ConfigureAwait(false);
//                      }
//                  });
//        }

//        private enum GetImageType
//        {
//            Champion,
//            Item
//        }
//        private static Image GetImage(string id, GetImageType imageType = GetImageType.Champion)
//        {
//            try
//            {
//                switch (imageType)
//                {
//                    case GetImageType.Champion:
//                        return Image.FromFile($"data/lol/champions/{id}.png");
//                    case GetImageType.Item:
//                    default:
//                        return Image.FromFile($"data/lol/items/{id}.png");
//                }
//            }
//            catch (Exception)
//            {
//                return Image.FromFile("data/lol/_ERROR.png");
//            }
//        }

//        private static string ResolvePos(string pos)
//        {
//            if (string.IsNullOrWhiteSpace(pos))
//                return null;
//            switch (pos.ToLowerInvariant())
//            {
//                case "m":
//                case "mid":
//                case "midorfeed":
//                case "midd":
//                case "middle":
//                    return "Middle";
//                case "top":
//                case "topp":
//                case "t":
//                case "toporfeed":
//                    return "Top";
//                case "j":
//                case "jun":
//                case "jungl":
//                case "jungle":
//                    return "Jungle";
//                case "a":
//                case "ad":
//                case "adc":
//                case "carry":
//                case "ad carry":
//                case "adcarry":
//                case "c":
//                    return "ADC";
//                case "s":
//                case "sup":
//                case "supp":
//                case "support":
//                    return "Support";
//                default:
//                    return pos;
//            }
//        }
//    }
//}
