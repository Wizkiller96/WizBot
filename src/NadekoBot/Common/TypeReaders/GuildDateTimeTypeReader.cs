using System;
using System.Threading.Tasks;
using Discord.Commands;
using NadekoBot.Modules.Administration.Services;

namespace NadekoBot.Common.TypeReaders
{
    public sealed class GuildDateTimeTypeReader : NadekoTypeReader<GuildDateTime>
    {
        private readonly GuildTimezoneService _gts;

        public GuildDateTimeTypeReader(GuildTimezoneService gts)
        {
            _gts = gts;
        }
        
        public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input)
        {
            var gdt = Parse(context.Guild.Id, input);
            if(gdt is null)
                return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "Input string is in an incorrect format."));

            return Task.FromResult(TypeReaderResult.FromSuccess(gdt));
        }

        private GuildDateTime Parse(ulong guildId, string input)
        {
            if (!DateTime.TryParse(input, out var dt))
                return null;

            var tz = _gts.GetTimeZoneOrUtc(guildId);

            return new(tz, dt);
        }
    }

    public class GuildDateTime
    {
        public TimeZoneInfo Timezone { get; }
        public DateTime CurrentGuildTime { get; }
        public DateTime InputTime { get; }
        public DateTime InputTimeUtc { get; }

        public GuildDateTime(TimeZoneInfo guildTimezone, DateTime inputTime)
        {
            var now = DateTime.UtcNow;
            Timezone = guildTimezone;
            CurrentGuildTime = TimeZoneInfo.ConvertTime(now, TimeZoneInfo.Utc, Timezone);
            InputTime = inputTime;
            InputTimeUtc = TimeZoneInfo.ConvertTime(inputTime, Timezone, TimeZoneInfo.Utc);
        }
    }
}
