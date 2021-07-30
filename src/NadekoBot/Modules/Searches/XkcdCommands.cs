using Discord;
using Discord.Commands;
using NadekoBot.Extensions;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;
using NadekoBot.Common;
using NadekoBot.Common.Attributes;

namespace NadekoBot.Modules.Searches
{
    public partial class Searches
    {
        [Group]
        public class XkcdCommands : NadekoSubmodule
        {
            private const string _xkcdUrl = "https://xkcd.com";
            private readonly IHttpClientFactory _httpFactory;

            public XkcdCommands(IHttpClientFactory factory)
            {
                _httpFactory = factory;
            }

            [NadekoCommand, Aliases]
            [Priority(0)]
            public async Task Xkcd(string arg = null)
            {
                if (arg?.ToLowerInvariant().Trim() == "latest")
                {
                    try
                    {
                        using (var http = _httpFactory.CreateClient())
                        {
                            var res = await http.GetStringAsync($"{_xkcdUrl}/info.0.json").ConfigureAwait(false);
                            var comic = JsonConvert.DeserializeObject<XkcdComic>(res);
                            var embed = _eb.Create().WithOkColor()
                                .WithImageUrl(comic.ImageLink)
                                .WithAuthor(comic.Title, "https://xkcd.com/s/919f27.ico", $"{_xkcdUrl}/{comic.Num}")
                                .AddField(GetText(strs.comic_number), comic.Num.ToString(), true)
                                .AddField(GetText(strs.date), $"{comic.Month}/{comic.Year}", true);
                            var sent = await ctx.Channel.EmbedAsync(embed)
                                         .ConfigureAwait(false);

                            await Task.Delay(10000).ConfigureAwait(false);

                            await sent.ModifyAsync(m => m.Embed = embed.AddField("Alt", comic.Alt.ToString(), false).Build()).ConfigureAwait(false);
                        }
                    }
                    catch (HttpRequestException)
                    {
                        await ReplyErrorLocalizedAsync(strs.comic_not_found).ConfigureAwait(false);
                    }
                    return;
                }
                await Xkcd(new NadekoRandom().Next(1, 1750)).ConfigureAwait(false);
            }

            [NadekoCommand, Aliases]
            [Priority(1)]
            public async Task Xkcd(int num)
            {
                if (num < 1)
                    return;
                try
                {
                    using (var http = _httpFactory.CreateClient())
                    {
                        var res = await http.GetStringAsync($"{_xkcdUrl}/{num}/info.0.json").ConfigureAwait(false);

                        var comic = JsonConvert.DeserializeObject<XkcdComic>(res);
                        var embed = _eb.Create()
                            .WithOkColor()
                            .WithImageUrl(comic.ImageLink)
                            .WithAuthor(comic.Title, "https://xkcd.com/s/919f27.ico", $"{_xkcdUrl}/{num}")
                            .AddField(GetText(strs.comic_number), comic.Num.ToString(), true)
                            .AddField(GetText(strs.date), $"{comic.Month}/{comic.Year}", true);
                        
                        var sent = await ctx.Channel.EmbedAsync(embed)
                                     .ConfigureAwait(false);

                        await Task.Delay(10000).ConfigureAwait(false);

                        await sent.ModifyAsync(m => m.Embed = embed.AddField("Alt", comic.Alt.ToString(), false).Build()).ConfigureAwait(false);
                    }
                }
                catch (HttpRequestException)
                {
                    await ReplyErrorLocalizedAsync(strs.comic_not_found).ConfigureAwait(false);
                }
            }
        }

        public class XkcdComic
        {
            public int Num { get; set; }
            public string Month { get; set; }
            public string Year { get; set; }
            [JsonProperty("safe_title")]
            public string Title { get; set; }
            [JsonProperty("img")]
            public string ImageLink { get; set; }
            public string Alt { get; set; }
        }
    }
}
