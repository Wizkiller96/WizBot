using Discord;

namespace WizBot.Core.Services.Database.Models
{
    public class DiscordPermOverride : DbEntity
    {
        public ChannelPerm Perm { get; set; }

        public ulong? GuildId { get; set; }
        public string Command { get; set; }
    }
}