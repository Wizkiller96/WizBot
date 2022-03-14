#nullable disable
using WizBot.Services.Database.Models;

namespace WizBot.Modules.Administration;

public class PunishQueueItem
{
    public PunishmentAction Action { get; set; }
    public ProtectionType Type { get; set; }
    public int MuteTime { get; set; }
    public ulong? RoleId { get; set; }
    public IGuildUser User { get; set; }
}