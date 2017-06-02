using Discord;
using Discord.Commands;
using WizBot.Attributes;
using WizBot.Extensions;
using WizBot.Services;
using WizBot.Services.Utility;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace WizBot.Modules.Utility
{
    public partial class Utility
    {
        [Group]
        public class CrossServerTextChannel : WizBotSubmodule
        {
            private readonly UtilityService _service;

            public CrossServerTextChannel(UtilityService service)
            {
                _service = service;
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [OwnerOnly]
            public async Task Scsc()
            {
                var token = new WizBotRandom().Next();
                var set = new ConcurrentHashSet<ITextChannel>();
                if (_service.Subscribers.TryAdd(token, set))
                {
                    set.Add((ITextChannel)Context.Channel);
                    await ((IGuildUser)Context.User).SendConfirmAsync(GetText("csc_token"), token.ToString())
                        .ConfigureAwait(false);
                }
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageGuild)]
            public async Task Jcsc(int token)
            {
                ConcurrentHashSet<ITextChannel> set;
                if (!_service.Subscribers.TryGetValue(token, out set))
                    return;
                set.Add((ITextChannel)Context.Channel);
                await ReplyConfirmLocalized("csc_join").ConfigureAwait(false);
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageGuild)]
            public async Task Lcsc()
            {
                foreach (var subscriber in _service.Subscribers)
                {
                    subscriber.Value.TryRemove((ITextChannel)Context.Channel);
                }
                await ReplyConfirmLocalized("csc_leave").ConfigureAwait(false);
            }
        }
    }
}