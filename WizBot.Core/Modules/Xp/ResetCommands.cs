using Discord;
using Discord.Commands;
using WizBot.Common.Attributes;
using WizBot.Modules.Xp.Services;
using System.Threading.Tasks;

namespace WizBot.Modules.Xp
{
    public partial class Xp
    {
        public class ResetCommands : WizBotSubmodule<XpService>
        {

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.Administrator)]
            public Task XpReset(IGuildUser user)
                => XpReset(user.Id);

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.Administrator)]
            public async Task XpReset(ulong userId)
            {
                var embed = new EmbedBuilder()
                    .WithTitle(GetText("reset"))
                    .WithDescription(GetText("reset_user_confirm"));

                if (!await PromptUserConfirmAsync(embed).ConfigureAwait(false))
                    return;

                _service.XpReset(ctx.Guild.Id, userId);

                await ReplyConfirmLocalizedAsync("reset_user", userId).ConfigureAwait(false);
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.Administrator)]
            public async Task XpReset()
            {
                var embed = new EmbedBuilder()
                       .WithTitle(GetText("reset"))
                       .WithDescription(GetText("reset_server_confirm"));

                if (!await PromptUserConfirmAsync(embed).ConfigureAwait(false))
                    return;

                _service.XpReset(ctx.Guild.Id);

                await ReplyConfirmLocalizedAsync("reset_server").ConfigureAwait(false);
            }
        }
    }
}
