#nullable disable
using NadekoBot.Db;
using NadekoBot.Services.Database.Models;

namespace NadekoBot.Modules.Administration.Services;

public class GuildTimezoneService : INService
{
    public static ConcurrentDictionary<ulong, GuildTimezoneService> AllServices { get; } = new();
    private readonly ConcurrentDictionary<ulong, TimeZoneInfo> _timezones;
    private readonly DbService _db;

    public GuildTimezoneService(DiscordSocketClient client, Bot bot, DbService db)
    {
        _timezones = bot.AllGuildConfigs.Select(GetTimzezoneTuple)
                        .Where(x => x.Timezone is not null)
                        .ToDictionary(x => x.GuildId, x => x.Timezone)
                        .ToConcurrent();

        var curUser = client.CurrentUser;
        if (curUser is not null)
            AllServices.TryAdd(curUser.Id, this);
        _db = db;

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

    public TimeZoneInfo GetTimeZoneOrDefault(ulong guildId)
    {
        if (_timezones.TryGetValue(guildId, out var tz))
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

    public TimeZoneInfo GetTimeZoneOrUtc(ulong guildId)
        => GetTimeZoneOrDefault(guildId) ?? TimeZoneInfo.Utc;
}