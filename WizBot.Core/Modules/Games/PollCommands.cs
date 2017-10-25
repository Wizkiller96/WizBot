using Discord;
using Discord.Commands;
using Discord.WebSocket;
using WizBot.Extensions;
using System.Threading.Tasks;
using WizBot.Common.Attributes;
using WizBot.Modules.Games.Services;

namespace WizBot.Modules.Games
{
    public partial class Games
    {
        [Group]
        public class PollCommands : WizBotSubmodule<PollService>
        {
            private readonly DiscordSocketClient _client;

            public PollCommands(DiscordSocketClient client)
            {
                _client = client;
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            [RequireContext(ContextType.Guild)]
            public Task Poll([Remainder] string arg = null)
                => InternalStartPoll(arg);

            [WizBotCommand, Usage, Description, Aliases]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            [RequireContext(ContextType.Guild)]
            public async Task PollStats()
            {
                if (!_service.ActivePolls.TryGetValue(Context.Guild.Id, out var poll))
                    return;

                await Context.Channel.EmbedAsync(poll.GetStats(GetText("current_poll_results")));
            }

            private async Task InternalStartPoll(string arg)
            {
                if(await _service.StartPoll(Context.Guild.Id, Context.Message, arg) == false)
                    await ReplyErrorLocalized("poll_already_running").ConfigureAwait(false);
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            [RequireContext(ContextType.Guild)]
            public async Task Pollend()
            {
                var channel = (ITextChannel)Context.Channel;

                if(_service.ActivePolls.TryRemove(channel.Guild.Id, out var poll))
                    await poll.StopPoll().ConfigureAwait(false);
            }
        }
    }
}