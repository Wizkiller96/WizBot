#nullable disable
namespace NadekoBot.Modules.Administration;

public partial class Administration
{
    [Group]
    public partial class PrefixCommands : NadekoModule
    {
        public enum Set
        {
            Set
        }

        [Cmd]
        [Priority(1)]
        public async partial Task Prefix()
            => await ReplyConfirmLocalizedAsync(strs.prefix_current(Format.Code(_cmdHandler.GetPrefix(ctx.Guild))));

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
            var newPrefix = _cmdHandler.SetPrefix(ctx.Guild, toSet);

            await ReplyConfirmLocalizedAsync(strs.prefix_new(Format.Code(oldPrefix), Format.Code(newPrefix)));
        }

        [Cmd]
        [OwnerOnly]
        public async partial Task DefPrefix([Leftover] string toSet = null)
        {
            if (string.IsNullOrWhiteSpace(toSet))
            {
                await ReplyConfirmLocalizedAsync(strs.defprefix_current(_cmdHandler.GetPrefix()));
                return;
            }

            var oldPrefix = _cmdHandler.GetPrefix();
            var newPrefix = _cmdHandler.SetDefaultPrefix(toSet);

            await ReplyConfirmLocalizedAsync(strs.defprefix_new(Format.Code(oldPrefix), Format.Code(newPrefix)));
        }
    }
}