using System;

namespace NadekoBot.Services
{
    public interface IStatsService
    {
        /// <summary>
        /// The author of the bot.
        /// </summary>
        string Author { get; }

        /// <summary>
        /// The total amount of commands ran since startup.
        /// </summary>
        long CommandsRan { get; }

        /// <summary>
        /// The Discord framework used by the bot.
        /// </summary>
        string Library { get; }

        /// <summary>
        /// The amount of messages seen by the bot since startup.
        /// </summary>
        long MessageCounter { get; }

        /// <summary>
        /// The rate of messages the bot sees every second.
        /// </summary>
        double MessagesPerSecond { get; }

        /// <summary>
        /// The total amount of text channels the bot can see.
        /// </summary>
        long TextChannels { get; }

        /// <summary>
        /// The total amount of voice channels the bot can see.
        /// </summary>
        long VoiceChannels { get; }

        /// <summary>
        /// Gets for how long the bot has been up since startup.
        /// </summary>
        TimeSpan GetUptime();

        /// <summary>
        /// Gets a formatted string of how long the bot has been up since startup.
        /// </summary>
        /// <param name="separator">The formatting separator.</param>
        string GetUptimeString(string separator = ", ");

        /// <summary>
        /// Gets total amount of private memory currently in use by the bot, in Megabytes.
        /// </summary>
        double GetPrivateMemory();
    }
}
