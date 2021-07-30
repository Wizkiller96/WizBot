using System.Threading.Tasks;
using Discord.Commands;
using System;
using NadekoBot.Services;
using NadekoBot.Extensions;
using Discord;
using NadekoBot.Common.Attributes;
using NadekoBot.Modules.Utility.Services;

namespace NadekoBot.Modules.Utility
{
    public partial class Utility
    {
        [Group]
        public class PatreonCommands : NadekoSubmodule<PatreonRewardsService>
        {
            private readonly IBotCredentials _creds;

            public PatreonCommands(IBotCredentials creds)
            {
                _creds = creds;
            }

            [NadekoCommand, Aliases]
            [RequireContext(ContextType.DM)]
            public async Task ClaimPatreonRewards()
            {
                if (string.IsNullOrWhiteSpace(_creds.PatreonAccessToken))
                    return;

                if (DateTime.UtcNow.Day < 5)
                {
                    await ReplyErrorLocalizedAsync(strs.clpa_too_early).ConfigureAwait(false);
                    return;
                }
                
                var rem = (_service.Interval - (DateTime.UtcNow - _service.LastUpdate));
                var helpcmd = Format.Code(Prefix + "donate");
                await ctx.Channel.EmbedAsync(_eb.Create().WithOkColor()
                    .WithDescription(GetText(strs.clpa_obsolete))
                    .AddField(GetText(strs.clpa_fail_already_title), GetText(strs.clpa_fail_already))
                    .AddField(GetText(strs.clpa_fail_wait_title), GetText(strs.clpa_fail_wait))
                    .AddField(GetText(strs.clpa_fail_conn_title), GetText(strs.clpa_fail_conn))
                    .AddField(GetText(strs.clpa_fail_sup_title), GetText(strs.clpa_fail_sup(helpcmd)))
                    .WithFooter(GetText(strs.clpa_next_update(rem))));
            }
        }
    }
}