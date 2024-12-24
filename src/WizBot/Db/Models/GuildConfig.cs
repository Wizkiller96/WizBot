#nullable disable
using System.ComponentModel.DataAnnotations;

namespace WizBot.Db.Models;
public class GuildFilterConfig
{
    [Key]
    public int Id { get; set; }
    
    public ulong GuildId { get; set; }
    public bool FilterInvites { get; set; }
    public bool FilterLinks { get; set; }
    public bool FilterWords { get; set; }
    public HashSet<FilterChannelId> FilterInvitesChannelIds { get; set; } = new();
    public HashSet<FilterLinksChannelId> FilterLinksChannelIds { get; set; } = new();
    public HashSet<FilteredWord> FilteredWords { get; set; } = new();
    public HashSet<FilterWordsChannelId> FilterWordsChannelIds { get; set; } = new();
}

public class GuildConfig : DbEntity
{
    public ulong GuildId { get; set; }
    public string Prefix { get; set; }

    public bool DeleteMessageOnCommand { get; set; }

    public string AutoAssignRoleIds { get; set; }

    public bool VerbosePermissions { get; set; } = true;
    public string PermissionRole { get; set; }

    //filtering
    public string MuteRoleName { get; set; }

    // chatterbot
    public bool CleverbotEnabled { get; set; }

    // aliases
    public bool WarningsInitialized { get; set; }

    public ulong? GameVoiceChannel { get; set; }
    public bool VerboseErrors { get; set; } = true;


    public bool NotifyStreamOffline { get; set; }
    public bool DeleteStreamOnlineMessage { get; set; }
    public int WarnExpireHours { get; set; }
    public WarnExpireAction WarnExpireAction { get; set; } = WarnExpireAction.Clear;

    public bool DisableGlobalExpressions { get; set; } = false;

    public bool StickyRoles { get; set; }
}