using System.ComponentModel.DataAnnotations;

namespace WizBot.Db.Models;

public class GuildColors
{
    [Key]
    public ulong GuildId { get; set; }

    [Length(0, 9)]
    public string? OkColor { get; set; }

    [Length(0, 9)]
    public string? ErrorColor { get; set; }

    [Length(0, 9)]
    public string? PendingColor { get; set; }
}