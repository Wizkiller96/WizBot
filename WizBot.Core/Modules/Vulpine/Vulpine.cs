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

        [WizBotCommand, Usage, Description, Aliases]
        public async Task RInfo([Remainder] string username = null)
        {
            if (string.IsNullOrWhiteSpace(username))
                return;

            try
            {
                JToken RInfo;
                using (var http = _httpFactory.CreateClient())
                {
                    RInfo = JObject.Parse(await http.GetStringAsync($"https://vulpineutility.net/api/v1/getPlayerInfo/username/{username}").ConfigureAwait(false));
                }
                await Context.Channel.EmbedAsync(new EmbedBuilder().WithColor(Color.Blue)
                    .WithAuthor(eab => eab.WithUrl("https://vulpineutility.net/")
                        .WithIconUrl("https://i.imgur.com/cqx791R.jpg")
                        .WithName($"Vulpine - Roblox Info"))
                    .WithThumbnailUrl($"http://www.roblox.com:80/Thumbs/Avatar.ashx?x=100&y=100&Format=Png&username={RInfo["username"]}")
                    .AddField(fb => fb.WithName("Username").WithValue($"{RInfo["username"]}").WithIsInline(false))
                    .AddField(fb => fb.WithName("Status").WithValue($"{RInfo["status"]}").WithIsInline(false))
                    .AddField(fb => fb.WithName("Blurb").WithValue($"{RInfo["blurb"]}").WithIsInline(false))
                    .AddField(fb => fb.WithName("Account Age").WithValue($"{RInfo["age"]}").WithIsInline(false))
                    .AddField(fb => fb.WithName("Join Date").WithValue($"{RInfo["joinDate"]:MM.dd.yyyy HH:mm}").WithIsInline(false))
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
