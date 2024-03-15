using Newtonsoft.Json.Linq;
using System.Net;
using WizBot.Modules.Searches.Services;

namespace WizBot.Modules.Roblox;

// Basic Roblox command using Roblox API.
// Official Roblox API can be found at: http://api.roblox.com/docs
// More Roblox API info at: https://api.roblox.com/docs?useConsolidatedPage=true

public partial class Roblox : WizBotModule<SearchesService>
{
    private readonly IHttpClientFactory _httpFactory;

    public Roblox(IHttpClientFactory factory)
    {
        _httpFactory = factory;
    }

    // Code is a bit messy as this was a temp solution to fix the nulled issue.
    // This code can be redone and clean up if anyone willing to do it.
    [Cmd]
    [Ratelimit(10)]
    public async Task RbxInfo([Remainder] string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return;

        try
        {
            // Make a checker to see if a Roblox account exist before showing info.
            JToken rbxInfo;
            //JToken rDevForum;
            //JToken rMT;
            using (var http = _httpFactory.CreateClient())
            {
                rbxInfo = JObject.Parse(await http
                                            .GetStringAsync($"https://wizbot.cc/api/v1/roblox/getPlayerInfo/{username}")
                                            .ConfigureAwait(false));
                /*rDevForum = JObject.Parse(await http
                                                .GetStringAsync(
                                                    $"https://devforum.roblox.com/u/by-external/{rbxInfo["userId"]}.json")
                                                .ConfigureAwait(false));*/
                // Roblox Membership Type Checker
                // rMT = JObject.Parse(await http.GetStringAsync($"https://groups.roblox.com/v1/users/{rbxInfo["userId"]}/group-membership-status").ConfigureAwait(false));
            }

            var none = "N/A";
            // Roblox Membership Type Checker
            /* if ((int)rMT["membershipType"] == 4)
            {
                rMT["membershipType"] = "Premium";
            }
            else
            {
                rMT["membershipType"] = "None";
            } */

            // DevForum Trust Level
            /*if ((int)rDevForum["user"]!["trust_level"]! == 1)
            {
                rDevForum["user"]!["trust_level"] = "Member";
            }
            else if ((int)rDevForum["user"]!["trust_level"]! == 2)
            {
                rDevForum["user"]!["trust_level"] = "Regular";
            }
            else if ((int)rDevForum["user"]!["trust_level"]! == 3)
            {
                rDevForum["user"]!["trust_level"] = "Community Editor";
            }
            else if ((int)rDevForum["user"]!["trust_level"]! == 4)
            {
                rDevForum["user"]!["trust_level"] = "Roblox Staff";
            }
            else
            {
                rDevForum["user"]!["trust_level"] = "Visitor";
            }*/

            var pastNames = string.Join("\n", rbxInfo["oldNames"]!.Take(5));
            if (string.IsNullOrEmpty(pastNames))
            {
                pastNames = "N/A";
            }

            await ctx.Channel.EmbedAsync(_eb.Create()
                                            .WithOkColor()
                                            .WithAuthor($"{rbxInfo["username"]}'s Roblox Info",
                                                "https://i.imgur.com/jDcWXPD.png",
                                                "https://roblox.com")
                                            .WithThumbnailUrl($"{rbxInfo["avatarBust"]}")
                                            .AddField("Username",
                                                $"[{rbxInfo["username"]}](https://www.roblox.com/users/{rbxInfo["userId"]}/profile)",
                                                true)
                                            .AddField("Display Name", $"{rbxInfo["displayName"]}", true)
                                            .AddField("User ID", $"{rbxInfo["userId"]}", true)
                                            .AddField("Verified Badge", $"{rbxInfo["verifiedBadge"]}", true)
                                            .AddField("Friends", $"{rbxInfo["friendCount"]}", true)
                                            .AddField("Followers", $"{rbxInfo["followerCount"]}", true)
                                            .AddField("Following", $"{rbxInfo["followingCount"]}", true)
                                            // .AddField("Membership", $"{rMT["membershipType"]}", true)
                                            .AddField("Account Age",
                                                string.IsNullOrEmpty($"{rbxInfo["age"]}") ? none : ($"{rbxInfo["age"]}"),
                                                true)
                                            .AddField("Join Date",
                                                string.IsNullOrEmpty($"{rbxInfo["joinDate"]}")
                                                    ? none
                                                    : ($"{rbxInfo["joinDate"]:MM.dd.yyyy HH:mm}"), true)
                                            .AddField("Blurb",
                                                (string.IsNullOrEmpty($"{rbxInfo["blurb"]}")
                                                    ? none
                                                    : ($"{rbxInfo["blurb"]}".TrimTo(170)))!)
                                            .AddField($"Past Names (" + rbxInfo["oldNames"]!.Count() + ")", pastNames))
                     .ConfigureAwait(false);

            // Add a check incase user doesn't have a devforum account.
            /*await ctx.Channel.EmbedAsync(_eb.Create()
                                            .WithOkColor()
                                            .WithAuthor($"{rbxInfo["username"]}'s DevForum Info",
                                                "https://doy2mn9upadnk.cloudfront.net/uploads/default/original/3X/a/7/a7c93ee978f5f5326adb01270f17c287771fbe81.png",
                                                "https://devforum.roblox.com")
                                            .AddField("Username",
                                                $"[{rbxInfo["username"]}](https://devforum.roblox.com/u/{rDevForum["user"]!["username"]})",
                                                true)
                                            .AddField("Title",
                                                string.IsNullOrEmpty($"{rDevForum["user"]!["title"]}")
                                                    ? none
                                                    : ($"{rDevForum["user"]!["title"]}"), true)
                                            .AddField("Trust Level", $"{rDevForum["user"]!["trust_level"]}", true)
                                            .AddField("Bio", (string.IsNullOrEmpty($"{rDevForum["user"]!["bio_raw"]}")
                                                ? none
                                                : ($"{rDevForum["user"]!["bio_raw"]}".TrimTo(170)))!))
                     .ConfigureAwait(false);*/
        }
        catch (Exception ex)
        {
            await SendErrorAsync(ex.Message).ConfigureAwait(false);
        }
    }
}