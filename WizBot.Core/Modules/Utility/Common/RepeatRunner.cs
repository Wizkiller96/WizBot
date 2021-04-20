﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using WizBot.Extensions;
using WizBot.Core.Services.Database.Models;
using NLog;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using WizBot.Common;
using WizBot.Common.Replacements;
using WizBot.Modules.Utility.Services;

namespace WizBot.Modules.Utility.Common
{
    public class RepeatRunner
    {
        private readonly Logger _log;

        public Repeater Repeater { get; }
        public SocketGuild Guild { get; }

        private readonly MessageRepeaterService _mrs;

        public ITextChannel Channel { get; private set; }

        private TimeSpan _initialInterval;

        public TimeSpan InitialInterval
        {
            get => _initialInterval;
            private set
            {
                _initialInterval = value;
                NextDateTime = DateTime.UtcNow + value;
            }
        }

        /// <summary>
        /// When's the next time the repeater will run.
        /// On bot startup, it will be InitialInterval + StartupDateTime.
        /// After first execution, it will be Interval + ExecutionDateTime
        /// </summary>
        public DateTime NextDateTime { get; set; }

        private Timer _t;
        private readonly DiscordSocketClient _client;

        public RepeatRunner(DiscordSocketClient client, SocketGuild guild, Repeater repeater, MessageRepeaterService mrs)
        {
            _log = LogManager.GetCurrentClassLogger();
            Repeater = repeater;
            Guild = guild;
            _mrs = mrs;
            _client = client;

            InitialInterval = Repeater.Interval;

            Run();
        }

        private void Run()
        {
            if (Repeater.StartTimeOfDay != null)
            {
                // if there was a start time of day
                // calculate whats the next time of day repeat should trigger at
                // based on teh dateadded

                // i know this is not null because of the .Where in the repeat service
                var added = Repeater.DateAdded.Value;

                // initial trigger was the time of day specified by the command.
                var initialTriggerTimeOfDay = Repeater.StartTimeOfDay.Value;

                DateTime initialDateTime;

                // if added timeofday is less than specified timeofday for initial trigger
                // that means the repeater first ran that same day at that exact specified time
                if (added.TimeOfDay <= initialTriggerTimeOfDay)
                {
                    // in that case, just add the difference to make sure the timeofday is the same
                    initialDateTime = added + (initialTriggerTimeOfDay - added.TimeOfDay);
                }
                else
                {
                    // if not, then it ran at that time the following day
                    // in other words; Add one day, and subtract how much time passed since that time of day
                    initialDateTime = added + TimeSpan.FromDays(1) - (added.TimeOfDay - initialTriggerTimeOfDay);
                }

                CalculateInitialInterval(initialDateTime);
            }
            else
            {
                // if repeater is not running daily, it's initial time is the time it was Added at, plus the interval
                CalculateInitialInterval(Repeater.DateAdded.Value + Repeater.Interval);
            }

            // wait at least a minute for the bot to have all data needed in the cache
            if (InitialInterval < TimeSpan.FromMinutes(1))
                InitialInterval = TimeSpan.FromMinutes(1);

            _t = new Timer(async (_) =>
            {
                try
                {
                    await Trigger().ConfigureAwait(false);
                }
                catch
                {
                }
            }, null, InitialInterval, Repeater.Interval);
        }

        /// <summary>
        /// Calculate when is the proper time to run the repeater again based on initial time repeater ran.
        /// </summary>
        /// <param name="initialDateTime">Initial time repeater ran at (or should run at).</param>
        private void CalculateInitialInterval(DateTime initialDateTime)
        {
            // if the initial time is greater than now, that means the repeater didn't still execute a single time.
            // just schedule it
            if (initialDateTime > DateTime.UtcNow)
            {
                InitialInterval = initialDateTime - DateTime.UtcNow;
            }
            else
            {
                // else calculate based on minutes difference

                // get the difference
                var diff = DateTime.UtcNow - initialDateTime;

                // see how many times the repeater theoretically ran already
                var triggerCount = diff / Repeater.Interval;

                // ok lets say repeater was scheduled to run 10h ago.
                // we have an interval of 2.4h
                // repeater should've ran 4 times- that's 9.6h
                // next time should be in 2h from now exactly
                // 10/2.4 is 4.166
                // 4.166 - Math.Truncate(4.166) is 0.166
                // initial interval multiplier is 1 - 0.166 = 0.834
                // interval (2.4h) * 0.834 is 2.0016 and that is the initial interval

                var initialIntervalMultiplier = 1 - (triggerCount - Math.Truncate(triggerCount));
                InitialInterval = Repeater.Interval * initialIntervalMultiplier;
            }
        }

        public async Task Trigger()
        {
            async Task ChannelMissingError()
            {
                _log.Warn("Channel not found or insufficient permissions. Repeater stopped. ChannelId : {0}",
                    Channel?.Id);
                Stop();
                await _mrs.RemoveRepeater(Repeater);
            }

            // next execution is interval amount of time after now
            NextDateTime = DateTime.UtcNow + Repeater.Interval;

            var toSend = Repeater.Message;
            try
            {
                Channel = Channel ?? Guild.GetTextChannel(Repeater.ChannelId);

                if (Channel == null)
                {
                    await ChannelMissingError().ConfigureAwait(false);
                    return;
                }

                if (Repeater.NoRedundant)
                {
                    var lastMsgInChannel = (await Channel.GetMessagesAsync(2).FlattenAsync().ConfigureAwait(false))
                        .FirstOrDefault();
                    if (lastMsgInChannel != null && lastMsgInChannel.Id == Repeater.LastMessageId
                    ) //don't send if it's the same message in the channel
                        return;
                }

                // if the message needs to be send
                // delete previous message if it exists
                try
                {
                    if (Repeater.LastMessageId != null)
                    {
                        var oldMsg = await Channel.GetMessageAsync(Repeater.LastMessageId.Value).ConfigureAwait(false);
                        if (oldMsg != null)
                        {
                            await oldMsg.DeleteAsync().ConfigureAwait(false);
                            oldMsg = null;
                        }
                    }
                }
                catch
                {
                    // ignored
                }

                var rep = new ReplacementBuilder()
                    .WithDefault(Guild.CurrentUser, Channel, Guild, _client)
                    .Build();

                IMessage newMsg;
                if (CREmbed.TryParse(toSend, out var crEmbed))
                {
                    rep.Replace(crEmbed);
                    newMsg = await Channel.EmbedAsync(crEmbed);
                }
                else
                {
                    newMsg = await Channel.SendMessageAsync(rep.Replace(toSend));
                }
                _ = newMsg.AddReactionAsync(new Emoji("🔄"));

                if (Repeater.NoRedundant)
                {
                    _mrs.SetRepeaterLastMessage(Repeater.Id, newMsg.Id);
                    Repeater.LastMessageId = newMsg.Id;
                }
            }
            catch (HttpException ex)
            {
                _log.Warn(ex.Message);
                await ChannelMissingError().ConfigureAwait(false);
                return;
            }
            catch (Exception ex)
            {
                _log.Warn(ex);
                Stop();
                await _mrs.RemoveRepeater(Repeater).ConfigureAwait(false);
            }
        }

        public void Reset()
        {
            Stop();
            Run();
        }

        public void Stop()
        {
            _t.Change(Timeout.Infinite, Timeout.Infinite);
        }

        public override string ToString() =>
            $"{Channel?.Mention ?? $"⚠<#{Repeater.ChannelId}>"} " +
            (this.Repeater.NoRedundant ? "| ✍" : "") +
            $"| {(int)Repeater.Interval.TotalHours}:{Repeater.Interval:mm} " +
            $"| {Repeater.Message.TrimTo(33)}";
    }
}
