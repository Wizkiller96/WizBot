﻿using System;

namespace WizBot.Services.Database.Models
{
    public class Repeater
    {
        public int Id { get; set; }
        public ulong GuildId { get; set; }
        public ulong ChannelId { get; set; }
        public ulong? LastMessageId { get; set; }
        public string Message { get; set; }
        public TimeSpan Interval { get; set; }
        public TimeSpan? StartTimeOfDay { get; set; }
        public bool NoRedundant { get; set; }
        public DateTime DateAdded { get; set; }
    }
}
