using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using WizBot.Services.Database;
using WizBot.Services.Database.Models;
using WizBot.Modules.Music.Classes;

namespace WizBot.Migrations
{
    [DbContext(typeof(WizBotContext))]
    [Migration("20170409193757_gmod-and-cmod")]
    partial class gmodandcmod
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "1.1.0-rtm-22752");

            modelBuilder.Entity("WizBot.Services.Database.Models.AntiRaidSetting", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("Action");

                    b.Property<DateTime?>("DateAdded");

                    b.Property<int>("GuildConfigId");

                    b.Property<int>("Seconds");

                    b.Property<int>("UserThreshold");

                    b.HasKey("Id");

                    b.HasIndex("GuildConfigId")
                        .IsUnique();

                    b.ToTable("AntiRaidSetting");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.AntiSpamIgnore", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int?>("AntiSpamSettingId");

                    b.Property<ulong>("ChannelId");

                    b.Property<DateTime?>("DateAdded");

                    b.HasKey("Id");

                    b.HasIndex("AntiSpamSettingId");

                    b.ToTable("AntiSpamIgnore");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.AntiSpamSetting", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("Action");

                    b.Property<DateTime?>("DateAdded");

                    b.Property<int>("GuildConfigId");

                    b.Property<int>("MessageThreshold");

                    b.HasKey("Id");

                    b.HasIndex("GuildConfigId")
                        .IsUnique();

                    b.ToTable("AntiSpamSetting");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.BlacklistItem", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int?>("BotConfigId");

                    b.Property<DateTime?>("DateAdded");

                    b.Property<ulong>("ItemId");

                    b.Property<int>("Type");

                    b.HasKey("Id");

                    b.HasIndex("BotConfigId");

                    b.ToTable("BlacklistItem");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.BlockedCmdOrMdl", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int?>("BotConfigId");

                    b.Property<int?>("BotConfigId1");

                    b.Property<DateTime?>("DateAdded");

                    b.Property<string>("Name");

                    b.HasKey("Id");

                    b.HasIndex("BotConfigId");

                    b.HasIndex("BotConfigId1");

                    b.ToTable("BlockedCmdOrMdl");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.BotConfig", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<float>("BetflipMultiplier");

                    b.Property<float>("Betroll100Multiplier");

                    b.Property<float>("Betroll67Multiplier");

                    b.Property<float>("Betroll91Multiplier");

                    b.Property<ulong>("BufferSize");

                    b.Property<int>("CurrencyDropAmount");

                    b.Property<float>("CurrencyGenerationChance");

                    b.Property<int>("CurrencyGenerationCooldown");

                    b.Property<string>("CurrencyName");

                    b.Property<string>("CurrencyPluralName");

                    b.Property<string>("CurrencySign");

                    b.Property<string>("DMHelpString");

                    b.Property<DateTime?>("DateAdded");

                    b.Property<string>("ErrorColor");

                    b.Property<bool>("ForwardMessages");

                    b.Property<bool>("ForwardToAllOwners");

                    b.Property<string>("HelpString");

                    b.Property<string>("Locale");

                    b.Property<int>("MigrationVersion");

                    b.Property<int>("MinimumBetAmount");

                    b.Property<string>("OkColor");

                    b.Property<string>("RemindMessageFormat");

                    b.Property<bool>("RotatingStatuses");

                    b.Property<int>("TriviaCurrencyReward");

                    b.HasKey("Id");

                    b.ToTable("BotConfig");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.ClashCaller", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<bool>("BaseDestroyed");

                    b.Property<string>("CallUser");

                    b.Property<int>("ClashWarId");

                    b.Property<DateTime?>("DateAdded");

                    b.Property<int?>("SequenceNumber");

                    b.Property<int>("Stars");

                    b.Property<DateTime>("TimeAdded");

                    b.HasKey("Id");

                    b.HasIndex("ClashWarId");

                    b.ToTable("ClashCallers");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.ClashWar", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<ulong>("ChannelId");

                    b.Property<DateTime?>("DateAdded");

                    b.Property<string>("EnemyClan");

                    b.Property<ulong>("GuildId");

                    b.Property<int>("Size");

                    b.Property<DateTime>("StartedAt");

                    b.Property<int>("WarState");

                    b.HasKey("Id");

                    b.ToTable("ClashOfClans");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.CommandAlias", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime?>("DateAdded");

                    b.Property<int?>("GuildConfigId");

                    b.Property<string>("Mapping");

                    b.Property<string>("Trigger");

                    b.HasKey("Id");

                    b.HasIndex("GuildConfigId");

                    b.ToTable("CommandAlias");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.CommandCooldown", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("CommandName");

                    b.Property<DateTime?>("DateAdded");

                    b.Property<int?>("GuildConfigId");

                    b.Property<int>("Seconds");

                    b.HasKey("Id");

                    b.HasIndex("GuildConfigId");

                    b.ToTable("CommandCooldown");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.CommandPrice", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int?>("BotConfigId");

                    b.Property<string>("CommandName");

                    b.Property<DateTime?>("DateAdded");

                    b.Property<int>("Price");

                    b.HasKey("Id");

                    b.HasIndex("BotConfigId");

                    b.HasIndex("Price")
                        .IsUnique();

                    b.ToTable("CommandPrice");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.ConvertUnit", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime?>("DateAdded");

                    b.Property<string>("InternalTrigger");

                    b.Property<decimal>("Modifier");

                    b.Property<string>("UnitType");

                    b.HasKey("Id");

                    b.ToTable("ConversionUnits");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.Currency", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<long>("Amount");

                    b.Property<DateTime?>("DateAdded");

                    b.Property<ulong>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("UserId")
                        .IsUnique();

                    b.ToTable("Currency");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.CurrencyTransaction", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<long>("Amount");

                    b.Property<DateTime?>("DateAdded");

                    b.Property<string>("Reason");

                    b.Property<ulong>("UserId");

                    b.HasKey("Id");

                    b.ToTable("CurrencyTransactions");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.CustomReaction", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<bool>("AutoDeleteTrigger");

                    b.Property<DateTime?>("DateAdded");

                    b.Property<bool>("DmResponse");

                    b.Property<ulong?>("GuildId");

                    b.Property<bool>("IsRegex");

                    b.Property<bool>("OwnerOnly");

                    b.Property<string>("Response");

                    b.Property<string>("Trigger");

                    b.HasKey("Id");

                    b.ToTable("CustomReactions");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.DiscordUser", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("AvatarId");

                    b.Property<DateTime?>("DateAdded");

                    b.Property<string>("Discriminator");

                    b.Property<ulong>("UserId");

                    b.Property<string>("Username");

                    b.HasKey("Id");

                    b.HasAlternateKey("UserId");

                    b.ToTable("DiscordUser");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.Donator", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("Amount");

                    b.Property<DateTime?>("DateAdded");

                    b.Property<string>("Name");

                    b.Property<ulong>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("UserId")
                        .IsUnique();

                    b.ToTable("Donators");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.EightBallResponse", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int?>("BotConfigId");

                    b.Property<DateTime?>("DateAdded");

                    b.Property<string>("Text");

                    b.HasKey("Id");

                    b.HasIndex("BotConfigId");

                    b.ToTable("EightBallResponses");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.FilterChannelId", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<ulong>("ChannelId");

                    b.Property<DateTime?>("DateAdded");

                    b.Property<int?>("GuildConfigId");

                    b.Property<int?>("GuildConfigId1");

                    b.HasKey("Id");

                    b.HasIndex("GuildConfigId");

                    b.HasIndex("GuildConfigId1");

                    b.ToTable("FilterChannelId");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.FilteredWord", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime?>("DateAdded");

                    b.Property<int?>("GuildConfigId");

                    b.Property<string>("Word");

                    b.HasKey("Id");

                    b.HasIndex("GuildConfigId");

                    b.ToTable("FilteredWord");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.FollowedStream", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<ulong>("ChannelId");

                    b.Property<DateTime?>("DateAdded");

                    b.Property<int?>("GuildConfigId");

                    b.Property<ulong>("GuildId");

                    b.Property<int>("Type");

                    b.Property<string>("Username");

                    b.HasKey("Id");

                    b.HasIndex("GuildConfigId");

                    b.ToTable("FollowedStream");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.GCChannelId", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<ulong>("ChannelId");

                    b.Property<DateTime?>("DateAdded");

                    b.Property<int?>("GuildConfigId");

                    b.HasKey("Id");

                    b.HasIndex("GuildConfigId");

                    b.ToTable("GCChannelId");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.GuildConfig", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<ulong>("AutoAssignRoleId");

                    b.Property<bool>("AutoDeleteByeMessages");

                    b.Property<int>("AutoDeleteByeMessagesTimer");

                    b.Property<bool>("AutoDeleteGreetMessages");

                    b.Property<int>("AutoDeleteGreetMessagesTimer");

                    b.Property<bool>("AutoDeleteSelfAssignedRoleMessages");

                    b.Property<ulong>("ByeMessageChannelId");

                    b.Property<string>("ChannelByeMessageText");

                    b.Property<string>("ChannelGreetMessageText");

                    b.Property<bool>("CleverbotEnabled");

                    b.Property<DateTime?>("DateAdded");

                    b.Property<float>("DefaultMusicVolume");

                    b.Property<bool>("DeleteMessageOnCommand");

                    b.Property<string>("DmGreetMessageText");

                    b.Property<bool>("ExclusiveSelfAssignedRoles");

                    b.Property<bool>("FilterInvites");

                    b.Property<bool>("FilterWords");

                    b.Property<ulong?>("GameVoiceChannel");

                    b.Property<ulong>("GreetMessageChannelId");

                    b.Property<ulong>("GuildId");

                    b.Property<string>("Locale");

                    b.Property<int?>("LogSettingId");

                    b.Property<string>("MuteRoleName");

                    b.Property<string>("PermissionRole");

                    b.Property<int?>("RootPermissionId");

                    b.Property<bool>("SendChannelByeMessage");

                    b.Property<bool>("SendChannelGreetMessage");

                    b.Property<bool>("SendDmGreetMessage");

                    b.Property<string>("TimeZoneId");

                    b.Property<bool>("VerbosePermissions");

                    b.Property<bool>("VoicePlusTextEnabled");

                    b.Property<bool>("WarningsInitialized");

                    b.HasKey("Id");

                    b.HasIndex("GuildId")
                        .IsUnique();

                    b.HasIndex("LogSettingId");

                    b.HasIndex("RootPermissionId");

                    b.ToTable("GuildConfigs");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.GuildRepeater", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<ulong>("ChannelId");

                    b.Property<DateTime?>("DateAdded");

                    b.Property<int?>("GuildConfigId");

                    b.Property<ulong>("GuildId");

                    b.Property<TimeSpan>("Interval");

                    b.Property<string>("Message");

                    b.HasKey("Id");

                    b.HasIndex("GuildConfigId");

                    b.ToTable("GuildRepeater");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.IgnoredLogChannel", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<ulong>("ChannelId");

                    b.Property<DateTime?>("DateAdded");

                    b.Property<int?>("LogSettingId");

                    b.HasKey("Id");

                    b.HasIndex("LogSettingId");

                    b.ToTable("IgnoredLogChannels");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.IgnoredVoicePresenceChannel", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<ulong>("ChannelId");

                    b.Property<DateTime?>("DateAdded");

                    b.Property<int?>("LogSettingId");

                    b.HasKey("Id");

                    b.HasIndex("LogSettingId");

                    b.ToTable("IgnoredVoicePresenceCHannels");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.LogSetting", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<bool>("ChannelCreated");

                    b.Property<ulong?>("ChannelCreatedId");

                    b.Property<bool>("ChannelDestroyed");

                    b.Property<ulong?>("ChannelDestroyedId");

                    b.Property<ulong>("ChannelId");

                    b.Property<bool>("ChannelUpdated");

                    b.Property<ulong?>("ChannelUpdatedId");

                    b.Property<DateTime?>("DateAdded");

                    b.Property<bool>("IsLogging");

                    b.Property<ulong?>("LogOtherId");

                    b.Property<bool>("LogUserPresence");

                    b.Property<ulong?>("LogUserPresenceId");

                    b.Property<bool>("LogVoicePresence");

                    b.Property<ulong?>("LogVoicePresenceId");

                    b.Property<ulong?>("LogVoicePresenceTTSId");

                    b.Property<bool>("MessageDeleted");

                    b.Property<ulong?>("MessageDeletedId");

                    b.Property<bool>("MessageUpdated");

                    b.Property<ulong?>("MessageUpdatedId");

                    b.Property<bool>("UserBanned");

                    b.Property<ulong?>("UserBannedId");

                    b.Property<bool>("UserJoined");

                    b.Property<ulong?>("UserJoinedId");

                    b.Property<bool>("UserLeft");

                    b.Property<ulong?>("UserLeftId");

                    b.Property<ulong?>("UserMutedId");

                    b.Property<ulong>("UserPresenceChannelId");

                    b.Property<bool>("UserUnbanned");

                    b.Property<ulong?>("UserUnbannedId");

                    b.Property<bool>("UserUpdated");

                    b.Property<ulong?>("UserUpdatedId");

                    b.Property<ulong>("VoicePresenceChannelId");

                    b.HasKey("Id");

                    b.ToTable("LogSettings");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.ModulePrefix", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int?>("BotConfigId");

                    b.Property<DateTime?>("DateAdded");

                    b.Property<string>("ModuleName");

                    b.Property<string>("Prefix");

                    b.HasKey("Id");

                    b.HasIndex("BotConfigId");

                    b.ToTable("ModulePrefixes");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.MusicPlaylist", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Author");

                    b.Property<ulong>("AuthorId");

                    b.Property<DateTime?>("DateAdded");

                    b.Property<string>("Name");

                    b.HasKey("Id");

                    b.ToTable("MusicPlaylists");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.MutedUserId", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime?>("DateAdded");

                    b.Property<int?>("GuildConfigId");

                    b.Property<ulong>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("GuildConfigId");

                    b.ToTable("MutedUserId");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.Permission", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime?>("DateAdded");

                    b.Property<int?>("NextId");

                    b.Property<int>("PrimaryTarget");

                    b.Property<ulong>("PrimaryTargetId");

                    b.Property<int>("SecondaryTarget");

                    b.Property<string>("SecondaryTargetName");

                    b.Property<bool>("State");

                    b.HasKey("Id");

                    b.HasIndex("NextId")
                        .IsUnique();

                    b.ToTable("Permission");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.Permissionv2", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime?>("DateAdded");

                    b.Property<int?>("GuildConfigId");

                    b.Property<int>("Index");

                    b.Property<int>("PrimaryTarget");

                    b.Property<ulong>("PrimaryTargetId");

                    b.Property<int>("SecondaryTarget");

                    b.Property<string>("SecondaryTargetName");

                    b.Property<bool>("State");

                    b.HasKey("Id");

                    b.HasIndex("GuildConfigId");

                    b.ToTable("Permissionv2");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.PlayingStatus", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int?>("BotConfigId");

                    b.Property<DateTime?>("DateAdded");

                    b.Property<string>("Status");

                    b.HasKey("Id");

                    b.HasIndex("BotConfigId");

                    b.ToTable("PlayingStatus");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.PlaylistSong", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime?>("DateAdded");

                    b.Property<int?>("MusicPlaylistId");

                    b.Property<string>("Provider");

                    b.Property<int>("ProviderType");

                    b.Property<string>("Query");

                    b.Property<string>("Title");

                    b.Property<string>("Uri");

                    b.HasKey("Id");

                    b.HasIndex("MusicPlaylistId");

                    b.ToTable("PlaylistSong");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.Quote", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<ulong>("AuthorId");

                    b.Property<string>("AuthorName")
                        .IsRequired();

                    b.Property<DateTime?>("DateAdded");

                    b.Property<ulong>("GuildId");

                    b.Property<string>("Keyword")
                        .IsRequired();

                    b.Property<string>("Text")
                        .IsRequired();

                    b.HasKey("Id");

                    b.ToTable("Quotes");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.RaceAnimal", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int?>("BotConfigId");

                    b.Property<DateTime?>("DateAdded");

                    b.Property<string>("Icon");

                    b.Property<string>("Name");

                    b.HasKey("Id");

                    b.HasIndex("BotConfigId");

                    b.ToTable("RaceAnimals");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.Reminder", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<ulong>("ChannelId");

                    b.Property<DateTime?>("DateAdded");

                    b.Property<bool>("IsPrivate");

                    b.Property<string>("Message");

                    b.Property<ulong>("ServerId");

                    b.Property<ulong>("UserId");

                    b.Property<DateTime>("When");

                    b.HasKey("Id");

                    b.ToTable("Reminders");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.RewardedUser", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("AmountRewardedThisMonth");

                    b.Property<DateTime?>("DateAdded");

                    b.Property<DateTime>("LastReward");

                    b.Property<ulong>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("UserId")
                        .IsUnique();

                    b.ToTable("RewardedUsers");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.SelfAssignedRole", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime?>("DateAdded");

                    b.Property<ulong>("GuildId");

                    b.Property<ulong>("RoleId");

                    b.HasKey("Id");

                    b.HasIndex("GuildId", "RoleId")
                        .IsUnique();

                    b.ToTable("SelfAssignableRoles");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.ShopEntry", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<ulong>("AuthorId");

                    b.Property<DateTime?>("DateAdded");

                    b.Property<int?>("GuildConfigId");

                    b.Property<int>("Index");

                    b.Property<string>("Name");

                    b.Property<int>("Price");

                    b.Property<ulong>("RoleId");

                    b.Property<string>("RoleName");

                    b.Property<int>("Type");

                    b.HasKey("Id");

                    b.HasIndex("GuildConfigId");

                    b.ToTable("ShopEntry");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.ShopEntryItem", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime?>("DateAdded");

                    b.Property<int?>("ShopEntryId");

                    b.Property<string>("Text");

                    b.HasKey("Id");

                    b.HasIndex("ShopEntryId");

                    b.ToTable("ShopEntryItem");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.SlowmodeIgnoredRole", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime?>("DateAdded");

                    b.Property<int?>("GuildConfigId");

                    b.Property<ulong>("RoleId");

                    b.HasKey("Id");

                    b.HasIndex("GuildConfigId");

                    b.ToTable("SlowmodeIgnoredRole");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.SlowmodeIgnoredUser", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime?>("DateAdded");

                    b.Property<int?>("GuildConfigId");

                    b.Property<ulong>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("GuildConfigId");

                    b.ToTable("SlowmodeIgnoredUser");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.StartupCommand", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int?>("BotConfigId");

                    b.Property<ulong>("ChannelId");

                    b.Property<string>("ChannelName");

                    b.Property<string>("CommandText");

                    b.Property<DateTime?>("DateAdded");

                    b.Property<ulong?>("GuildId");

                    b.Property<string>("GuildName");

                    b.Property<int>("Index");

                    b.Property<ulong?>("VoiceChannelId");

                    b.Property<string>("VoiceChannelName");

                    b.HasKey("Id");

                    b.HasIndex("BotConfigId");

                    b.ToTable("StartupCommand");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.UnmuteTimer", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime?>("DateAdded");

                    b.Property<int?>("GuildConfigId");

                    b.Property<DateTime>("UnmuteAt");

                    b.Property<ulong>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("GuildConfigId");

                    b.ToTable("UnmuteTimer");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.UserPokeTypes", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime?>("DateAdded");

                    b.Property<ulong>("UserId");

                    b.Property<string>("type");

                    b.HasKey("Id");

                    b.HasIndex("UserId")
                        .IsUnique();

                    b.ToTable("PokeGame");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.VcRoleInfo", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime?>("DateAdded");

                    b.Property<int?>("GuildConfigId");

                    b.Property<ulong>("RoleId");

                    b.Property<ulong>("VoiceChannelId");

                    b.HasKey("Id");

                    b.HasIndex("GuildConfigId");

                    b.ToTable("VcRoleInfo");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.WaifuInfo", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int?>("AffinityId");

                    b.Property<int?>("ClaimerId");

                    b.Property<DateTime?>("DateAdded");

                    b.Property<int>("Price");

                    b.Property<int>("WaifuId");

                    b.HasKey("Id");

                    b.HasIndex("AffinityId");

                    b.HasIndex("ClaimerId");

                    b.HasIndex("WaifuId")
                        .IsUnique();

                    b.ToTable("WaifuInfo");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.WaifuUpdate", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime?>("DateAdded");

                    b.Property<int?>("NewId");

                    b.Property<int?>("OldId");

                    b.Property<int>("UpdateType");

                    b.Property<int>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("NewId");

                    b.HasIndex("OldId");

                    b.HasIndex("UserId");

                    b.ToTable("WaifuUpdates");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.Warning", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime?>("DateAdded");

                    b.Property<bool>("Forgiven");

                    b.Property<string>("ForgivenBy");

                    b.Property<ulong>("GuildId");

                    b.Property<string>("Moderator");

                    b.Property<string>("Reason");

                    b.Property<ulong>("UserId");

                    b.HasKey("Id");

                    b.ToTable("Warnings");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.WarningPunishment", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("Count");

                    b.Property<DateTime?>("DateAdded");

                    b.Property<int?>("GuildConfigId");

                    b.Property<int>("Punishment");

                    b.Property<int>("Time");

                    b.HasKey("Id");

                    b.HasIndex("GuildConfigId");

                    b.ToTable("WarningPunishment");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.AntiRaidSetting", b =>
                {
                    b.HasOne("WizBot.Services.Database.Models.GuildConfig", "GuildConfig")
                        .WithOne("AntiRaidSetting")
                        .HasForeignKey("WizBot.Services.Database.Models.AntiRaidSetting", "GuildConfigId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.AntiSpamIgnore", b =>
                {
                    b.HasOne("WizBot.Services.Database.Models.AntiSpamSetting")
                        .WithMany("IgnoredChannels")
                        .HasForeignKey("AntiSpamSettingId");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.AntiSpamSetting", b =>
                {
                    b.HasOne("WizBot.Services.Database.Models.GuildConfig", "GuildConfig")
                        .WithOne("AntiSpamSetting")
                        .HasForeignKey("WizBot.Services.Database.Models.AntiSpamSetting", "GuildConfigId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.BlacklistItem", b =>
                {
                    b.HasOne("WizBot.Services.Database.Models.BotConfig")
                        .WithMany("Blacklist")
                        .HasForeignKey("BotConfigId");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.BlockedCmdOrMdl", b =>
                {
                    b.HasOne("WizBot.Services.Database.Models.BotConfig")
                        .WithMany("BlockedCommands")
                        .HasForeignKey("BotConfigId");

                    b.HasOne("WizBot.Services.Database.Models.BotConfig")
                        .WithMany("BlockedModules")
                        .HasForeignKey("BotConfigId1");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.ClashCaller", b =>
                {
                    b.HasOne("WizBot.Services.Database.Models.ClashWar", "ClashWar")
                        .WithMany("Bases")
                        .HasForeignKey("ClashWarId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.CommandAlias", b =>
                {
                    b.HasOne("WizBot.Services.Database.Models.GuildConfig")
                        .WithMany("CommandAliases")
                        .HasForeignKey("GuildConfigId");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.CommandCooldown", b =>
                {
                    b.HasOne("WizBot.Services.Database.Models.GuildConfig")
                        .WithMany("CommandCooldowns")
                        .HasForeignKey("GuildConfigId");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.CommandPrice", b =>
                {
                    b.HasOne("WizBot.Services.Database.Models.BotConfig")
                        .WithMany("CommandPrices")
                        .HasForeignKey("BotConfigId");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.EightBallResponse", b =>
                {
                    b.HasOne("WizBot.Services.Database.Models.BotConfig")
                        .WithMany("EightBallResponses")
                        .HasForeignKey("BotConfigId");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.FilterChannelId", b =>
                {
                    b.HasOne("WizBot.Services.Database.Models.GuildConfig")
                        .WithMany("FilterInvitesChannelIds")
                        .HasForeignKey("GuildConfigId");

                    b.HasOne("WizBot.Services.Database.Models.GuildConfig")
                        .WithMany("FilterWordsChannelIds")
                        .HasForeignKey("GuildConfigId1");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.FilteredWord", b =>
                {
                    b.HasOne("WizBot.Services.Database.Models.GuildConfig")
                        .WithMany("FilteredWords")
                        .HasForeignKey("GuildConfigId");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.FollowedStream", b =>
                {
                    b.HasOne("WizBot.Services.Database.Models.GuildConfig")
                        .WithMany("FollowedStreams")
                        .HasForeignKey("GuildConfigId");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.GCChannelId", b =>
                {
                    b.HasOne("WizBot.Services.Database.Models.GuildConfig")
                        .WithMany("GenerateCurrencyChannelIds")
                        .HasForeignKey("GuildConfigId");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.GuildConfig", b =>
                {
                    b.HasOne("WizBot.Services.Database.Models.LogSetting", "LogSetting")
                        .WithMany()
                        .HasForeignKey("LogSettingId");

                    b.HasOne("WizBot.Services.Database.Models.Permission", "RootPermission")
                        .WithMany()
                        .HasForeignKey("RootPermissionId");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.GuildRepeater", b =>
                {
                    b.HasOne("WizBot.Services.Database.Models.GuildConfig")
                        .WithMany("GuildRepeaters")
                        .HasForeignKey("GuildConfigId");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.IgnoredLogChannel", b =>
                {
                    b.HasOne("WizBot.Services.Database.Models.LogSetting", "LogSetting")
                        .WithMany("IgnoredChannels")
                        .HasForeignKey("LogSettingId");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.IgnoredVoicePresenceChannel", b =>
                {
                    b.HasOne("WizBot.Services.Database.Models.LogSetting", "LogSetting")
                        .WithMany("IgnoredVoicePresenceChannelIds")
                        .HasForeignKey("LogSettingId");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.ModulePrefix", b =>
                {
                    b.HasOne("WizBot.Services.Database.Models.BotConfig")
                        .WithMany("ModulePrefixes")
                        .HasForeignKey("BotConfigId");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.MutedUserId", b =>
                {
                    b.HasOne("WizBot.Services.Database.Models.GuildConfig")
                        .WithMany("MutedUsers")
                        .HasForeignKey("GuildConfigId");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.Permission", b =>
                {
                    b.HasOne("WizBot.Services.Database.Models.Permission", "Next")
                        .WithOne("Previous")
                        .HasForeignKey("WizBot.Services.Database.Models.Permission", "NextId");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.Permissionv2", b =>
                {
                    b.HasOne("WizBot.Services.Database.Models.GuildConfig")
                        .WithMany("Permissions")
                        .HasForeignKey("GuildConfigId");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.PlayingStatus", b =>
                {
                    b.HasOne("WizBot.Services.Database.Models.BotConfig")
                        .WithMany("RotatingStatusMessages")
                        .HasForeignKey("BotConfigId");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.PlaylistSong", b =>
                {
                    b.HasOne("WizBot.Services.Database.Models.MusicPlaylist")
                        .WithMany("Songs")
                        .HasForeignKey("MusicPlaylistId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.RaceAnimal", b =>
                {
                    b.HasOne("WizBot.Services.Database.Models.BotConfig")
                        .WithMany("RaceAnimals")
                        .HasForeignKey("BotConfigId");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.ShopEntry", b =>
                {
                    b.HasOne("WizBot.Services.Database.Models.GuildConfig")
                        .WithMany("ShopEntries")
                        .HasForeignKey("GuildConfigId");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.ShopEntryItem", b =>
                {
                    b.HasOne("WizBot.Services.Database.Models.ShopEntry")
                        .WithMany("Items")
                        .HasForeignKey("ShopEntryId");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.SlowmodeIgnoredRole", b =>
                {
                    b.HasOne("WizBot.Services.Database.Models.GuildConfig")
                        .WithMany("SlowmodeIgnoredRoles")
                        .HasForeignKey("GuildConfigId");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.SlowmodeIgnoredUser", b =>
                {
                    b.HasOne("WizBot.Services.Database.Models.GuildConfig")
                        .WithMany("SlowmodeIgnoredUsers")
                        .HasForeignKey("GuildConfigId");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.StartupCommand", b =>
                {
                    b.HasOne("WizBot.Services.Database.Models.BotConfig")
                        .WithMany("StartupCommands")
                        .HasForeignKey("BotConfigId");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.UnmuteTimer", b =>
                {
                    b.HasOne("WizBot.Services.Database.Models.GuildConfig")
                        .WithMany("UnmuteTimers")
                        .HasForeignKey("GuildConfigId");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.VcRoleInfo", b =>
                {
                    b.HasOne("WizBot.Services.Database.Models.GuildConfig")
                        .WithMany("VcRoleInfos")
                        .HasForeignKey("GuildConfigId");
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.WaifuInfo", b =>
                {
                    b.HasOne("WizBot.Services.Database.Models.DiscordUser", "Affinity")
                        .WithMany()
                        .HasForeignKey("AffinityId");

                    b.HasOne("WizBot.Services.Database.Models.DiscordUser", "Claimer")
                        .WithMany()
                        .HasForeignKey("ClaimerId");

                    b.HasOne("WizBot.Services.Database.Models.DiscordUser", "Waifu")
                        .WithOne()
                        .HasForeignKey("WizBot.Services.Database.Models.WaifuInfo", "WaifuId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.WaifuUpdate", b =>
                {
                    b.HasOne("WizBot.Services.Database.Models.DiscordUser", "New")
                        .WithMany()
                        .HasForeignKey("NewId");

                    b.HasOne("WizBot.Services.Database.Models.DiscordUser", "Old")
                        .WithMany()
                        .HasForeignKey("OldId");

                    b.HasOne("WizBot.Services.Database.Models.DiscordUser", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("WizBot.Services.Database.Models.WarningPunishment", b =>
                {
                    b.HasOne("WizBot.Services.Database.Models.GuildConfig")
                        .WithMany("WarnPunishments")
                        .HasForeignKey("GuildConfigId");
                });
        }
    }
}
