#if GLOBAL_WIZBOT
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
using System.Net.Http;

namespace WizBot.Modules.Roblox
{
    // Basic Roblox command using Roblox API.
    // Official Roblox API can be found at: http://api.roblox.com/docs
    // More Roblox API info at: https://api.roblox.com/docs?useConsolidatedPage=true

    public class Roblox : WizBotTopLevelModule<SearchesService>
    {
        private readonly IHttpClientFactory _httpFactory;

        public Roblox(IHttpClientFactory factory)
        {
            _httpFactory = factory;
        }

        // Code is a bit messy as this was a temp solution to fix the nulled issue.
        // This code can be redone and clean up if anyone willing to do it.
        [WizBotCommand, Usage, Description, Aliases]
        public async Task RInfo([Remainder] string username = null)
        {
            if (string.IsNullOrWhiteSpace(username))
                return;

            try
            {
                // Todo make a checker to see if a Roblox account exist before showing info.
                //JToken RChecker;
                JToken RInfo;
                //JToken RUID;
                JToken RStatus;
                JToken RMT;
                using (var http = _httpFactory.CreateClient())
                {
                    //RChecker = JObject.Parse(await http.GetStringAsync($"https://auth.roblox.com/v2/usernames/validate?request.birthday=01%2F01%2F1990&request.username={username}").ConfigureAwait(false));
                    RInfo = JObject.Parse(await http.GetStringAsync($"https://wizbot.cc/api/v1/roblox/getPlayerInfo/{username}").ConfigureAwait(false));
                    // RUID = JObject.Parse(await http.GetStringAsync($"http://api.roblox.com/users/get-by-username?username={username}").ConfigureAwait(false)); // Backup UserId
                    RStatus = JObject.Parse(await http.GetStringAsync($"http://api.roblox.com/users/{RInfo["userid"]}/onlinestatus").ConfigureAwait(false));
                    // Roblox Membership Type Checker
                    RMT = JObject.Parse(await http.GetStringAsync($"https://groups.roblox.com/v1/users/{RInfo["userid"]}/group-membership-status").ConfigureAwait(false));
                }
                // Currently doesn't work at this time. If you know a sulotion feel free to try.
                // if ((string)RInfo == "User does not exist")
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
                if ((int)RMT["membershipType"] == 4)
                {
                    RMT["membershipType"] = "Premium";
                }
                else
                {
                    RMT["membershipType"] = "None";
                }
                await ctx.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                    .WithAuthor(eab => eab.WithUrl("https://roblox.com/")
                        .WithIconUrl("https://i.imgur.com/jDcWXPD.png")
                        .WithName($"{RInfo["username"]}'s Roblox Info"))
                    .WithThumbnailUrl($"https://assetgame.roblox.com/Thumbs/Avatar.ashx?username={RInfo["username"]}")
                    .AddField(fb => fb.WithName("Username").WithValue($"[{RInfo["username"]}](https://www.roblox.com/users/{RInfo["userid"]}/profile)").WithIsInline(true))
                    .AddField(fb => fb.WithName("User ID").WithValue($"{RInfo["userid"]}").WithIsInline(true))
                    .AddField(fb => fb.WithName("Friends").WithValue($"{RInfo["friends"]}").WithIsInline(true))
                    .AddField(fb => fb.WithName("Followers").WithValue($"{RInfo["followers"]}").WithIsInline(true))
                    .AddField(fb => fb.WithName("Following").WithValue($"{RInfo["following"]}").WithIsInline(true))
                    .AddField(fb => fb.WithName("Membership").WithValue($"{RMT["membershipType"]}").WithIsInline(true))
                    .AddField(fb => fb.WithName("Presence").WithValue($"{RStatus["LastLocation"]}").WithIsInline(true))
                    .AddField(fb => fb.WithName("Account Age").WithValue($"{RInfo["age"]}").WithIsInline(true))
                    .AddField(fb => fb.WithName("Join Date").WithValue($"{RInfo["joinDate"]:MM.dd.yyyy HH:mm}").WithIsInline(true))
                    .AddField(fb => fb.WithName("Status").WithValue(string.IsNullOrEmpty($"{RInfo["status"]}") ? none : ($"{RInfo["status"]}")).WithIsInline(false))
                    .AddField(fb => fb.WithName("Blurb").WithValue(string.IsNullOrEmpty($"{RInfo["blurb"]}") ? none : ($"{RInfo["blurb"]}".TrimTo(170))).WithIsInline(false)))
                .ConfigureAwait(false);
                //}
            }
            catch (Exception ex)
            {
                await ctx.Channel.SendErrorAsync(ex.Message).ConfigureAwait(false);
            }
        }
    }
}
#endif