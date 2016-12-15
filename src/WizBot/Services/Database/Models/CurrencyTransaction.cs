﻿namespace WizBot.Services.Database.Models
{
    public class CurrencyTransaction : DbEntity
    {
        public long Amount { get; set; }
        public string Reason { get; set; }
        public ulong UserId { get; set; }
    }
}
