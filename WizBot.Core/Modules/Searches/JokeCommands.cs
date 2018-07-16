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

            [WizBotCommand, Usage, Description, Aliases]
            public async Task Yomama()
            {
                await Context.Channel.SendConfirmAsync(await _service.GetYomamaJoke().ConfigureAwait(false)).ConfigureAwait(false);
            }

            [WizBotCommand, Usage, Description, Aliases]
            public async Task Randjoke()
            {
                var (Text, BaseUri) = await SearchesService.GetRandomJoke().ConfigureAwait(false);
                await Context.Channel.SendConfirmAsync("", Text, footer: BaseUri).ConfigureAwait(false);
            }

            [WizBotCommand, Usage, Description, Aliases]
            public async Task ChuckNorris()
            {
                await Context.Channel.SendConfirmAsync(await _service.GetChuckNorrisJoke().ConfigureAwait(false)).ConfigureAwait(false);
            }

            [WizBotCommand, Usage, Description, Aliases]
            public async Task WowJoke()
            {
                if (!_service.WowJokes.Any())
                {
                    await ReplyErrorLocalized("jokes_not_loaded").ConfigureAwait(false);
                    return;
                }
                var joke = _service.WowJokes[new WizBotRandom().Next(0, _service.WowJokes.Count)];
                await Context.Channel.SendConfirmAsync(joke.Question, joke.Answer).ConfigureAwait(false);
            }

            [WizBotCommand, Usage, Description, Aliases]
            public async Task MagicItem()
            {
                if (!_service.WowJokes.Any())
                {
                    await ReplyErrorLocalized("magicitems_not_loaded").ConfigureAwait(false);
                    return;
                }
                var item = _service.MagicItems[new WizBotRandom().Next(0, _service.MagicItems.Count)];

                await Context.Channel.SendConfirmAsync("✨" + item.Name, item.Description).ConfigureAwait(false);
            }
        }
    }
}