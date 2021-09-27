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

            [WizBotCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.Administrator)]
            public Task XpReset(IGuildUser user)
                => XpReset(user.Id);

            [WizBotCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.Administrator)]
            public async Task XpReset(ulong userId)
            {
                var embed = _eb.Create()
                    .WithTitle(GetText(strs.reset))
                    .WithDescription(GetText(strs.reset_user_confirm));

                if (!await PromptUserConfirmAsync(embed).ConfigureAwait(false))
                    return;

                _service.XpReset(ctx.Guild.Id, userId);

                await ReplyConfirmLocalizedAsync(strs.reset_user(userId));
            }

            [WizBotCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.Administrator)]
            public async Task XpReset()
            {
                var embed = _eb.Create()
                       .WithTitle(GetText(strs.reset))
                       .WithDescription(GetText(strs.reset_server_confirm));

                if (!await PromptUserConfirmAsync(embed).ConfigureAwait(false))
                    return;

                _service.XpReset(ctx.Guild.Id);

                await ReplyConfirmLocalizedAsync(strs.reset_server).ConfigureAwait(false);
            }
        }
    }
}
