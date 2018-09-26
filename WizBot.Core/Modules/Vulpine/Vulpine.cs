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
using WizBot.Modules.Searches.Exceptions;
using System.Net.Http;

namespace WizBot.Modules.Vulpine
{
    // Basic Roblox command using Vulpine API.
    public class Vulpine : WizBotTopLevelModule<SearchesService>
    {
        private readonly IHttpClientFactory _httpFactory;

        public Vulpine(IHttpClientFactory factory)
        {
            _httpFactory = factory;
        }

        // Code is a bit messy as this was a temp solution to fix the nulled issue.
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
                using (var http = _httpFactory.CreateClient())
                {
                    //RChecker = JObject.Parse(await http.GetStringAsync($"https://www.roblox.com/UserCheck/doesusernameexist?username={username}"));
                    RInfo = JObject.Parse(await http.GetStringAsync($"https://vulpineutility.net/api/v1/getPlayerInfo/username/{username}").ConfigureAwait(false));
                }
                if((string.IsNullOrEmpty($"{RInfo["blurb"]}")) && (string.IsNullOrEmpty($"{RInfo["status"]}")))
                    await Context.Channel.EmbedAsync(new EmbedBuilder().WithColor(Color.Blue)
                        .WithAuthor(eab => eab.WithUrl("https://vulpineutility.net/")
                            .WithIconUrl("https://i.imgur.com/cqx791R.jpg")
                            .WithName($"Vulpine - Roblox Info"))
                        .WithThumbnailUrl($"http://www.roblox.com:80/Thumbs/Avatar.ashx?x=100&y=100&Format=Png&username={RInfo["username"]}")
                        .AddField(fb => fb.WithName("Username").WithValue($"[{RInfo["username"]}](https://www.roblox.com/users/{RInfo["userid"]}/profile)").WithIsInline(true))
                        .AddField(fb => fb.WithName("User ID").WithValue($"{RInfo["userid"]}").WithIsInline(true))
                        .AddField(fb => fb.WithName("Account Age").WithValue($"{RInfo["age"]}").WithIsInline(true))
                        .AddField(fb => fb.WithName("Join Date").WithValue($"{RInfo["joinDate"]:MM.dd.yyyy HH:mm}").WithIsInline(true))
                        .WithFooter("© Vulpine Utility"))
                        .ConfigureAwait(false);
                else if (string.IsNullOrEmpty($"{RInfo["blurb"]}"))
                    await Context.Channel.EmbedAsync(new EmbedBuilder().WithColor(Color.Blue)
                        .WithAuthor(eab => eab.WithUrl("https://vulpineutility.net/")
                            .WithIconUrl("https://i.imgur.com/cqx791R.jpg")
                            .WithName($"Vulpine - Roblox Info"))
                        .WithThumbnailUrl($"http://www.roblox.com:80/Thumbs/Avatar.ashx?x=100&y=100&Format=Png&username={RInfo["username"]}")
                        .AddField(fb => fb.WithName("Username").WithValue($"[{RInfo["username"]}](https://www.roblox.com/users/{RInfo["userid"]}/profile)").WithIsInline(true))
                        .AddField(fb => fb.WithName("User ID").WithValue($"{RInfo["userid"]}").WithIsInline(true))
                        .AddField(fb => fb.WithName("Account Age").WithValue($"{RInfo["age"]}").WithIsInline(true))
                        .AddField(fb => fb.WithName("Join Date").WithValue($"{RInfo["joinDate"]:MM.dd.yyyy HH:mm}").WithIsInline(true))
                        .AddField(fb => fb.WithName("Status").WithValue($"{RInfo["status"]}").WithIsInline(false))
                        .WithFooter("© Vulpine Utility"))
                        .ConfigureAwait(false);
                else if (string.IsNullOrEmpty($"{RInfo["status"]}"))
                     await Context.Channel.EmbedAsync(new EmbedBuilder().WithColor(Color.Blue)
                        .WithAuthor(eab => eab.WithUrl("https://vulpineutility.net/")
                            .WithIconUrl("https://i.imgur.com/cqx791R.jpg")
                            .WithName($"Vulpine - Roblox Info"))
                        .WithThumbnailUrl($"http://www.roblox.com:80/Thumbs/Avatar.ashx?x=100&y=100&Format=Png&username={RInfo["username"]}")
                        .AddField(fb => fb.WithName("Username").WithValue($"[{RInfo["username"]}](https://www.roblox.com/users/{RInfo["userid"]}/profile)").WithIsInline(true))
                        .AddField(fb => fb.WithName("User ID").WithValue($"{RInfo["userid"]}").WithIsInline(true))
                        .AddField(fb => fb.WithName("Account Age").WithValue($"{RInfo["age"]}").WithIsInline(true))
                        .AddField(fb => fb.WithName("Join Date").WithValue($"{RInfo["joinDate"]:MM.dd.yyyy HH:mm}").WithIsInline(true))
                        .AddField(fb => fb.WithName("Blurb").WithValue($"{RInfo["blurb"]}").WithIsInline(false))
                        .WithFooter("© Vulpine Utility"))
                        .ConfigureAwait(false);
                else
                    await Context.Channel.EmbedAsync(new EmbedBuilder().WithColor(Color.Blue)
                        .WithAuthor(eab => eab.WithUrl("https://vulpineutility.net/")
                            .WithIconUrl("https://i.imgur.com/cqx791R.jpg")
                            .WithName($"Vulpine - Roblox Info"))
                        .WithThumbnailUrl($"http://www.roblox.com:80/Thumbs/Avatar.ashx?x=100&y=100&Format=Png&username={RInfo["username"]}")
                        .AddField(fb => fb.WithName("Username").WithValue($"[{RInfo["username"]}](https://www.roblox.com/users/{RInfo["userid"]}/profile)").WithIsInline(true))
                        .AddField(fb => fb.WithName("User ID").WithValue($"{RInfo["userid"]}").WithIsInline(true))
                        .AddField(fb => fb.WithName("Account Age").WithValue($"{RInfo["age"]}").WithIsInline(true))
                        .AddField(fb => fb.WithName("Join Date").WithValue($"{RInfo["joinDate"]:MM.dd.yyyy HH:mm}").WithIsInline(true))
                        .AddField(fb => fb.WithName("Status").WithValue($"{RInfo["status"]}").WithIsInline(true))
                        .AddField(fb => fb.WithName("Blurb").WithValue($"{RInfo["blurb"]}").WithIsInline(false))
                        .WithFooter("© Vulpine Utility"))
                        .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await Context.Channel.SendErrorAsync(ex.Message).ConfigureAwait(false);
            }
        }
    }
}
#endif