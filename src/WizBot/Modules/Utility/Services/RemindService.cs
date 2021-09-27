﻿using System;
using System.Linq;
using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;
using WizBot.Extensions;
using WizBot.Services;
using WizBot.Services.Database.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace WizBot.Modules.Utility.Services
{
    public class RemindService : INService
    {
        private readonly Regex _regex = new Regex(@"^(?:in\s?)?\s*(?:(?<mo>\d+)(?:\s?(?:months?|mos?),?))?(?:(?:\sand\s|\s*)?(?<w>\d+)(?:\s?(?:weeks?|w),?))?(?:(?:\sand\s|\s*)?(?<d>\d+)(?:\s?(?:days?|d),?))?(?:(?:\sand\s|\s*)?(?<h>\d+)(?:\s?(?:hours?|h),?))?(?:(?:\sand\s|\s*)?(?<m>\d+)(?:\s?(?:minutes?|mins?|m),?))?\s+(?:to:?\s+)?(?<what>(?:\r\n|[\r\n]|.)+)",
                                RegexOptions.Compiled | RegexOptions.Multiline);

        private readonly DiscordSocketClient _client;
        private readonly DbService _db;
        private readonly IBotCredentials _creds;
        private readonly IEmbedBuilderService _eb;

        public RemindService(DiscordSocketClient client, DbService db, IBotCredentials creds, IEmbedBuilderService eb)
        {
            _client = client;
            _db = db;
            _creds = creds;
            _eb = eb;
            _ = StartReminderLoop();
        }

        private async Task StartReminderLoop()
        {
            while (true)
            {
                await Task.Delay(15000);
                try
                {
                    var now = DateTime.UtcNow;
                    var reminders = await GetRemindersBeforeAsync(now);
                    if (reminders.Count == 0)
                        continue;
                    
                    Log.Information($"Executing {reminders.Count} reminders.");
                    
                    // make groups of 5, with 1.5 second inbetween each one to ensure against ratelimits
                    foreach (var group in reminders.Chunk(5))
                    {
                        var executedReminders = group.ToList();
                        await Task.WhenAll(executedReminders.Select(ReminderTimerAction));
                        await RemoveReminders(executedReminders);
                        await Task.Delay(1500); 
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning($"Error in reminder loop: {ex.Message}");
                    Log.Warning(ex.ToString());
                }
            }
        }

        private async Task RemoveReminders(List<Reminder> reminders)
        {
            using (var uow = _db.GetDbContext())
            {
                uow.Set<Reminder>()
                    .RemoveRange(reminders);

                await uow.SaveChangesAsync();
            }
        }

        private Task<List<Reminder>> GetRemindersBeforeAsync(DateTime now)
        {
            using (var uow = _db.GetDbContext())
            {
                return uow.Reminders
                    .FromSqlInterpolated($"select * from reminders where ((serverid >> 22) % {_creds.TotalShards}) == {_client.ShardId} and \"when\" < {now};")
                    .ToListAsync();
            }
        }

        public struct RemindObject
        {
            public string What { get; set; }
            public TimeSpan Time { get; set; }
        }

        public bool TryParseRemindMessage(string input, out RemindObject obj)
        {
            var m = _regex.Match(input);

            obj = default;
            if (m.Length == 0)
            {
                return false;
            }
            
            var values = new Dictionary<string, int>();
            
            var what = m.Groups["what"].Value;

            if (string.IsNullOrWhiteSpace(what))
            {
                Log.Warning("No message provided for the reminder.");
                return false;
            }
            
            foreach (var groupName in _regex.GetGroupNames())
            {
                if (groupName == "0" || groupName== "what") continue;
                if (string.IsNullOrWhiteSpace(m.Groups[groupName].Value))
                {
                    values[groupName] = 0;
                    continue;
                }
                if (!int.TryParse(m.Groups[groupName].Value, out var value))
                {
                    Log.Warning($"Reminder regex group {groupName} has invalid value.");
                    return false;
                }

                if (value < 1)
                {
                    Log.Warning("Reminder time value has to be an integer greater than 0.");
                    return false;
                }
                
                values[groupName] = value;
            }
            
            var ts = new TimeSpan
            (
            30 * values["mo"] + 7 * values["w"] + values["d"],
                values["h"],
                values["m"],
                0
            );

            obj = new RemindObject()
            {
                Time = ts,
                What = what
            };

            return true;
        }

        private async Task ReminderTimerAction(Reminder r)
        {
            try
            {
                IMessageChannel ch;
                if (r.IsPrivate)
                {
                    var user = _client.GetUser(r.ChannelId);
                    if (user is null)
                        return;
                    ch = await user.GetOrCreateDMChannelAsync().ConfigureAwait(false);
                }
                else
                {
                    ch = _client.GetGuild(r.ServerId)?.GetTextChannel(r.ChannelId);
                }
                if (ch is null)
                    return;

                await ch.EmbedAsync(_eb.Create()
                    .WithOkColor()
                    .WithTitle("Reminder")
                    .AddField("Created At", r.DateAdded.HasValue ? r.DateAdded.Value.ToLongDateString() : "?")
                    .AddField("By", (await ch.GetUserAsync(r.UserId).ConfigureAwait(false))?.ToString() ?? r.UserId.ToString()),
                    msg: r.Message).ConfigureAwait(false);
            }
            catch (Exception ex) { Log.Information(ex.Message + $"({r.Id})"); }
        }
    }
}