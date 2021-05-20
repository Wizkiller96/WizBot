﻿using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;

namespace WizBot.Core.Services.Database.Models
{
    public class CustomReaction : DbEntity
    {

        [NotMapped]
        public Regex Regex { get; set; }
        public ulong UseCount { get; set; }
        public bool IsRegex { get; set; }
        public bool OwnerOnly { get; set; }
        
        public ulong? GuildId { get; set; }
        public string Response { get; set; }
        public string Trigger { get; set; }

        public bool AutoDeleteTrigger { get; set; }
        public bool DmResponse { get; set; }
        public bool ContainsAnywhere { get; set; }
        public bool AllowTarget { get; set; }
        public string Reactions { get; set; }

        public string[] GetReactions() =>
            Array.Empty<string>();
        
        public bool IsGlobal() => GuildId is null || GuildId == 0;
    }

    public class ReactionResponse : DbEntity
    {
        public bool OwnerOnly { get; set; }
        public string Text { get; set; }
    }
}