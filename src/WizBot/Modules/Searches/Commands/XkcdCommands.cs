﻿using Discord;
using Discord.Commands;
using WizBot.Attributes;
using WizBot.Extensions;
using WizBot.Services;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;

namespace WizBot.Modules.Searches
{
    public partial class Searches
    {
        [Group]
        public class XkcdCommands
        {
            private const string xkcdUrl = "https://xkcd.com";

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [Priority(1)]
            public async Task Xkcd(IUserMessage msg, string arg = null)
            {
                var channel = (ITextChannel)msg.Channel;

                if (arg?.ToLowerInvariant().Trim() == "latest")
                {
                    using (var http = new HttpClient())
                    {
                        var res = await http.GetStringAsync($"{xkcdUrl}/info.0.json").ConfigureAwait(false);
                        var comic = JsonConvert.DeserializeObject<XkcdComic>(res);
                        var sent = await channel.SendMessageAsync($"{msg.Author.Mention} " + comic.ToString())
                                     .ConfigureAwait(false);

                        await Task.Delay(10000).ConfigureAwait(false);

                        await sent.ModifyAsync(m => m.Content = sent.Content + $"\n`Alt:` {comic.Alt}");
                    }
                    return;
                }
                await Xkcd(msg, new WizBotRandom().Next(1, 1750)).ConfigureAwait(false);
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [Priority(0)]
            public async Task Xkcd(IUserMessage msg, int num)
            {
                var channel = (ITextChannel)msg.Channel;

                if (num < 1)
                    return;

                using (var http = new HttpClient())
                {
                    var res = await http.GetStringAsync($"{xkcdUrl}/{num}/info.0.json").ConfigureAwait(false);

                    var comic = JsonConvert.DeserializeObject<XkcdComic>(res);
                    var embed = new EmbedBuilder().WithColor(WizBot.OkColor)
                                                  .WithImage(eib => eib.WithUrl(comic.ImageLink))
                                                  .WithAuthor(eab => eab.WithName(comic.Title).WithUrl($"{xkcdUrl}/{num}").WithIconUrl("http://xkcd.com/s/919f27.ico"))
                                                  .AddField(efb => efb.WithName("Comic#").WithValue(comic.Num.ToString()).WithIsInline(true))
                                                  .AddField(efb => efb.WithName("Date").WithValue($"{comic.Month}/{comic.Year}").WithIsInline(true));
                    var sent = await channel.EmbedAsync(embed.Build())
                                 .ConfigureAwait(false);

                    await Task.Delay(10000).ConfigureAwait(false);

                    await sent.ModifyAsync(m => m.Embed = embed.AddField(efb => efb.WithName("Alt").WithValue(comic.Alt.ToString()).WithIsInline(false)).Build());
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

            public override string ToString() 
                => $"`Comic:` #{Num} `Title:` {Title} `Date:` {Month}/{Year}\n{ImageLink}";
        }
    }
}
