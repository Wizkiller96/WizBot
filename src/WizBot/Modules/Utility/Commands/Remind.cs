﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using WizBot.Attributes;
using WizBot.Extensions;
using WizBot.Services;
using WizBot.Services.Database.Models;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WizBot.Modules.Utility
{
    public partial class Utility
    {
        [Group]
        public class RemindCommands
        {

            Regex regex = new Regex(@"^(?:(?<months>\d)mo)?(?:(?<weeks>\d)w)?(?:(?<days>\d{1,2})d)?(?:(?<hours>\d{1,2})h)?(?:(?<minutes>\d{1,2})m)?$",
                                    RegexOptions.Compiled | RegexOptions.Multiline);

            private string RemindMessageFormat { get; }

            IDictionary<string, Func<Reminder, string>> replacements = new Dictionary<string, Func<Reminder, string>>
            {
                { "%message%" , (r) => r.Message },
                { "%user%", (r) => $"<@!{r.UserId}>" },
                { "%target%", (r) =>  r.IsPrivate ? "Direct Message" : $"<#{r.ChannelId}>"}
            };
            private Logger _log { get; }

            public RemindCommands()
            {
                _log = LogManager.GetCurrentClassLogger();
                List<Reminder> reminders;
                using (var uow = DbHandler.UnitOfWork())
                {
                    reminders = uow.Reminders.GetAll().ToList();

                    RemindMessageFormat = uow.BotConfig.GetOrCreate().RemindMessageFormat;
                }

                foreach (var r in reminders)
                {
                    try { var t = StartReminder(r); } catch (Exception ex) { _log.Warn(ex); }
                }
            }

            private async Task StartReminder(Reminder r)
            {
                var now = DateTime.Now;
                var twoMins = new TimeSpan(0, 2, 0);
                TimeSpan time = r.When - now;

                if (time.TotalMilliseconds > int.MaxValue)
                    return;

                await Task.Delay(time);
                try
                {
                    IMessageChannel ch;
                    if (r.IsPrivate)
                    {
                        ch = await WizBot.Client.GetDMChannelAsync(r.ChannelId).ConfigureAwait(false);
                    }
                    else
                    {
                        ch = WizBot.Client.GetGuild(r.ServerId)?.GetTextChannel(r.ChannelId);
                    }
                    if (ch == null)
                        return;

                    await ch.SendMessageAsync(
                        replacements.Aggregate(RemindMessageFormat,
                            (cur, replace) => cur.Replace(replace.Key, replace.Value(r)))
                            .SanitizeMentions()
                            ).ConfigureAwait(false); //it works trust me
                }
                catch (Exception ex) { _log.Warn(ex); }
                finally
                {
                    using (var uow = DbHandler.UnitOfWork())
                    {
                        uow.Reminders.Remove(r);
                        await uow.CompleteAsync();
                    }
                }
            }

            public enum MeOrHere
            {
                Me,Here
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [Priority(1)]
            public async Task Remind(IUserMessage umsg, MeOrHere meorhere, string timeStr, [Remainder] string message)
            {
                var channel = (ITextChannel)umsg.Channel;

                IMessageChannel target;
                if (meorhere == MeOrHere.Me)
                {
                    target = await ((IGuildUser)umsg.Author).CreateDMChannelAsync().ConfigureAwait(false);
                }
                else
                {
                    target = channel;
                }
                await Remind(umsg, target, timeStr, message).ConfigureAwait(false);
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [Priority(0)]
            public async Task Remind(IUserMessage umsg, IMessageChannel ch, string timeStr, [Remainder] string message)
            {
                var channel = (ITextChannel)umsg.Channel;

                if (ch == null)
                {
                    await channel.SendErrorAsync($"{umsg.Author.Mention} Something went wrong (channel cannot be found) ;(").ConfigureAwait(false);
                    return;
                }

                var m = regex.Match(timeStr);

                if (m.Length == 0)
                {
                    await channel.SendErrorAsync("Not a valid time format. Type `-h .remind`").ConfigureAwait(false);
                    return;
                }

                string output = "";
                var namesAndValues = new Dictionary<string, int>();

                foreach (var groupName in regex.GetGroupNames())
                {
                    if (groupName == "0") continue;
                    int value = 0;
                    int.TryParse(m.Groups[groupName].Value, out value);

                    if (string.IsNullOrEmpty(m.Groups[groupName].Value))
                    {
                        namesAndValues[groupName] = 0;
                        continue;
                    }
                    else if (value < 1 ||
                        (groupName == "months" && value > 1) ||
                        (groupName == "weeks" && value > 4) ||
                        (groupName == "days" && value >= 7) ||
                        (groupName == "hours" && value > 23) ||
                        (groupName == "minutes" && value > 59))
                    {
                        await channel.SendErrorAsync($"Invalid {groupName} value.").ConfigureAwait(false);
                        return;
                    }
                    else
                        namesAndValues[groupName] = value;
                    output += m.Groups[groupName].Value + " " + groupName + " ";
                }
                var time = DateTime.Now + new TimeSpan(30 * namesAndValues["months"] +
                                                        7 * namesAndValues["weeks"] +
                                                        namesAndValues["days"],
                                                        namesAndValues["hours"],
                                                        namesAndValues["minutes"],
                                                        0);

                var rem = new Reminder
                {
                    ChannelId = ch.Id,
                    IsPrivate = ch is IDMChannel,
                    When = time,
                    Message = message,
                    UserId = umsg.Author.Id,
                    ServerId = channel.Guild.Id
                };

                using (var uow = DbHandler.UnitOfWork())
                {
                    uow.Reminders.Add(rem);
                    await uow.CompleteAsync();
                }

                try { await channel.SendConfirmAsync($"⏰ I will remind **\"{(ch is ITextChannel ? ((ITextChannel)ch).Name : umsg.Author.Username)}\"** to **\"{message.SanitizeMentions()}\"** in **{output}** `({time:d.M.yyyy.} at {time:HH:mm})`").ConfigureAwait(false); } catch { }
                await StartReminder(rem);
            }
            
            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [OwnerOnly]
            public async Task RemindTemplate(IUserMessage umsg, [Remainder] string arg)
            {
                var channel = (ITextChannel)umsg.Channel;

                if (string.IsNullOrWhiteSpace(arg))
                    return;

                using (var uow = DbHandler.UnitOfWork())
                {
                    uow.BotConfig.GetOrCreate().RemindMessageFormat = arg.Trim();
                    await uow.CompleteAsync().ConfigureAwait(false);
                }
                await channel.SendConfirmAsync("🆗 New remind template set.");
            }
        }
    }
}
