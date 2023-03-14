#nullable disable
using Nadeko.Bot.Db;

namespace NadekoBot.Services.Database.Models;

public class DiscordPermOverride : DbEntity
{
    public GuildPerm Perm { get; set; }

    public ulong? GuildId { get; set; }
    public string Command { get; set; }
}