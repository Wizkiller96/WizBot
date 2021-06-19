#nullable enable
using Discord;
using Discord.Commands;
using NadekoBot.Common.Attributes;
using NadekoBot.Common.TypeReaders;
using NadekoBot.Extensions;
using NadekoBot.Modules.Utility.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using NadekoBot.Core.Common.TypeReaders.Models;

namespace NadekoBot.Modules.Utility
{
    public partial class Utility
    {
        [Group]
        public class RepeatCommands : NadekoSubmodule<RepeaterService>
        {
            // public override string RunningRepeaterToString() =>
            //     $"{Channel?.Mention ?? $"⚠<#{Repeater.ChannelId}>"} " +
            //     (this.Repeater.NoRedundant ? "| ✍" : "") +
            //     $"| {(int) Repeater.Interval.TotalHours}:{Repeater.Interval:mm} " +
            //     $"| {Repeater.Message.TrimTo(33)}";

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageMessages)]
            public async Task RepeatInvoke(int index)
            {
                if (--index < 0)
                    return;

                var success = await _service.TriggerExternal(ctx.Guild.Id, index);
                if (!success)
                {
                    await ReplyErrorLocalizedAsync("repeat_invoke_none").ConfigureAwait(false);
                }
            }
            
            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageMessages)]
            public async Task RepeatRemove(int index)
            {
                if (--index < 0)
                    return;

                var removed =  await _service.RemoveByIndexAsync(ctx.Guild.Id, index);
                if (removed is null)
                {
                    await ReplyErrorLocalizedAsync("repeater_remove_fail").ConfigureAwait(false);
                    return;
                }
                
                var description = GetRepeaterInfoString(removed);
                await ctx.Channel.EmbedAsync(new EmbedBuilder()
                    .WithOkColor()
                    .WithTitle(GetText("repeater_removed", index + 1))
                    .WithDescription(description));
            }
            
            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageMessages)]
            public async Task RepeatRedundant(int index)
            {
                if (--index < 0)
                    return;
                
                var result = await _service.ToggleRedundantAsync(ctx.Guild.Id, index);
            
                if (result is null)
                {
                    await ReplyErrorLocalizedAsync("index_out_of_range").ConfigureAwait(false);
                    return;
                }
            
                if (result.Value)
                {
                    await ReplyConfirmLocalizedAsync("repeater_redundant_no", index + 1);
                }
                else
                {
                    await ReplyConfirmLocalizedAsync("repeater_redundant_yes" ,index + 1);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageMessages)]
            [Priority(-1)]
            public Task Repeat([Leftover]string message)
                => Repeat(null, null, message);
            
            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageMessages)]
            [Priority(0)]
            public Task Repeat(StoopidTime interval, [Leftover]string message)
                => Repeat(null, interval, message);

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageMessages)]
            [Priority(1)]
            public Task Repeat(GuildDateTime dt, [Leftover] string message)
                => Repeat(dt, null, message);

            [NadekoCommand, Usage, Description, Aliases]
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
                    await ReplyErrorLocalizedAsync("repeater_exceed_limit", 5);
                    return;
                }
                
                var description = GetRepeaterInfoString(runner);
                await ctx.Channel.EmbedAsync(new EmbedBuilder()
                    .WithOkColor()
                    .WithTitle(GetText("repeater_created"))
                    .WithDescription(description));
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageMessages)]
            public async Task RepeatList()
            {
                var repeaters = _service.GetRepeaters(ctx.Guild.Id);
                if (repeaters.Count == 0)
                {
                    await ReplyConfirmLocalizedAsync("repeaters_none").ConfigureAwait(false);
                    return;
                }
            
                var embed = new EmbedBuilder()
                    .WithTitle(GetText("list_of_repeaters"))
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
            
                await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
            }
            
            private string GetRepeaterInfoString(RunningRepeater runner)
            {
                var intervalString = Format.Bold(runner.Repeater.Interval.ToPrettyStringHM());
                var executesIn = runner.NextTime - DateTime.UtcNow;
                var executesInString = Format.Bold(executesIn.ToPrettyStringHM());
                var message = Format.Sanitize(runner.Repeater.Message.TrimTo(50));
            
                string description = "";
                if (_service.IsNoRedundant(runner.Repeater.Id))
                {
                    description = Format.Underline(Format.Bold(GetText("no_redundant:"))) + "\n\n";
                }
                
                description += $"<#{runner.Repeater.ChannelId}>\n" +
                                  $"`{GetText("interval:")}` {intervalString}\n" +
                                  $"`{GetText("executes_in:")}` {executesInString}\n" +
                                  $"`{GetText("message:")}` {message}";
            
                return description;
            }
        }
    }
}