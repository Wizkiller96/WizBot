#nullable disable
namespace WizBot.Db.Models;

public class GuildConfig : DbEntity
{
    // public bool Keep { get; set; }
    public ulong GuildId { get; set; }

    public string Prefix { get; set; }

    public bool DeleteMessageOnCommand { get; set; }
    public HashSet<DelMsgOnCmdChannel> DelMsgOnCmdChannels { get; set; } = new();

    public string AutoAssignRoleIds { get; set; }

    //todo FUTURE: DELETE, UNUSED
    public bool ExclusiveSelfAssignedRoles { get; set; }
    public bool AutoDeleteSelfAssignedRoleMessages { get; set; }


    //stream notifications
    public HashSet<FollowedStream> FollowedStreams { get; set; } = new();

    //currencyGeneration
    public HashSet<GCChannelId> GenerateCurrencyChannelIds { get; set; } = new();

    public List<Permissionv2> Permissions { get; set; }
    public bool VerbosePermissions { get; set; } = true;
    public string PermissionRole { get; set; }

    public HashSet<CommandCooldown> CommandCooldowns { get; set; } = new();

    //filtering
    public bool FilterInvites { get; set; }
    public bool FilterLinks { get; set; }
    public HashSet<FilterChannelId> FilterInvitesChannelIds { get; set; } = new();
    public HashSet<FilterLinksChannelId> FilterLinksChannelIds { get; set; } = new();
    
    public bool FilterWords { get; set; }
    public HashSet<FilteredWord> FilteredWords { get; set; } = new();
    public HashSet<FilterWordsChannelId> FilterWordsChannelIds { get; set; } = new();

    // mute
    public HashSet<MutedUserId> MutedUsers { get; set; } = new();

    public string MuteRoleName { get; set; }
    
    // chatterbot
    public bool CleverbotEnabled { get; set; }

    // protection
    public AntiRaidSetting AntiRaidSetting { get; set; }
    public AntiSpamSetting AntiSpamSetting { get; set; }
    public AntiAltSetting AntiAltSetting { get; set; }

    // time
    public string Locale { get; set; }
    public string TimeZoneId { get; set; }

    // timers 
    public HashSet<UnmuteTimer> UnmuteTimers { get; set; } = new();
    public HashSet<UnbanTimer> UnbanTimer { get; set; } = new();
    public HashSet<UnroleTimer> UnroleTimer { get; set; } = new();
    
    // vcrole
    public HashSet<VcRoleInfo> VcRoleInfos { get; set; }
    
    // aliases
    public HashSet<CommandAlias> CommandAliases { get; set; } = new();
    public bool WarningsInitialized { get; set; }
    public HashSet<SlowmodeIgnoredUser> SlowmodeIgnoredUsers { get; set; }
    public HashSet<SlowmodeIgnoredRole> SlowmodeIgnoredRoles { get; set; }

    public List<ShopEntry> ShopEntries { get; set; }
    public ulong? GameVoiceChannel { get; set; }
    public bool VerboseErrors { get; set; } = true;

    public StreamRoleSettings StreamRole { get; set; }

    public XpSettings XpSettings { get; set; }
    public List<FeedSub> FeedSubs { get; set; } = new();
    public bool NotifyStreamOffline { get; set; }
    public bool DeleteStreamOnlineMessage { get; set; }
    public int WarnExpireHours { get; set; }
    public WarnExpireAction WarnExpireAction { get; set; } = WarnExpireAction.Clear;

    public bool DisableGlobalExpressions { get; set; } = false;

    public bool StickyRoles { get; set; }
}