using CommandLine;
using WizBot.Core.Common;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace WizBot.Core.Services.Database.Models
{
    [Table("GuildRepeater")]
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

            [Option('i', "interval", Required = false, Default = null,
                HelpText = "How frequently the repeating message is posted, in minutes.")]
            public int? Interval { get; set; } = null;
            //[Option('s', "start-time", Required = false, Default = null,
            //    HelpText = "At what time will the repeater first run.")]
            //public string StrStartTimeOfDay { get; set; } = null;
            //public TimeSpan StartTimeOfDay { get; set; }

            public void NormalizeOptions()
            {
                if (Interval < 1)
                    Interval = null;

                if (Interval >= 25001)
                    Interval = 25001;
            }
        }

        public ulong GuildId { get; set; }
        public ulong ChannelId { get; set; }
        public ulong? LastMessageId { get; set; }
        public string Message { get; set; }
        public TimeSpan Interval { get; set; }
        public TimeSpan? StartTimeOfDay { get; set; }
        public bool NoRedundant { get; set; }
    }
}
