#nullable disable
using WizBot.Db;
using WizBot.Db.Models;
using WizBot.Common.ModuleBehaviors;

namespace WizBot.Modules.Administration.Services;

public sealed class GuildTimezoneService : ITimezoneService, IReadyExecutor, INService
{
    private readonly ConcurrentDictionary<ulong, TimeZoneInfo> _timezones;
    private readonly DbService _db;
    private readonly IReplacementPatternStore _repStore;

    public GuildTimezoneService(IBot bot, DbService db, IReplacementPatternStore repStore)
    {
        _timezones = bot.AllGuildConfigs.Select(GetTimzezoneTuple)
            .Where(x => x.Timezone is not null)
            .ToDictionary(x => x.GuildId, x => x.Timezone)
            .ToConcurrent();

        _db = db;
        _repStore = repStore;

        bot.JoinedGuild += Bot_JoinedGuild;
    }

    private Task Bot_JoinedGuild(GuildConfig arg)
    {
        var (guildId, tz) = GetTimzezoneTuple(arg);
        if (tz is not null)
            _timezones.TryAdd(guildId, tz);
        return Task.CompletedTask;
    }

    private static (ulong GuildId, TimeZoneInfo Timezone) GetTimzezoneTuple(GuildConfig x)
    {
        TimeZoneInfo tz;
        try
        {
            if (x.TimeZoneId is null)
                tz = null;
            else
                tz = TimeZoneInfo.FindSystemTimeZoneById(x.TimeZoneId);
        }
        catch
        {
            tz = null;
        }

        return (x.GuildId, Timezone: tz);
    }

    public TimeZoneInfo GetTimeZoneOrDefault(ulong? guildId)
    {
        if (guildId is ulong gid && _timezones.TryGetValue(gid, out var tz))
            return tz;

        return null;
    }

    public void SetTimeZone(ulong guildId, TimeZoneInfo tz)
    {
        using var uow = _db.GetDbContext();
        var gc = uow.GuildConfigsForId(guildId, set => set);

        gc.TimeZoneId = tz?.Id;
        uow.SaveChanges();

        if (tz is null)
            _timezones.TryRemove(guildId, out tz);
        else
            _timezones.AddOrUpdate(guildId, tz, (_, _) => tz);
    }

    public TimeZoneInfo GetTimeZoneOrUtc(ulong? guildId)
        => GetTimeZoneOrDefault(guildId) ?? TimeZoneInfo.Utc;

    public Task OnReadyAsync()
    {
        _repStore.Register("%server.time%",
            (IGuild g) =>
            {
                var to = TimeZoneInfo.Local;
                if (g is not null)
                {
                    to = GetTimeZoneOrDefault(g.Id) ?? TimeZoneInfo.Local;
                }

                return TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.Utc, to).ToString("HH:mm ")
                       + to.StandardName.GetInitials();
            });

        return Task.CompletedTask;
    }
}