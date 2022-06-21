using Newtonsoft.Json.Linq;
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
        public async partial Task RInfo([Remainder] string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return;

            try
            {
                // Todo make a checker to see if a Roblox account exist before showing info.
                JToken rInfo;
                JToken rAvatar;
                //JToken rUID;
                //JToken rStatus;
                //JToken rMT;
                using (var http = _httpFactory.CreateClient())
                {
                    rInfo = JObject.Parse(await http.GetStringAsync($"https://wizbot.cc/api/v1/roblox/getPlayerInfo/{username}").ConfigureAwait(false));
                    rAvatar = JObject.Parse(await http.GetStringAsync($"https://thumbnails.roblox.com/v1/users/avatar?userIds={rInfo["userid"]}&size=720x720&format=png&isCircular=false").ConfigureAwait(false));
                    // rUID = JObject.Parse(await http.GetStringAsync($"http://api.roblox.com/users/get-by-username?username={username}").ConfigureAwait(false)); // Backup UserId
                    // rStatus = JObject.Parse(await http.GetStringAsync($"http://api.roblox.com/users/{rInfo["userid"]}/onlinestatus").ConfigureAwait(false));
                    // Roblox Membership Type Checker
                    // rMT = JObject.Parse(await http.GetStringAsync($"https://groups.roblox.com/v1/users/{rInfo["userid"]}/group-membership-status").ConfigureAwait(false));
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
                var pastNames = string.Join("\n", rInfo["oldNames"]!.Take(5));
                if (string.IsNullOrEmpty(pastNames))
                {
                    pastNames = "N/A";
                }
                await ctx.Channel.EmbedAsync(_eb.Create().WithOkColor()
                    .WithAuthor($"{rInfo["username"]}'s Roblox Info", 
                        "https://i.imgur.com/jDcWXPD.png",
                        "https://roblox.com")
                    .WithThumbnailUrl($"{rAvatar["data"]![0]!["imageUrl"]}")
                    .AddField("Username", $"[{rInfo["username"]}](https://www.roblox.com/users/{rInfo["userid"]}/profile)", true)
                    .AddField("User ID", $"{rInfo["userid"]}", true)
                    .AddField("Friends", $"{rInfo["friendCount"]}", true)
                    .AddField("Followers", $"{rInfo["followerCount"]}", true)
                    .AddField("Following", $"{rInfo["followingCount"]}", true)
                    // .AddField("Membership", $"{rMT["membershipType"]}", true)
                    // .AddField("Presence", $"{rStatus["LastLocation"]}", true)
                    .AddField("Account Age", string.IsNullOrEmpty($"{rInfo["age"]}") ? none : ($"{rInfo["age"]}"), true)
                    .AddField("Join Date", string.IsNullOrEmpty($"{rInfo["joinDate"]}") ? none : ($"{rInfo["joinDate"]:MM.dd.yyyy HH:mm}"), true)
                    .AddField("Status", string.IsNullOrEmpty($"{rInfo["status"]}") ? none : ($"{rInfo["status"]}"))
                    .AddField("Blurb", (string.IsNullOrEmpty($"{rInfo["blurb"]}") ? none : ($"{rInfo["blurb"]}".TrimTo(170)))!)
                    .AddField($"Past Names (" + rInfo["oldNames"]!.Count() + ")", pastNames))
                .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await SendErrorAsync(ex.Message).ConfigureAwait(false);
            }
        }
    }