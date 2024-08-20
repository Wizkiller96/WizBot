﻿#nullable disable
using System.Globalization;
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using WizBot.Common.ModuleBehaviors;
using WizBot.Db.Models;
using System.Text.RegularExpressions;

namespace WizBot.Modules.Utility.Services;

public class RemindService : INService, IReadyExecutor, IRemindService
{
    private readonly Regex _regex =
        new(
            @"^(?:(?:at|on(?:\sthe)?)?\s*(?<date>(?:\d{2}:\d{2}\s)?\d{1,2}\.\d{1,2}(?:\.\d{2,4})?)|(?:in\s?)?\s*(?:(?<mo>\d+)(?:\s?(?:months?|mos?),?))?(?:(?:\sand\s|\s*)?(?<w>\d+)(?:\s?(?:weeks?|w),?))?(?:(?:\sand\s|\s*)?(?<d>\d+)(?:\s?(?:days?|d),?))?(?:(?:\sand\s|\s*)?(?<h>\d+)(?:\s?(?:hours?|h),?))?(?:(?:\sand\s|\s*)?(?<m>\d+)(?:\s?(?:minutes?|mins?|m),?))?)\s+(?:to:?\s+)?(?<what>(?:\r\n|[\r\n]|.)+)",
            RegexOptions.Compiled | RegexOptions.Multiline);

    private readonly DiscordSocketClient _client;
    private readonly DbService _db;
    private readonly IBotCredentials _creds;
    private readonly IMessageSenderService _sender;
    private readonly CultureInfo _culture;

    public RemindService(
        DiscordSocketClient client,
        DbService db,
        IBotCredentials creds,
        IMessageSenderService sender)
    {
        _client = client;
        _db = db;
        _creds = creds;
        _sender = sender;

        try
        {
            _culture = new CultureInfo("en-GB");
        }
        catch
        {
            _culture = CultureInfo.InvariantCulture;
        }
    }

    public async Task OnReadyAsync()
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));
        while (await timer.WaitForNextTickAsync())
        {
            await OnReminderLoopTickInternalAsync();
        }
    }

    private async Task OnReminderLoopTickInternalAsync()
    {
        try
        {
            var now = DateTime.UtcNow;
            var reminders = await GetRemindersBeforeAsync(now);
            if (reminders.Count == 0)
                return;

            Log.Information("Executing {ReminderCount} reminders", reminders.Count);

            // make groups of 5, with 1.5 second in between each one to ensure against ratelimits
            foreach (var group in reminders.Chunk(5))
            {
                var executedReminders = group.ToList();
                await executedReminders.Select(ReminderTimerAction).WhenAll();
                await RemoveReminders(executedReminders.Select(x => x.Id));
                await Task.Delay(1500);
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error in reminder loop: {ErrorMessage}", ex.Message);
        }
    }

    private async Task RemoveReminders(IEnumerable<int> reminders)
    {
        await using var uow = _db.GetDbContext();
        await uow.Set<Reminder>()
                 .ToLinqToDBTable()
                 .DeleteAsync(x => reminders.Contains(x.Id));

        await uow.SaveChangesAsync();
    }

    private async Task<List<Reminder>> GetRemindersBeforeAsync(DateTime now)
    {
        await using var uow = _db.GetDbContext();
        return await uow.Set<Reminder>()
                        .ToLinqToDBTable()
                        .Where(x => Linq2DbExpressions.GuildOnShard(x.ServerId, _creds.TotalShards, _client.ShardId)
                                    && x.When < now)
                        .ToListAsyncLinqToDB();
    }

    public bool TryParseRemindMessage(string input, out RemindObject obj)
    {
        var m = _regex.Match(input);

        obj = default;
        if (m.Length == 0)
            return false;

        var values = new Dictionary<string, int>();

        var what = m.Groups["what"].Value;

        if (string.IsNullOrWhiteSpace(what))
        {
            Log.Warning("No message provided for the reminder");
            return false;
        }

        TimeSpan ts;

        var dateString = m.Groups["date"].Value;
        if (!string.IsNullOrWhiteSpace(dateString))
        {
            var now = DateTime.UtcNow;

            if (!DateTime.TryParse(dateString, _culture, DateTimeStyles.None, out var dt))
            {
                Log.Warning("Invalid remind datetime format");
                return false;
            }

            if (now >= dt)
            {
                Log.Warning("That remind time has already passed");
                return false;
            }

            ts = dt - now;
        }
        else
        {
            foreach (var groupName in _regex.GetGroupNames())
            {
                if (groupName is "0" or "what")
                    continue;

                if (string.IsNullOrWhiteSpace(m.Groups[groupName].Value))
                {
                    values[groupName] = 0;
                    continue;
                }

                if (!int.TryParse(m.Groups[groupName].Value, out var value))
                {
                    Log.Warning("Reminder regex group {GroupName} has invalid value", groupName);
                    return false;
                }

                if (value < 1)
                {
                    Log.Warning("Reminder time value has to be an integer greater than 0");
                    return false;
                }

                values[groupName] = value;
            }

            ts = new TimeSpan((30 * values["mo"]) + (7 * values["w"]) + values["d"], values["h"], values["m"], 0);
        }


        obj = new()
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
                ch = await user.CreateDMChannelAsync();
            }
            else
                ch = _client.GetGuild(r.ServerId)?.GetTextChannel(r.ChannelId);

            if (ch is null)
                return;

            var st = SmartText.CreateFrom(r.Message);

            if (st is SmartEmbedText set)
            {
                await _sender.Response(ch).Embed(set.GetEmbed()).SendAsync();
            }
            else if (st is SmartEmbedTextArray seta)
            {
                await _sender.Response(ch).Embeds(seta.GetEmbedBuilders()).SendAsync();
            }
            else
            {
                await _sender.Response(ch)
                             .Embed(_sender.CreateEmbed()
                                           .WithOkColor()
                                           .WithTitle("Reminder")
                                           .AddField("Created At",
                                               r.DateAdded.HasValue ? r.DateAdded.Value.ToLongDateString() : "?")
                                           .AddField("By",
                                               (await ch.GetUserAsync(r.UserId))?.ToString() ?? r.UserId.ToString()))
                             .Text(r.Message)
                             .SendAsync();
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error executing reminder {ReminderId}: {ErrorMessage}", r.Id, ex.Message);
        }
    }

    public struct RemindObject
    {
        public string What { get; set; }
        public TimeSpan Time { get; set; }
    }

    public async Task AddReminderAsync(
        ulong userId,
        ulong targetId,
        ulong? guildId,
        bool isPrivate,
        DateTime time,
        string message,
        ReminderType reminderType)
    {
        var rem = new Reminder
        {
            UserId = userId,
            ChannelId = targetId,
            ServerId = guildId ?? 0,
            IsPrivate = isPrivate,
            When = time,
            Message = message,
            Type = reminderType
        };

        await using var ctx = _db.GetDbContext();
        await ctx.Set<Reminder>()
                 .AddAsync(rem);
        await ctx.SaveChangesAsync();
    }

    public async Task<List<Reminder>> GetServerReminders(int page, ulong guildId)
    {
        await using var uow = _db.GetDbContext();
        return uow.Set<Reminder>().RemindersForServer(guildId, page).ToList();
    }

    public async Task<List<Reminder>> GetUserReminders(int page, ulong userId)
    {
        await using var uow = _db.GetDbContext();
        return uow.Set<Reminder>().RemindersFor(userId, page).ToList();
    }
}