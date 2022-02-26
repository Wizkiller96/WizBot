#nullable disable
using NadekoBot.Modules.Administration.Services;

namespace NadekoBot.Modules.Administration;

public partial class Administration
{
    [Group]
    public partial class PruneCommands : NadekoModule<PruneService>
    {
        private static readonly TimeSpan _twoWeeks = TimeSpan.FromDays(14);

        //delets her own messages, no perm required
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async partial Task Prune(string parameter = null)
        {
            var user = await ctx.Guild.GetCurrentUserAsync();

            if (parameter is "-s" or "--safe")
                await _service.PruneWhere((ITextChannel)ctx.Channel, 100, x => x.Author.Id == user.Id && !x.IsPinned);
            else
                await _service.PruneWhere((ITextChannel)ctx.Channel, 100, x => x.Author.Id == user.Id);
            ctx.Message.DeleteAfter(3);
        }

        // prune x
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(ChannelPerm.ManageMessages)]
        [BotPerm(ChannelPerm.ManageMessages)]
        [Priority(1)]
        public async partial Task Prune(int count, string parameter = null)
        {
            count++;
            if (count < 1)
                return;
            if (count > 1000)
                count = 1000;

            if (parameter is "-s" or "--safe")
                await _service.PruneWhere((ITextChannel)ctx.Channel, count, x => !x.IsPinned);
            else
                await _service.PruneWhere((ITextChannel)ctx.Channel, count, _ => true);
        }

        //prune @user [x]
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(ChannelPerm.ManageMessages)]
        [BotPerm(ChannelPerm.ManageMessages)]
        [Priority(0)]
        public partial Task Prune(IGuildUser user, int count = 100, string parameter = null)
            => Prune(user.Id, count, parameter);

        //prune userid [x]
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(ChannelPerm.ManageMessages)]
        [BotPerm(ChannelPerm.ManageMessages)]
        [Priority(0)]
        public async partial Task Prune(ulong userId, int count = 100, string parameter = null)
        {
            if (userId == ctx.User.Id)
                count++;

            if (count < 1)
                return;

            if (count > 1000)
                count = 1000;

            if (parameter is "-s" or "--safe")
            {
                await _service.PruneWhere((ITextChannel)ctx.Channel,
                    count,
                    m => m.Author.Id == userId && DateTime.UtcNow - m.CreatedAt < _twoWeeks && !m.IsPinned);
            }
            else
            {
                await _service.PruneWhere((ITextChannel)ctx.Channel,
                    count,
                    m => m.Author.Id == userId && DateTime.UtcNow - m.CreatedAt < _twoWeeks);
            }
        }
    }
}