using Discord;
using Discord.Commands;
using WizBot.Attributes;
using WizBot.Extensions;
using WizBot.Services.Administration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WizBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        public class PruneCommands : ModuleBase
        {
            private readonly TimeSpan twoWeeks = TimeSpan.FromDays(14);
            private readonly PruneService _prune;

            public PruneCommands(PruneService prune)
            {
                _prune = prune;
            }

            //delets her own messages, no perm required
            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Prune()
            {
                var user = await Context.Guild.GetCurrentUserAsync().ConfigureAwait(false);

                await _prune.PruneWhere((ITextChannel)Context.Channel, 100, (x) => x.Author.Id == user.Id).ConfigureAwait(false);
                Context.Message.DeleteAfter(3);
            }
            // prune x
            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(ChannelPermission.ManageMessages)]
            [RequireBotPermission(GuildPermission.ManageMessages)]
            [Priority(0)]
            public async Task Prune(int count)
            {
                if (count < 1)
                    return;
                if (count > 1000)
                    count = 1000;
                await Context.Message.DeleteAsync().ConfigureAwait(false);
                await _prune.PruneWhere((ITextChannel)Context.Channel, count, x => true).ConfigureAwait(false);
            }

            //prune @user [x]
            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(ChannelPermission.ManageMessages)]
            [RequireBotPermission(GuildPermission.ManageMessages)]
            [Priority(1)]
            public async Task Prune(IGuildUser user, int count = 100)
            {
                if (count < 1)
                    return;

                if (count > 1000)
                    count = 1000;
                await _prune.PruneWhere((ITextChannel)Context.Channel, count, m => m.Author.Id == user.Id && DateTime.UtcNow - m.CreatedAt < twoWeeks);
            }
        }
    }
}