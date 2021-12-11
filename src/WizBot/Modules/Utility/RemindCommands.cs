﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using WizBot.Common.Attributes;
using WizBot.Services;
using WizBot.Services.Database.Models;
using WizBot.Db;
using WizBot.Extensions;
using WizBot.Modules.Administration.Services;
using WizBot.Modules.Utility.Services;

namespace WizBot.Modules.Utility
{
    public partial class Utility
    {
        [Group]
        public class RemindCommands : WizBotSubmodule<RemindService>
        {
            private readonly DbService _db;
            private readonly GuildTimezoneService _tz;

            public RemindCommands(DbService db, GuildTimezoneService tz)
            {
                _db = db;
                _tz = tz;
            }

            public enum MeOrHere
            {
                Me,
                Here
            }

            [WizBotCommand, Aliases]
            [Priority(1)]
            public async Task Remind(MeOrHere meorhere, [Leftover] string remindString)
            {
                if (!_service.TryParseRemindMessage(remindString, out var remindData))
                {
                    await ReplyErrorLocalizedAsync(strs.remind_invalid);
                    return;
                }
                
                ulong target;
                target = meorhere == MeOrHere.Me ? ctx.User.Id : ctx.Channel.Id;
                if (!await RemindInternal(target, meorhere == MeOrHere.Me || ctx.Guild is null, remindData.Time, remindData.What)
                    .ConfigureAwait(false))
                {
                    await ReplyErrorLocalizedAsync(strs.remind_too_long).ConfigureAwait(false);
                }
            }

            [WizBotCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageMessages)]
            [Priority(0)]
            public async Task Remind(ITextChannel channel, [Leftover] string remindString)
            {
                var perms = ((IGuildUser) ctx.User).GetPermissions(channel);
                if (!perms.SendMessages || !perms.ViewChannel)
                {
                    await ReplyErrorLocalizedAsync(strs.cant_read_or_send).ConfigureAwait(false);
                    return;
                }

                if (!_service.TryParseRemindMessage(remindString, out var remindData))
                {
                    await ReplyErrorLocalizedAsync(strs.remind_invalid);
                    return;
                }


                if (!await RemindInternal(channel.Id, false, remindData.Time, remindData.What)
                    .ConfigureAwait(false))
                {
                    await ReplyErrorLocalizedAsync(strs.remind_too_long).ConfigureAwait(false);
                }
            }
            
            public enum Server
            {
                Server = int.MinValue,
                Srvr = int.MinValue,
                Serv = int.MinValue,
                S = int.MinValue, 
            }

            [WizBotCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.Administrator)]
            [Priority(0)]
            public Task RemindList(Server _, int page = 1)
                => RemindList(page, true);

            [WizBotCommand, Aliases]
            [Priority(1)]
            public Task RemindList(int page = 1)
                => RemindList(page, false);
            
            private async Task RemindList(int page, bool isServer)
            {
                if (--page < 0)
                    return;

                var embed = _eb.Create()
                    .WithOkColor()
                    .WithTitle(GetText(isServer ? strs.reminder_server_list : strs.reminder_list));

                List<Reminder> rems;
                using (var uow = _db.GetDbContext())
                {
                    if (isServer)
                    {
                        rems = uow.Reminders
                            .RemindersForServer(ctx.Guild.Id, page)
                            .ToList();
                    }
                    else
                    {
                        rems = uow.Reminders
                            .RemindersFor(ctx.User.Id, page)
                            .ToList();
                    }
                }

                if (rems.Any())
                {
                    var i = 0;
                    foreach (var rem in rems)
                    {
                        var when = rem.When;
                        var diff = when - DateTime.UtcNow;
                        embed.AddField(
                            $"#{++i + (page * 10)} {rem.When:HH:mm yyyy-MM-dd} UTC (in {(int) diff.TotalHours}h {(int) diff.Minutes}m)",
                            $@"`Target:` {(rem.IsPrivate ? "DM" : "Channel")}
`TargetId:` {rem.ChannelId}
`Message:` {rem.Message?.TrimTo(50)}", false);
                    }
                }
                else
                {
                    embed.WithDescription(GetText(strs.reminders_none));
                }

                embed.AddPaginatedFooter(page + 1, null);
                await ctx.Channel.EmbedAsync(embed).ConfigureAwait(false);
            }

            [WizBotCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.Administrator)]
            [Priority(0)]
            public Task RemindDelete(Server _, int index)
                => RemindDelete(index, true);
            
            [WizBotCommand, Aliases]
            [Priority(1)]
            public Task RemindDelete(int index)
                => RemindDelete(index, false);
            
            private async Task RemindDelete(int index, bool isServer)
            {
                if (--index < 0)
                    return;

                Reminder rem = null;
                using (var uow = _db.GetDbContext())
                {
                    var rems = isServer
                        ? uow.Reminders
                            .RemindersForServer(ctx.Guild.Id, index / 10)
                            .ToList()
                        : uow.Reminders
                            .RemindersFor(ctx.User.Id, index / 10)
                            .ToList();
                    
                    var pageIndex = index % 10;
                    if (rems.Count > pageIndex)
                    {
                        rem = rems[pageIndex];
                        uow.Reminders.Remove(rem);
                        uow.SaveChanges();
                    }
                }

                if (rem is null)
                {
                    await ReplyErrorLocalizedAsync(strs.reminder_not_exist).ConfigureAwait(false);
                }
                else
                {
                    await ReplyConfirmLocalizedAsync(strs.reminder_deleted(index + 1));
                }
            }
            
            [WizBotCommand, Aliases]
            public async Task ServerRemindDelete(int index)
            {
                if (--index < 0)
                    return;

                Reminder rem = null;
                using (var uow = _db.GetDbContext())
                {
                    var rems = uow.Reminders
                        .RemindersForServer(ctx.Guild.Id, index / 10)
                        .ToList();
                    var pageIndex = index % 10;
                    if (rems.Count > pageIndex)
                    {
                        rem = rems[pageIndex];
                        uow.Reminders.Remove(rem);
                        uow.SaveChanges();
                    }
                }

                if (rem is null)
                {
                    await ReplyErrorLocalizedAsync(strs.reminder_not_exist).ConfigureAwait(false);
                }
                else
                {
                    await ReplyConfirmLocalizedAsync(strs.reminder_deleted(index + 1));
                }
            }

            private async Task<bool> RemindInternal(ulong targetId, bool isPrivate, TimeSpan ts, string message)
            {
                var time = DateTime.UtcNow + ts;

                if (ts > TimeSpan.FromDays(60))
                    return false;

                if (ctx.Guild != null)
                {
                    var perms = ((IGuildUser) ctx.User).GetPermissions((IGuildChannel) ctx.Channel);
                    if (!perms.MentionEveryone)
                    {
                        message = message.SanitizeAllMentions();
                    }
                }

                var rem = new Reminder
                {
                    ChannelId = targetId,
                    IsPrivate = isPrivate,
                    When = time,
                    Message = message,
                    UserId = ctx.User.Id,
                    ServerId = ctx.Guild?.Id ?? 0
                };

                using (var uow = _db.GetDbContext())
                {
                    uow.Reminders.Add(rem);
                    await uow.SaveChangesAsync();
                }

                var gTime = ctx.Guild is null
                    ? time
                    : TimeZoneInfo.ConvertTime(time, _tz.GetTimeZoneOrUtc(ctx.Guild.Id));
                try
                {
                    await SendConfirmAsync(
                        "⏰ " + GetText(strs.remind(
                            Format.Bold(!isPrivate ? $"<#{targetId}>" : ctx.User.Username),
                            Format.Bold(message),
                            $"{ts.Days}d {ts.Hours}h {ts.Minutes}min",
                            gTime, gTime))).ConfigureAwait(false);
                }
                catch
                {
                }

                return true;
            }
        }
    }
}