#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using NadekoBot.Common.ModuleBehaviors;
using NadekoBot.Services.Database.Models;
using System.Text.RegularExpressions;

namespace NadekoBot.Modules.Utility.Services;

public class RemindService : INService, IReadyExecutor
{
    private readonly Regex _regex =
        new(
            @"^(?:in\s?)?\s*(?:(?<mo>\d+)(?:\s?(?:months?|mos?),?))?(?:(?:\sand\s|\s*)?(?<w>\d+)(?:\s?(?:weeks?|w),?))?(?:(?:\sand\s|\s*)?(?<d>\d+)(?:\s?(?:days?|d),?))?(?:(?:\sand\s|\s*)?(?<h>\d+)(?:\s?(?:hours?|h),?))?(?:(?:\sand\s|\s*)?(?<m>\d+)(?:\s?(?:minutes?|mins?|m),?))?\s+(?:to:?\s+)?(?<what>(?:\r\n|[\r\n]|.)+)",
            RegexOptions.Compiled | RegexOptions.Multiline);

    private readonly DiscordSocketClient _client;
    private readonly DbService _db;
    private readonly IBotCredentials _creds;
    private readonly IEmbedBuilderService _eb;

    public RemindService(
        DiscordSocketClient client,
        DbService db,
        IBotCredentials creds,
        IEmbedBuilderService eb)
    {
        _client = client;
        _db = db;
        _creds = creds;
        _eb = eb;
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

            // make groups of 5, with 1.5 second inbetween each one to ensure against ratelimits
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
        await uow.Reminders
                 .ToLinqToDBTable()
                 .DeleteAsync(x => reminders.Contains(x.Id));

        await uow.SaveChangesAsync();
    }

    private async Task<List<Reminder>> GetRemindersBeforeAsync(DateTime now)
    {
        await using var uow = _db.GetDbContext();
        return await uow.Reminders
                  .ToLinqToDBTable()
                  .Where(x => x.ServerId / 4194304 % (ulong)_creds.TotalShards == (ulong)_client.ShardId
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

        var ts = new TimeSpan((30 * values["mo"]) + (7 * values["w"]) + values["d"], values["h"], values["m"], 0);

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

            await ch.EmbedAsync(_eb.Create()
                                   .WithOkColor()
                                   .WithTitle("Reminder")
                                   .AddField("Created At",
                                       r.DateAdded.HasValue ? r.DateAdded.Value.ToLongDateString() : "?")
                                   .AddField("By",
                                       (await ch.GetUserAsync(r.UserId))?.ToString() ?? r.UserId.ToString()),
                r.Message);
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
}