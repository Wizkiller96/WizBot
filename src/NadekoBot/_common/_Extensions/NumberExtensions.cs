namespace NadekoBot.Extensions;

public static class NumberExtensions
{
    public static DateTimeOffset ToUnixTimestamp(this double number)
        => new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero).AddSeconds(number);
}