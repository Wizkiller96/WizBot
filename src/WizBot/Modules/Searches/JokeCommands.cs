using Discord.Commands;
using WizBot.Extensions;
using WizBot.Modules.Searches.Services;
using System.Linq;
using System.Threading.Tasks;
using WizBot.Common;
using WizBot.Common.Attributes;

namespace WizBot.Modules.Searches
{
    public partial class Searches
    {
        [Group]
        public class JokeCommands : WizBotSubmodule<SearchesService>
        {

            [WizBotCommand, Aliases]
            public async Task Yomama()
            {
                await SendConfirmAsync(await _service.GetYomamaJoke().ConfigureAwait(false)).ConfigureAwait(false);
            }

            [WizBotCommand, Aliases]
            public async Task Randjoke()
            {
                var (setup, punchline) = await _service.GetRandomJoke().ConfigureAwait(false);
                await SendConfirmAsync(setup, punchline).ConfigureAwait(false);
            }

            [WizBotCommand, Aliases]
            public async Task ChuckNorris()
            {
                await SendConfirmAsync(await _service.GetChuckNorrisJoke().ConfigureAwait(false)).ConfigureAwait(false);
            }

            [WizBotCommand, Aliases]
            public async Task WowJoke()
            {
                if (!_service.WowJokes.Any())
                {
                    await ReplyErrorLocalizedAsync(strs.jokes_not_loaded).ConfigureAwait(false);
                    return;
                }
                var joke = _service.WowJokes[new WizBotRandom().Next(0, _service.WowJokes.Count)];
                await SendConfirmAsync(joke.Question, joke.Answer).ConfigureAwait(false);
            }

            [WizBotCommand, Aliases]
            public async Task MagicItem()
            {
                if (!_service.WowJokes.Any())
                {
                    await ReplyErrorLocalizedAsync(strs.magicitems_not_loaded).ConfigureAwait(false);
                    return;
                }
                var item = _service.MagicItems[new WizBotRandom().Next(0, _service.MagicItems.Count)];

                await SendConfirmAsync("✨" + item.Name, item.Description).ConfigureAwait(false);
            }
        }
    }
}
