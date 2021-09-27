using System;

namespace WizBot.Services
{
    public interface IStatsService
    {
        string Author { get; }
        long CommandsRan { get; }
        string Heap { get; }
        string Library { get; }
        long MessageCounter { get; }
        double MessagesPerSecond { get; }
        long TextChannels { get; }
        long VoiceChannels { get; }

        TimeSpan GetUptime();
        string GetUptimeString(string separator = ", ");
    }
}
