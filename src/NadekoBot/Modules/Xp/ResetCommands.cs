using Discord;
using Discord.Commands;
using NadekoBot.Common.Attributes;
using NadekoBot.Modules.Xp.Services;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Xp
{
    public partial class Xp
    {
        public class ResetCommands : NadekoSubmodule<XpService>
        {

            [NadekoCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.Administrator)]
            public Task XpReset(IGuildUser user)
                => XpReset(user.Id);

            [NadekoCommand, Aliases]
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

            [NadekoCommand, Aliases]
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
