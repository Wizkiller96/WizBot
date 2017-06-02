using Discord.Commands;
using WizBot.Attributes;
using WizBot.Extensions;
using WizBot.Services;
using WizBot.Services.Searches;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace WizBot.Modules.Searches
{
    public partial class Searches
    {
        [Group]
        public class JokeCommands : WizBotSubModule
        {
            private readonly SearchesService _searches;

            public JokeCommands(SearchesService searches)
            {
                _searches = searches;
            }

            [WizBotCommand, Usage, Description, Aliases]
            public async Task Yomama()
            {
                using (var http = new HttpClient())
                {
                    var response = await http.GetStringAsync("http://api.yomomma.info/").ConfigureAwait(false);
                    await Context.Channel.SendConfirmAsync(JObject.Parse(response)["joke"].ToString() + " 😆").ConfigureAwait(false);
                }
            }

            [WizBotCommand, Usage, Description, Aliases]
            public async Task Randjoke()
            {
                using (var http = new HttpClient())
                {
                    var response = await http.GetStringAsync("http://tambal.azurewebsites.net/joke/random").ConfigureAwait(false);
                    await Context.Channel.SendConfirmAsync(JObject.Parse(response)["joke"].ToString() + " 😆").ConfigureAwait(false);
                }
            }

            [WizBotCommand, Usage, Description, Aliases]
            public async Task ChuckNorris()
            {
                using (var http = new HttpClient())
                {
                    var response = await http.GetStringAsync("http://api.icndb.com/jokes/random/").ConfigureAwait(false);
                    await Context.Channel.SendConfirmAsync(JObject.Parse(response)["value"]["joke"].ToString() + " 😆").ConfigureAwait(false);
                }
            }

            [WizBotCommand, Usage, Description, Aliases]
            public async Task WowJoke()
            {
                if (!_searches.WowJokes.Any())
                {
                    await ReplyErrorLocalized("jokes_not_loaded").ConfigureAwait(false);
                    return;
                }
                var joke = _searches.WowJokes[new WizBotRandom().Next(0, _searches.WowJokes.Count)];
                await Context.Channel.SendConfirmAsync(joke.Question, joke.Answer).ConfigureAwait(false);
            }

            [WizBotCommand, Usage, Description, Aliases]
            public async Task MagicItem()
            {
                if (!_searches.WowJokes.Any())
                {
                    await ReplyErrorLocalized("magicitems_not_loaded").ConfigureAwait(false);
                    return;
                }
                var item = _searches.MagicItems[new WizBotRandom().Next(0, _searches.MagicItems.Count)];

                await Context.Channel.SendConfirmAsync("✨" + item.Name, item.Description).ConfigureAwait(false);
            }
        }
    }
}
