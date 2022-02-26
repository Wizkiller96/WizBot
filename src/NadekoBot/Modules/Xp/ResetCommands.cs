#nullable disable
using NadekoBot.Modules.Xp.Services;

namespace NadekoBot.Modules.Xp;

public partial class Xp
{
    public partial class ResetCommands : NadekoModule<XpService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public partial Task XpReset(IGuildUser user)
            => XpReset(user.Id);

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async partial Task XpReset(ulong userId)
        {
            var embed = _eb.Create().WithTitle(GetText(strs.reset)).WithDescription(GetText(strs.reset_user_confirm));

            if (!await PromptUserConfirmAsync(embed))
                return;

            _service.XpReset(ctx.Guild.Id, userId);

            await ReplyConfirmLocalizedAsync(strs.reset_user(userId));
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async partial Task XpReset()
        {
            var embed = _eb.Create().WithTitle(GetText(strs.reset)).WithDescription(GetText(strs.reset_server_confirm));

            if (!await PromptUserConfirmAsync(embed))
                return;

            _service.XpReset(ctx.Guild.Id);

            await ReplyConfirmLocalizedAsync(strs.reset_server);
        }
    }
}