#nullable disable
using NadekoBot.Modules.Searches.Services;

namespace NadekoBot.Modules.Searches;

public partial class Searches
{
    [Group]
    public partial class JokeCommands : NadekoModule<SearchesService>
    {
        [Cmd]
        public async partial Task Yomama()
            => await SendConfirmAsync(await _service.GetYomamaJoke());

        [Cmd]
        public async partial Task Randjoke()
        {
            var (setup, punchline) = await _service.GetRandomJoke();
            await SendConfirmAsync(setup, punchline);
        }

        [Cmd]
        public async partial Task ChuckNorris()
            => await SendConfirmAsync(await _service.GetChuckNorrisJoke());

        [Cmd]
        public async partial Task WowJoke()
        {
            if (!_service.WowJokes.Any())
            {
                await ReplyErrorLocalizedAsync(strs.jokes_not_loaded);
                return;
            }

            var joke = _service.WowJokes[new NadekoRandom().Next(0, _service.WowJokes.Count)];
            await SendConfirmAsync(joke.Question, joke.Answer);
        }

        [Cmd]
        public async partial Task MagicItem()
        {
            if (!_service.WowJokes.Any())
            {
                await ReplyErrorLocalizedAsync(strs.magicitems_not_loaded);
                return;
            }

            var item = _service.MagicItems[new NadekoRandom().Next(0, _service.MagicItems.Count)];

            await SendConfirmAsync("✨" + item.Name, item.Description);
        }
    }
}