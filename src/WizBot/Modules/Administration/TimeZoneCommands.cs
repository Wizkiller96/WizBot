﻿using Discord;
using Discord.Commands;
using WizBot.Extensions;
using System;
using System.Linq;
using System.Threading.Tasks;
using WizBot.Common.Attributes;
using WizBot.Modules.Administration.Services;

namespace WizBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        public class TimeZoneCommands : WizBotSubmodule<GuildTimezoneService>
        {
            [WizBotCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Timezones(int page = 1)
            {
                page--;

                if (page < 0 || page > 20)
                    return;

                var timezones = TimeZoneInfo.GetSystemTimeZones()
                    .OrderBy(x => x.BaseUtcOffset)
                    .ToArray();
                var timezonesPerPage = 20;

                var curTime = DateTimeOffset.UtcNow;

                var i = 0;
                var timezoneStrings = timezones
                    .Select(x => (x, ++i % 2 == 0))
                    .Select(data =>
                    {
                        var (tzInfo, flip) = data;
                        var nameStr = $"{tzInfo.Id,-30}";
                        var offset = curTime.ToOffset(tzInfo.GetUtcOffset(curTime)).ToString("zzz");
                        if (flip)
                        {
                            return $"{offset} {Format.Code(nameStr)}";
                        }
                        else
                        {
                            return $"{Format.Code(offset)} {nameStr}";
                        }
                    });
                
                
                
                await ctx.SendPaginatedConfirmAsync(page,
                    (curPage) => _eb.Create()
                        .WithOkColor()
                        .WithTitle(GetText(strs.timezones_available))
                        .WithDescription(string.Join("\n", timezoneStrings
                            .Skip(curPage * timezonesPerPage)
                            .Take(timezonesPerPage))),
                    timezones.Length, timezonesPerPage).ConfigureAwait(false);
            }

            [WizBotCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Timezone()
            {
                await ReplyConfirmLocalizedAsync(strs.timezone_guild(_service.GetTimeZoneOrUtc(ctx.Guild.Id))).ConfigureAwait(false);
            }

            [WizBotCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.Administrator)]
            public async Task Timezone([Leftover] string id)
            {
                TimeZoneInfo tz;
                try { tz = TimeZoneInfo.FindSystemTimeZoneById(id); } catch { tz = null; }


                if (tz is null)
                {
                    await ReplyErrorLocalizedAsync(strs.timezone_not_found).ConfigureAwait(false);
                    return;
                }
                _service.SetTimeZone(ctx.Guild.Id, tz);

                await SendConfirmAsync(tz.ToString()).ConfigureAwait(false);
            }
        }
    }
}