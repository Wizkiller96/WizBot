﻿#nullable enable
using Discord;
using Discord.Commands;
using WizBot.Common.Attributes;
using WizBot.Common.TypeReaders;
using WizBot.Extensions;
using WizBot.Modules.Utility.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using WizBot.Common.TypeReaders.Models;

namespace WizBot.Modules.Utility
{
    public partial class Utility
    {
        [Group]
        public class RepeatCommands : WizBotSubmodule<RepeaterService>
        {
            [WizBotCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageMessages)]
            public async Task RepeatInvoke(int index)
            {
                if (--index < 0)
                    return;

                var success = await _service.TriggerExternal(ctx.Guild.Id, index);
                if (!success)
                {
                    await ReplyErrorLocalizedAsync(strs.repeat_invoke_none).ConfigureAwait(false);
                }
            }
            
            [WizBotCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageMessages)]
            public async Task RepeatRemove(int index)
            {
                if (--index < 0)
                    return;

                var removed =  await _service.RemoveByIndexAsync(ctx.Guild.Id, index);
                if (removed is null)
                {
                    await ReplyErrorLocalizedAsync(strs.repeater_remove_fail).ConfigureAwait(false);
                    return;
                }
                
                var description = GetRepeaterInfoString(removed);
                await ctx.Channel.EmbedAsync(_eb.Create()
                    .WithOkColor()
                    .WithTitle(GetText(strs.repeater_removed(index + 1)))
                    .WithDescription(description));
            }
            
            [WizBotCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageMessages)]
            public async Task RepeatRedundant(int index)
            {
                if (--index < 0)
                    return;
                
                var result = await _service.ToggleRedundantAsync(ctx.Guild.Id, index);
            
                if (result is null)
                {
                    await ReplyErrorLocalizedAsync(strs.index_out_of_range).ConfigureAwait(false);
                    return;
                }
            
                if (result.Value)
                {
                    await ReplyErrorLocalizedAsync(strs.repeater_redundant_no(index + 1));
                }
                else
                {
                    await ReplyConfirmLocalizedAsync(strs.repeater_redundant_yes(index + 1));
                }
            }

            [WizBotCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageMessages)]
            [Priority(-1)]
            public Task Repeat([Leftover]string message)
                => Repeat(null, null, message);
            
            [WizBotCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageMessages)]
            [Priority(0)]
            public Task Repeat(StoopidTime interval, [Leftover]string message)
                => Repeat(null, interval, message);

            [WizBotCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageMessages)]
            [Priority(1)]
            public Task Repeat(GuildDateTime dt, [Leftover] string message)
                => Repeat(dt, null, message);

            [WizBotCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageMessages)]
            [Priority(2)]
            public async Task Repeat(GuildDateTime? dt, StoopidTime? interval, [Leftover]string message)
            {
                var startTimeOfDay = dt?.InputTimeUtc.TimeOfDay;
                // if interval not null, that means user specified it (don't change it)
                
                // if interval is null set the default to:
                // if time of day is specified: 1 day
                // else 5 minutes
                var realInterval = interval?.Time ?? (startTimeOfDay is null 
                    ? TimeSpan.FromMinutes(5) 
                    : TimeSpan.FromDays(1));
                
                if (string.IsNullOrWhiteSpace(message)
                    || (interval != null &&
                        (interval.Time > TimeSpan.FromMinutes(25000) || interval.Time < TimeSpan.FromMinutes(1))))
                {
                    return;
                }

                message = ((IGuildUser) ctx.User).GuildPermissions.MentionEveryone
                    ? message
                    : message.SanitizeMentions(true);

                var runner = await _service.AddRepeaterAsync(
                    ctx.Channel.Id,
                    ctx.Guild.Id,
                    realInterval,
                    message,
                    false,
                    startTimeOfDay
                );

                if (runner is null)
                {
                    await ReplyErrorLocalizedAsync(strs.repeater_exceed_limit(5));
                    return;
                }
                
                var description = GetRepeaterInfoString(runner);
                await ctx.Channel.EmbedAsync(_eb.Create()
                    .WithOkColor()
                    .WithTitle(GetText(strs.repeater_created))
                    .WithDescription(description));
            }

            [WizBotCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageMessages)]
            public async Task RepeatList()
            {
                var repeaters = _service.GetRepeaters(ctx.Guild.Id);
                if (repeaters.Count == 0)
                {
                    await ReplyConfirmLocalizedAsync(strs.repeaters_none).ConfigureAwait(false);
                    return;
                }
            
                var embed = _eb.Create()
                    .WithTitle(GetText(strs.list_of_repeaters))
                    .WithOkColor();

                var i = 0;
                foreach(var runner in repeaters.OrderBy(r => r.Repeater.Id))
                {
                    var description = GetRepeaterInfoString(runner);
                    var name = $"#`{++i}`";
                    embed.AddField(
                        name,
                        description
                    );
                }
            
                await ctx.Channel.EmbedAsync(embed).ConfigureAwait(false);
            }
            
            private string GetRepeaterInfoString(RunningRepeater runner)
            {
                var intervalString = Format.Bold(runner.Repeater.Interval.ToPrettyStringHM());
                var executesIn = runner.NextTime < DateTime.UtcNow
                    ? TimeSpan.Zero
                    : runner.NextTime - DateTime.UtcNow;
                var executesInString = Format.Bold(executesIn.ToPrettyStringHM());
                var message = Format.Sanitize(runner.Repeater.Message.TrimTo(50));
            
                string description = "";
                if (_service.IsNoRedundant(runner.Repeater.Id))
                {
                    description = Format.Underline(Format.Bold(GetText(strs.no_redundant))) + "\n\n";
                }
                
                description += $"<#{runner.Repeater.ChannelId}>\n" +
                                  $"`{GetText(strs.interval_colon)}` {intervalString}\n" +
                                  $"`{GetText(strs.executes_in_colon)}` {executesInString}\n" +
                                  $"`{GetText(strs.message_colon)}` {message}";
            
                return description;
            }
        }
    }
}