namespace WizBot.Common;

public interface ITimezoneService
{
    TimeZoneInfo GetTimeZoneOrUtc(ulong? guildId);
}