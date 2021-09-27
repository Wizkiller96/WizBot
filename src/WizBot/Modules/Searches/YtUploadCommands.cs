using Discord.Commands;
using WizBot.Modules.Searches.Services;
using WizBot.Modules;

namespace WizBot.Modules.Searches
{
    public partial class Searches
    {
        // [Group]
        // public class YtTrackCommands : WizBotSubmodule<YtTrackService>
        // {
        //     ;
        //     [RequireContext(ContextType.Guild)]
        //     public async Task YtFollow(string ytChannelId, [Leftover] string uploadMessage = null)
        //     {
        //         var succ = await _service.ToggleChannelFollowAsync(ctx.Guild.Id, ctx.Channel.Id, ytChannelId, uploadMessage);
        //         if(succ)
        //         {
        //             await ReplyConfirmLocalizedAsync(strs.yt_follow_added).ConfigureAwait(false);
        //         }
        //         else
        //         {
        //             await ReplyConfirmLocalizedAsync(strs.yt_follow_fail).ConfigureAwait(false);
        //         }
        //     }
        //     
        //     [WizBotCommand, Usage, Description, Aliases]
        //     [RequireContext(ContextType.Guild)]
        //     public async Task YtTrackRm(int index)
        //     {
        //         //var succ = await _service.ToggleChannelTrackingAsync(ctx.Guild.Id, ctx.Channel.Id, ytChannelId, uploadMessage);
        //         //if (succ)
        //         //{
        //         //    await ReplyConfirmLocalizedAsync(strs.yt_track_added).ConfigureAwait(false);
        //         //}
        //         //else
        //         //{
        //         //    await ReplyConfirmLocalizedAsync(strs.yt_track_fail).ConfigureAwait(false);
        //         //}
        //     }
        //     
        //     [WizBotCommand, Usage, Description, Aliases]
        //     [RequireContext(ContextType.Guild)]
        //     public async Task YtTrackList()
        //     {
        //         //var succ = await _service.ToggleChannelTrackingAsync(ctx.Guild.Id, ctx.Channel.Id, ytChannelId, uploadMessage);
        //         //if (succ)
        //         //{
        //         //    await ReplyConfirmLocalizedAsync(strs.yt_track_added).ConfigureAwait(false);
        //         //}
        //         //else
        //         //{
        //         //    await ReplyConfirmLocalizedAsync(strs.yt_track_fail).ConfigureAwait(false);
        //         //}
        //     }
        // }
    }
}
