﻿using System.Threading.Tasks;
using Discord.Commands;
using System;
using WizBot.Core.Services;
using WizBot.Extensions;
using Discord;
using WizBot.Common.Attributes;
using WizBot.Modules.Utility.Services;

namespace WizBot.Modules.Utility
{
    public partial class Utility
    {
        [Group]
        public class PatreonCommands : WizBotSubmodule<PatreonRewardsService>
        {
            private readonly IBotCredentials _creds;

            public PatreonCommands(IBotCredentials creds)
            {
                _creds = creds;
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.DM)]
            [OwnerOnly]
            public async Task PatreonRewardsReload()
            {
                if (string.IsNullOrWhiteSpace(_creds.PatreonAccessToken))
                    return;
                await _service.RefreshPledges().ConfigureAwait(false);

                await ctx.Channel.SendConfirmAsync("👌").ConfigureAwait(false);
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.DM)]
            public async Task ClaimPatreonRewards()
            {
                if (string.IsNullOrWhiteSpace(_creds.PatreonAccessToken))
                    return;

                if (DateTime.UtcNow.Day < 5)
                {
                    await ReplyErrorLocalizedAsync("clpa_too_early").ConfigureAwait(false);
                    return;
                }

                var rem = (_service.Interval - (DateTime.UtcNow - _service.LastUpdate));
                var helpcmd = Format.Code(Prefix + "donate");
                await ctx.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                    .WithDescription(GetText("clpa_obsolete"))
                    .AddField(efb => efb.WithName(GetText("clpa_fail_already_title")).WithValue(GetText("clpa_fail_already")))
                    .AddField(efb => efb.WithName(GetText("clpa_fail_wait_title")).WithValue(GetText("clpa_fail_wait")))
                    .AddField(efb => efb.WithName(GetText("clpa_fail_conn_title")).WithValue(GetText("clpa_fail_conn")))
                    .AddField(efb => efb.WithName(GetText("clpa_fail_sup_title")).WithValue(GetText("clpa_fail_sup", helpcmd)))
                    .WithFooter(efb => efb.WithText(GetText("clpa_next_update", rem))))
                    .ConfigureAwait(false);
            }
        }
    }
}
