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
                JToken RInfo;
                //JToken RUID;
                JToken RStatus;
                //JToken RMT;
                using (var http = _httpFactory.CreateClient())
                {
                    RInfo = JObject.Parse(await http.GetStringAsync($"https://wizbot.cc/api/v1/roblox/getPlayerInfo/{username}").ConfigureAwait(false));
                    // RUID = JObject.Parse(await http.GetStringAsync($"http://api.roblox.com/users/get-by-username?username={username}").ConfigureAwait(false)); // Backup UserId
                    RStatus = JObject.Parse(await http.GetStringAsync($"http://api.roblox.com/users/{RInfo["userid"]}/onlinestatus").ConfigureAwait(false));
                    // Roblox Membership Type Checker
                    // RMT = JObject.Parse(await http.GetStringAsync($"https://groups.roblox.com/v1/users/{RInfo["userid"]}/group-membership-status").ConfigureAwait(false));
                }
                // Currently doesn't work at this time. If you know a sulotion feel free to try.
                // if ((int)RInfo["ErrorCode"] == 404)
                // {
                //     await ctx.Channel.EmbedAsync(new EmbedBuilder().WithErrorColor()
                //     .WithAuthor(eab => eab.WithUrl("https://roblox.com/")
                //         .WithIconUrl("https://i.imgur.com/jDcWXPD.png")
                //         .WithName($"Roblox Info Error"))
                //     .WithDescription("The user you are trying to find doesn't exist or is banned. Please try again."))
                //     .ConfigureAwait(false);
                // }
                // else
                // {
                // If Roblox User Blurb and Status is nulled.
                var none = "N/A";
                // Roblox Membership Type Checker
                /* if ((int)RMT["membershipType"] == 4)
                {
                    RMT["membershipType"] = "Premium";
                }
                else
                {
                    RMT["membershipType"] = "None";
                } */
                // Roblox Ban Check
                /* if ((bool)RInfo["isBanned"] == true)
                {
                    RInfo["isBanned"] = "Yes";
                }
                else
                {
                    RInfo["isBanned"] = "No";
                } */
                var pastNames = string.Join("\n", RInfo["oldNames"]!.Take(5));
                if (string.IsNullOrEmpty(pastNames))
                {
                    pastNames = "N/A";
                }
                await ctx.Channel.EmbedAsync(_eb.Create().WithOkColor()
                    .WithAuthor($"{RInfo["username"]}'s Roblox Info", 
                        "https://i.imgur.com/jDcWXPD.png",
                        "https://roblox.com")
                    .WithThumbnailUrl($"https://assetgame.roblox.com/Thumbs/Avatar.ashx?username={RInfo["username"]}")
                    .AddField("Username", $"[{RInfo["username"]}](https://www.roblox.com/users/{RInfo["userid"]}/profile)", true)
                    .AddField("User ID", $"{RInfo["userid"]}", true)
                    // .AddField("Banned", $"{RInfo["isBanned"]}", true)
                    .AddField("Friends", $"{RInfo["friendCount"]}", true)
                    .AddField("Followers", $"{RInfo["followerCount"]}", true)
                    .AddField("Following", $"{RInfo["followingCount"]}", true)
                    // .AddField("Membership", $"{RMT["membershipType"]}", true)
                    .AddField("Presence", $"{RStatus["LastLocation"]}", true)
                    .AddField("Account Age", string.IsNullOrEmpty($"{RInfo["age"]}") ? none : ($"{RInfo["age"]}"), true)
                    .AddField("Join Date", string.IsNullOrEmpty($"{RInfo["joinDate"]}") ? none : ($"{RInfo["joinDate"]:MM.dd.yyyy HH:mm}"), true)
                    .AddField("Status", string.IsNullOrEmpty($"{RInfo["status"]}") ? none : ($"{RInfo["status"]}"), false)
                    .AddField("Blurb", string.IsNullOrEmpty($"{RInfo["blurb"]}") ? none : ($"{RInfo["blurb"]}".TrimTo(170)), false)
                    .AddField($"Past Names (" + RInfo["oldNames"]!.Count() + ")", pastNames, false))
                .ConfigureAwait(false);
                //}
            }
            catch (Exception ex)
            {
                await SendErrorAsync(ex.Message).ConfigureAwait(false);
            }
        }
    }