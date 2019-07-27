//#if GLOBAL_WIZBOT
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
                JToken RUID;
                JToken RStatus;
                using (var http = _httpFactory.CreateClient())
                {
                    //RChecker = JObject.Parse(await http.GetStringAsync($"https://auth.roblox.com/v2/usernames/validate?request.birthday=01%2F01%2F1990&request.username={username}").ConfigureAwait(false));
                    RInfo = JObject.Parse(await http.GetStringAsync($"https://wizbot.cf/api/v1/roblox/getPlayerInfo/{username}").ConfigureAwait(false));
                    RUID = JObject.Parse(await http.GetStringAsync($"http://api.roblox.com/users/get-by-username?username={username}").ConfigureAwait(false));
                    RStatus = JObject.Parse(await http.GetStringAsync($"http://api.roblox.com/users/{RUID["Id"]}/onlinestatus").ConfigureAwait(false));
                }
                // Currently doesn't work at this time. If you know a sulotion feel free to try.
                /* if ((bool) RChecker["code"] != false)
                {
                    await ctx.Channel.EmbedAsync(new EmbedBuilder().WithErrorColor()
                    .WithAuthor(eab => eab.WithUrl("https://roblox.com/")
                        .WithIconUrl("https://i.imgur.com/jDcWXPD.png")
                        .WithName($"Roblox Info Error"))
                    .WithDescription("The user you are trying to find doesn't exist or is banned. Please try again."))
                    .ConfigureAwait(false);
                }
                else
                { */
                    // If Roblox User Blurb and Status is nulled.
                    if ((string.IsNullOrEmpty($"{RInfo["blurb"]}")) && (string.IsNullOrEmpty($"{RInfo["status"]}")))
                        await ctx.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                            .WithAuthor(eab => eab.WithUrl("https://roblox.com/")
                                .WithIconUrl("https://i.imgur.com/jDcWXPD.png")
                                .WithName($"{RInfo["username"]}'s Roblox Info"))
                            .WithThumbnailUrl($"http://www.roblox.com:80/Thumbs/Avatar.ashx?x=100&y=100&Format=Png&username={RInfo["username"]}")
                            .AddField(fb => fb.WithName("Username").WithValue($"[{RInfo["username"]}](https://www.roblox.com/users/{RUID["Id"]}/profile)").WithIsInline(true))
                            .AddField(fb => fb.WithName("User ID").WithValue($"{RUID["Id"]}").WithIsInline(true))
                            .AddField(fb => fb.WithName("Presence").WithValue($"{RStatus["LastLocation"]}").WithIsInline(true))
                            .AddField(fb => fb.WithName("Account Age").WithValue($"{RInfo["age"]}").WithIsInline(true))
                            .AddField(fb => fb.WithName("Join Date").WithValue($"{RInfo["joinDate"]:MM.dd.yyyy HH:mm}").WithIsInline(true)))
                            .ConfigureAwait(false);
                    // If Roblox User Blurb is nulled.
                    else if (string.IsNullOrEmpty($"{RInfo["blurb"]}"))
                        await ctx.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                            .WithAuthor(eab => eab.WithUrl("https://roblox.com/")
                                .WithIconUrl("https://i.imgur.com/jDcWXPD.png")
                                .WithName($"{RInfo["username"]}'s Roblox Info"))
                            .WithThumbnailUrl($"http://www.roblox.com:80/Thumbs/Avatar.ashx?x=100&y=100&Format=Png&username={RInfo["username"]}")
                            .AddField(fb => fb.WithName("Username").WithValue($"[{RInfo["username"]}](https://www.roblox.com/users/{RUID["Id"]}/profile)").WithIsInline(true))
                            .AddField(fb => fb.WithName("User ID").WithValue($"{RUID["Id"]}").WithIsInline(true))
                            .AddField(fb => fb.WithName("Presence").WithValue($"{RStatus["LastLocation"]}").WithIsInline(true))
                            .AddField(fb => fb.WithName("Account Age").WithValue($"{RInfo["age"]}").WithIsInline(true))
                            .AddField(fb => fb.WithName("Join Date").WithValue($"{RInfo["joinDate"]:MM.dd.yyyy HH:mm}").WithIsInline(true))
                            .AddField(fb => fb.WithName("Status").WithValue($"{RInfo["status"]}").WithIsInline(false)))
                            .ConfigureAwait(false);
                    // If Roblox User Status is nulled.
                    else if (string.IsNullOrEmpty($"{RInfo["status"]}"))
                        await ctx.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                            .WithAuthor(eab => eab.WithUrl("https://roblox.com/")
                                .WithIconUrl("https://i.imgur.com/jDcWXPD.png")
                                .WithName($"{RInfo["username"]}'s Roblox Info"))
                            .WithThumbnailUrl($"http://www.roblox.com:80/Thumbs/Avatar.ashx?x=100&y=100&Format=Png&username={RInfo["username"]}")
                            .AddField(fb => fb.WithName("Username").WithValue($"[{RInfo["username"]}](https://www.roblox.com/users/{RUID["Id"]}/profile)").WithIsInline(true))
                            .AddField(fb => fb.WithName("User ID").WithValue($"{RUID["Id"]}").WithIsInline(true))
                            .AddField(fb => fb.WithName("Presence").WithValue($"{RStatus["LastLocation"]}").WithIsInline(true))
                            .AddField(fb => fb.WithName("Account Age").WithValue($"{RInfo["age"]}").WithIsInline(true))
                            .AddField(fb => fb.WithName("Join Date").WithValue($"{RInfo["joinDate"]:MM.dd.yyyy HH:mm}").WithIsInline(true))
                            .AddField(fb => fb.WithName("Blurb").WithValue($"{RInfo["blurb"]}").WithIsInline(false)))
                            .ConfigureAwait(false);
                    // If Roblox User Blurb and Status isn't nulled.
                    else
                        await ctx.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                            .WithAuthor(eab => eab.WithUrl("https://roblox.com/")
                                .WithIconUrl("https://i.imgur.com/jDcWXPD.png")
                                .WithName($"{RInfo["username"]}'s Roblox Info"))
                            .WithThumbnailUrl($"http://www.roblox.com:80/Thumbs/Avatar.ashx?x=100&y=100&Format=Png&username={RInfo["username"]}")
                            .AddField(fb => fb.WithName("Username").WithValue($"[{RInfo["username"]}](https://www.roblox.com/users/{RUID["Id"]}/profile)").WithIsInline(true))
                            .AddField(fb => fb.WithName("User ID").WithValue($"{RUID["Id"]}").WithIsInline(true))
                            .AddField(fb => fb.WithName("Presence").WithValue($"{RStatus["LastLocation"]}".TrimTo(50)).WithIsInline(true))
                            .AddField(fb => fb.WithName("Account Age").WithValue($"{RInfo["age"]}").WithIsInline(true))
                            .AddField(fb => fb.WithName("Join Date").WithValue($"{RInfo["joinDate"]:MM.dd.yyyy HH:mm}").WithIsInline(true))
                            .AddField(fb => fb.WithName("Status").WithValue($"{RInfo["status"]}").WithIsInline(true))
                            .AddField(fb => fb.WithName("Blurb").WithValue($"{RInfo["blurb"]}").WithIsInline(false)))
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
//#endif