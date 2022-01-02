#nullable disable
namespace NadekoBot.Modules.Administration;

public partial class Administration
{
    [Group]
    public partial class PrefixCommands : NadekoSubmodule
    {
        public enum Set
        {
            Set
        }

        [Cmd]
        [Priority(1)]
        public async partial Task Prefix()
            => await ReplyConfirmLocalizedAsync(strs.prefix_current(Format.Code(CmdHandler.GetPrefix(ctx.Guild))));

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        [Priority(0)]
        public partial Task Prefix(Set _, [Leftover] string prefix)
            => Prefix(prefix);

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        [Priority(0)]
        public async partial Task Prefix([Leftover] string prefix)
        {
            if (string.IsNullOrWhiteSpace(prefix))
                return;

            var oldPrefix = base.prefix;
            var newPrefix = CmdHandler.SetPrefix(ctx.Guild, prefix);

            await ReplyConfirmLocalizedAsync(strs.prefix_new(Format.Code(oldPrefix), Format.Code(newPrefix)));
        }

        [Cmd]
        [OwnerOnly]
        public async partial Task DefPrefix([Leftover] string prefix = null)
        {
            if (string.IsNullOrWhiteSpace(prefix))
            {
                await ReplyConfirmLocalizedAsync(strs.defprefix_current(CmdHandler.GetPrefix()));
                return;
            }

            var oldPrefix = CmdHandler.GetPrefix();
            var newPrefix = CmdHandler.SetDefaultPrefix(prefix);

            await ReplyConfirmLocalizedAsync(strs.defprefix_new(Format.Code(oldPrefix), Format.Code(newPrefix)));
        }
    }
}