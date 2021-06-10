﻿using WizBot.Common.Collections;
using System.Collections.Generic;

namespace WizBot.Core.Services.Database.Models
{
    public class GuildConfig : DbEntity
    {
        public ulong GuildId { get; set; }

        public string Prefix { get; set; } = null;

        public bool DeleteMessageOnCommand { get; set; }
        public HashSet<DelMsgOnCmdChannel> DelMsgOnCmdChannels { get; set; } = new HashSet<DelMsgOnCmdChannel>();
        public ulong AutoAssignRoleId { get; set; }
        //greet stuff
        public bool AutoDeleteGreetMessages { get; set; } //unused
        public bool AutoDeleteByeMessages { get; set; } // unused
        public int AutoDeleteGreetMessagesTimer { get; set; } = 30;
        public int AutoDeleteByeMessagesTimer { get; set; } = 30;

        public ulong GreetMessageChannelId { get; set; }
        public ulong ByeMessageChannelId { get; set; }

        public bool SendDmGreetMessage { get; set; }
        public string DmGreetMessageText { get; set; } = "Welcome to the %server% server, %user%!";

        public bool SendChannelGreetMessage { get; set; }
        public string ChannelGreetMessageText { get; set; } = "Welcome to the %server% server, %user%!";

        public bool SendChannelByeMessage { get; set; }
        public string ChannelByeMessageText { get; set; } = "%user% has left!";

        public LogSetting LogSetting { get; set; } = new LogSetting();

        //self assignable roles
        public bool ExclusiveSelfAssignedRoles { get; set; }
        public bool AutoDeleteSelfAssignedRoleMessages { get; set; }
        public float DefaultMusicVolume { get; set; } = 1.0f;
        public bool VoicePlusTextEnabled { get; set; }

        //stream notifications
        public HashSet<FollowedStream> FollowedStreams { get; set; } = new HashSet<FollowedStream>();

        //currencyGeneration
        public HashSet<GCChannelId> GenerateCurrencyChannelIds { get; set; } = new HashSet<GCChannelId>();

        //permissions
        public Permission RootPermission { get; set; } = null;
        public List<Permissionv2> Permissions { get; set; }
        public bool VerbosePermissions { get; set; } = true;
        public string PermissionRole { get; set; } = null;

        public HashSet<CommandCooldown> CommandCooldowns { get; set; } = new HashSet<CommandCooldown>();

        //filtering
        public bool FilterInvites { get; set; }
        public bool FilterLinks { get; set; }
        public HashSet<FilterChannelId> FilterInvitesChannelIds { get; set; } = new HashSet<FilterChannelId>();
        public HashSet<FilterLinksChannelId> FilterLinksChannelIds { get; set; } = new HashSet<FilterLinksChannelId>();

        //public bool FilterLinks { get; set; }
        //public HashSet<FilterLinksChannelId> FilterLinksChannels { get; set; } = new HashSet<FilterLinksChannelId>();

        public bool FilterWords { get; set; }
        public HashSet<FilteredWord> FilteredWords { get; set; } = new HashSet<FilteredWord>();
        public HashSet<FilterChannelId> FilterWordsChannelIds { get; set; } = new HashSet<FilterChannelId>();

        public HashSet<MutedUserId> MutedUsers { get; set; } = new HashSet<MutedUserId>();

        public string MuteRoleName { get; set; }
        public bool CleverbotEnabled { get; set; }

        public AntiRaidSetting AntiRaidSetting { get; set; }
        public AntiSpamSetting AntiSpamSetting { get; set; }
        public AntiAltSetting AntiAltSetting { get; set; }

        public string Locale { get; set; } = null;
        public string TimeZoneId { get; set; } = null;

        public HashSet<UnmuteTimer> UnmuteTimers { get; set; } = new HashSet<UnmuteTimer>();
        public HashSet<UnbanTimer> UnbanTimer { get; set; } = new HashSet<UnbanTimer>();
        public HashSet<UnroleTimer> UnroleTimer { get; set; } = new HashSet<UnroleTimer>();
        public HashSet<VcRoleInfo> VcRoleInfos { get; set; }
        public HashSet<CommandAlias> CommandAliases { get; set; } = new HashSet<CommandAlias>();
        public List<WarningPunishment> WarnPunishments { get; set; } = new List<WarningPunishment>();
        public bool WarningsInitialized { get; set; }
        public HashSet<SlowmodeIgnoredUser> SlowmodeIgnoredUsers { get; set; }
        public HashSet<SlowmodeIgnoredRole> SlowmodeIgnoredRoles { get; set; }
        public HashSet<NsfwBlacklitedTag> NsfwBlacklistedTags { get; set; } = new HashSet<NsfwBlacklitedTag>();

        public List<ShopEntry> ShopEntries { get; set; }
        public ulong? GameVoiceChannel { get; set; } = null;
        public bool VerboseErrors { get; set; } = false;

        public StreamRoleSettings StreamRole { get; set; }

        public XpSettings XpSettings { get; set; }
        public List<FeedSub> FeedSubs { get; set; } = new List<FeedSub>();
        public bool AutoDcFromVc { get; set; }
        public IndexedCollection<ReactionRoleMessage> ReactionRoleMessages { get; set; } = new IndexedCollection<ReactionRoleMessage>();
        public bool NotifyStreamOffline { get; set; }
        public List<GroupName> SelfAssignableRoleGroupNames { get; set; }
        public int WarnExpireHours { get; set; } = 0;
        public WarnExpireAction WarnExpireAction { get; set; } = WarnExpireAction.Clear;
    }
}
