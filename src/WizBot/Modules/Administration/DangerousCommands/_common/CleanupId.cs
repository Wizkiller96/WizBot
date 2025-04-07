using System.ComponentModel.DataAnnotations;

namespace WizBot.Modules.Administration.DangerousCommands;

public sealed class CleanupId
{
    [Key]
    public ulong GuildId { get; set; }
}