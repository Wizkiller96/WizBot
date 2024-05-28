namespace Wiz.Common;

public interface ITimezoneService
{
    TimeZoneInfo GetTimeZoneOrUtc(ulong? guildId);
}