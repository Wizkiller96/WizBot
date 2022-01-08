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
        public partial Task Prefix(Set _, [Leftover] string newPrefix)
            => Prefix(newPrefix);

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        [Priority(0)]
        public async partial Task Prefix([Leftover] string toSet)
        {
            if (string.IsNullOrWhiteSpace(prefix))
                return;

            var oldPrefix = prefix;
            var newPrefix = CmdHandler.SetPrefix(ctx.Guild, toSet);

            await ReplyConfirmLocalizedAsync(strs.prefix_new(Format.Code(oldPrefix), Format.Code(newPrefix)));
        }

        [Cmd]
        [OwnerOnly]
        public async partial Task DefPrefix([Leftover] string toSet = null)
        {
            if (string.IsNullOrWhiteSpace(toSet))
            {
                await ReplyConfirmLocalizedAsync(strs.defprefix_current(CmdHandler.GetPrefix()));
                return;
            }

            var oldPrefix = CmdHandler.GetPrefix();
            var newPrefix = CmdHandler.SetDefaultPrefix(toSet);

            await ReplyConfirmLocalizedAsync(strs.defprefix_new(Format.Code(oldPrefix), Format.Code(newPrefix)));
        }
    }
}