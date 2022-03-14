#nullable disable
using WizBot.Services.Database.Models;

namespace WizBot.Modules.Permissions.Common;

public class PermissionCache
{
    public string PermRole { get; set; }
    public bool Verbose { get; set; } = true;
    public PermissionsCollection<Permissionv2> Permissions { get; set; }
}