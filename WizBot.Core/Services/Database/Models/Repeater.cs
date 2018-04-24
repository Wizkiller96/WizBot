using CommandLine;
using WizBot.Core.Common;
using System;

namespace WizBot.Core.Services.Database.Models
{
    public class Repeater : DbEntity
    {
        public class Options : IWizBotCommandOptions
        {
            [Option('m', "message", Required = true,
                HelpText = "Message to be repeated")]
            public string Message { get; set; } = "";

            [Option('n', "no-redundant", Required = false, Default = false,
                HelpText = "Whether the message should be reposted if the last message in the channel is this same message.")]
            public bool NoRedundant { get; set; } = false;

            [Option('i', "interval", Required = false, Default = 5,
                HelpText = "How frequently the repeating message is posted, in minutes.")]
            public int Interval { get; set; } = 5;
            //[Option('s', "start-time", Required = false, Default = null,
            //    HelpText = "At what time will the repeater first run.")]
            //public string StrStartTimeOfDay { get; set; } = null;
            //public TimeSpan StartTimeOfDay { get; set; }

            public void NormalizeOptions()
            {
                if (Interval < 1)
                    Interval = 5;
            }
        }

        public ulong GuildId { get; set; }
        public ulong ChannelId { get; set; }
        public string Message { get; set; }
        public TimeSpan Interval { get; set; }
        public TimeSpan? StartTimeOfDay { get; set; }
        public bool NoRedundant { get; set; }
    }

    public class GuildRepeater : Repeater
    {
    }
}