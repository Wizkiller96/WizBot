namespace NadekoBot.Modules.Utility;

public partial class Utility
{
    [Name("Giveaways")]
    [Group("ga")]
    public partial class GiveawayCommands : NadekoModule<GiveawayService>
    {
        [Cmd]
        [UserPerm(GuildPerm.ManageMessages)]
        [BotPerm(ChannelPerm.ManageMessages | ChannelPerm.AddReactions)]
        public async Task GiveawayStart(TimeSpan duration, [Leftover] string message)
        {
            if (duration > TimeSpan.FromDays(30))
            {
                await ReplyErrorLocalizedAsync(strs.giveaway_duration_invalid);
                return;
            }

            var eb = _eb.Create(ctx)
                .WithPendingColor()
                .WithTitle(GetText(strs.giveaway_starting))
                .WithDescription(message);

            var startingMsg = await ctx.Channel.EmbedAsync(eb);

            var maybeId =
                await _service.StartGiveawayAsync(ctx.Guild.Id, ctx.Channel.Id, startingMsg.Id, duration, message);


            if (maybeId is not int id)
            {
                await startingMsg.DeleteAsync();
                await ReplyErrorLocalizedAsync(strs.giveaway_max_amount_reached);
                return;
            }

            eb
                .WithOkColor()
                .WithTitle(GetText(strs.giveaway_started))
                .WithFooter($"id:  {new kwum(id).ToString()}");

            await startingMsg.AddReactionAsync(new Emoji(GiveawayService.GiveawayEmoji));
            await startingMsg.ModifyAsync(x => x.Embed = eb.Build());
        }

        [Cmd]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task GiveawayEnd(kwum id)
        {
           var success = await _service.EndGiveawayAsync(ctx.Guild.Id, id);

           if(!success)
           {
               await ReplyErrorLocalizedAsync(strs.giveaway_not_found);
               return;
           }

           await ctx.OkAsync();
            _ = ctx.Message.DeleteAfter(5);
        }

        [Cmd]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task GiveawayReroll(kwum id)
        { 
            var success = await _service.RerollGiveawayAsync(ctx.Guild.Id, id);
            if (!success)
            {
                await ReplyErrorLocalizedAsync(strs.giveaway_not_found);
                return;
            }
            
            
            await ctx.OkAsync();
            _ = ctx.Message.DeleteAfter(5);
        }

        [Cmd]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task GiveawayCancel(kwum id)
        {
            var success = await _service.CancelGiveawayAsync(ctx.Guild.Id, id);

            if (!success)
            {
                await ReplyConfirmLocalizedAsync(strs.giveaway_not_found);
                return;
            }

            await ReplyConfirmLocalizedAsync(strs.giveaway_cancelled);
        }

        [Cmd]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task GiveawayList()
        {
            var giveaways = await _service.GetGiveawaysAsync(ctx.Guild.Id);

            if (!giveaways.Any())
            {
                await ReplyErrorLocalizedAsync(strs.no_givaways);
                return;
            }

            var eb = _eb.Create(ctx)
                .WithTitle(GetText(strs.giveaway_list))
                .WithOkColor();

            foreach (var g in giveaways)
            {
                eb.AddField($"id:  {new kwum(g.Id)}", g.Message, true);
            }

            await ctx.Channel.EmbedAsync(eb);
        }
    }
}