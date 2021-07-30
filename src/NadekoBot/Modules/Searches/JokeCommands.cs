using Discord.Commands;
using NadekoBot.Extensions;
using NadekoBot.Modules.Searches.Services;
using System.Linq;
using System.Threading.Tasks;
using NadekoBot.Common;
using NadekoBot.Common.Attributes;

namespace NadekoBot.Modules.Searches
{
    public partial class Searches
    {
        [Group]
        public class JokeCommands : NadekoSubmodule<SearchesService>
        {

            [NadekoCommand, Aliases]
            public async Task Yomama()
            {
                await SendConfirmAsync(await _service.GetYomamaJoke().ConfigureAwait(false)).ConfigureAwait(false);
            }

            [NadekoCommand, Aliases]
            public async Task Randjoke()
            {
                var (setup, punchline) = await _service.GetRandomJoke().ConfigureAwait(false);
                await SendConfirmAsync(setup, punchline).ConfigureAwait(false);
            }

            [NadekoCommand, Aliases]
            public async Task ChuckNorris()
            {
                await SendConfirmAsync(await _service.GetChuckNorrisJoke().ConfigureAwait(false)).ConfigureAwait(false);
            }

            [NadekoCommand, Aliases]
            public async Task WowJoke()
            {
                if (!_service.WowJokes.Any())
                {
                    await ReplyErrorLocalizedAsync(strs.jokes_not_loaded).ConfigureAwait(false);
                    return;
                }
                var joke = _service.WowJokes[new NadekoRandom().Next(0, _service.WowJokes.Count)];
                await SendConfirmAsync(joke.Question, joke.Answer).ConfigureAwait(false);
            }

            [NadekoCommand, Aliases]
            public async Task MagicItem()
            {
                if (!_service.WowJokes.Any())
                {
                    await ReplyErrorLocalizedAsync(strs.magicitems_not_loaded).ConfigureAwait(false);
                    return;
                }
                var item = _service.MagicItems[new NadekoRandom().Next(0, _service.MagicItems.Count)];

                await SendConfirmAsync("✨" + item.Name, item.Description).ConfigureAwait(false);
            }
        }
    }
}
