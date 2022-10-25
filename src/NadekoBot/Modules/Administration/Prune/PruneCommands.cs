#nullable disable
using CommandLine;
using NadekoBot.Modules.Administration.Services;

namespace NadekoBot.Modules.Administration;

public partial class Administration
{
    [Group]
    public partial class PruneCommands : NadekoModule<PruneService>
    {
        private static readonly TimeSpan _twoWeeks = TimeSpan.FromDays(14);

        public sealed class PruneOptions : INadekoCommandOptions
        {
            [Option(shortName: 's', longName: "safe", Default = false, HelpText = "Whether pinned messages should be deleted.", Required = false)]
            public bool Safe { get; set; }
            
            [Option(shortName: 'a', longName: "after", Default = null, HelpText = "Prune only messages after the specified message ID.", Required = false)]
            public ulong? After { get; set; }

            public void NormalizeOptions()
            {
            }
        }
        
        //deletes her own messages, no perm required
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [NadekoOptions(typeof(PruneOptions))]
        public async Task Prune(params string[] args)
        {
            var (opts, _) = OptionsParser.ParseFrom<PruneOptions>(new PruneOptions(), args);
            
            var user = await ctx.Guild.GetCurrentUserAsync();

            if (opts.Safe)
                await _service.PruneWhere((ITextChannel)ctx.Channel, 100, x => x.Author.Id == user.Id && !x.IsPinned, opts.After);
            else
                await _service.PruneWhere((ITextChannel)ctx.Channel, 100, x => x.Author.Id == user.Id, opts.After);
            
            ctx.Message.DeleteAfter(3);
        }

        // prune x
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(ChannelPerm.ManageMessages)]
        [BotPerm(ChannelPerm.ManageMessages)]
        [NadekoOptions(typeof(PruneOptions))]
        [Priority(1)]
        public async Task Prune(int count, params string[] args)
        {
            count++;
            if (count < 1)
                return;
            if (count > 1000)
                count = 1000;
            
            var (opts, _) = OptionsParser.ParseFrom<PruneOptions>(new PruneOptions(), args);

            if (opts.Safe)
                await _service.PruneWhere((ITextChannel)ctx.Channel, count, x => !x.IsPinned, opts.After);
            else
                await _service.PruneWhere((ITextChannel)ctx.Channel, count, _ => true, opts.After);
        }

        //prune @user [x]
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(ChannelPerm.ManageMessages)]
        [BotPerm(ChannelPerm.ManageMessages)]
        [NadekoOptions(typeof(PruneOptions))]
        [Priority(0)]
        public Task Prune(IGuildUser user, int count = 100, params string[] args)
            => Prune(user.Id, count, args);

        //prune userid [x]
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(ChannelPerm.ManageMessages)]
        [BotPerm(ChannelPerm.ManageMessages)]
        [NadekoOptions(typeof(PruneOptions))]
        [Priority(0)]
        public async Task Prune(ulong userId, int count = 100, params string[] args)
        {
            if (userId == ctx.User.Id)
                count++;

            if (count < 1)
                return;

            if (count > 1000)
                count = 1000;

            var (opts, _) = OptionsParser.ParseFrom<PruneOptions>(new PruneOptions(), args);
            
            if (opts.Safe)
            {
                await _service.PruneWhere((ITextChannel)ctx.Channel,
                    count,
                    m => m.Author.Id == userId && DateTime.UtcNow - m.CreatedAt < _twoWeeks && !m.IsPinned,
                    opts.After);
            }
            else
            {
                await _service.PruneWhere((ITextChannel)ctx.Channel,
                    count,
                    m => m.Author.Id == userId && DateTime.UtcNow - m.CreatedAt < _twoWeeks,
                    opts.After);
            }
        }
    }
}