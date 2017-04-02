using System;

namespace WizBot.Services.Database.Models
{
    public class RewardedUser : DbEntity
    {
        public ulong UserId { get; set; }
        public int AmountRewardedThisMonth { get; set; }
        public DateTime LastReward { get; set; }
    }
}