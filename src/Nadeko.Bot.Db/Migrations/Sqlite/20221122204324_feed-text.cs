using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NadekoBot.Db.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class init_v5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AutoCommands",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CommandText = table.Column<string>(type: "TEXT", nullable: true),
                    ChannelId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    ChannelName = table.Column<string>(type: "TEXT", nullable: true),
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    GuildName = table.Column<string>(type: "TEXT", nullable: true),
                    VoiceChannelId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    VoiceChannelName = table.Column<string>(type: "TEXT", nullable: true),
                    Interval = table.Column<int>(type: "INTEGER", nullable: false),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutoCommands", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AutoPublishChannel",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    ChannelId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutoPublishChannel", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AutoTranslateChannels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    ChannelId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    AutoDelete = table.Column<bool>(type: "INTEGER", nullable: false),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutoTranslateChannels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BankUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Balance = table.Column<long>(type: "INTEGER", nullable: false),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BanTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Text = table.Column<string>(type: "TEXT", nullable: true),
                    PruneDays = table.Column<int>(type: "INTEGER", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BanTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Blacklist",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ItemId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Blacklist", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CurrencyTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Amount = table.Column<long>(type: "INTEGER", nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: true),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    Extra = table.Column<string>(type: "TEXT", nullable: false),
                    OtherId = table.Column<ulong>(type: "INTEGER", nullable: true, defaultValueSql: "NULL"),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurrencyTransactions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DiscordPermOverrides",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Perm = table.Column<ulong>(type: "INTEGER", nullable: false),
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    Command = table.Column<string>(type: "TEXT", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscordPermOverrides", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Expressions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    Response = table.Column<string>(type: "TEXT", nullable: true),
                    Trigger = table.Column<string>(type: "TEXT", nullable: true),
                    AutoDeleteTrigger = table.Column<bool>(type: "INTEGER", nullable: false),
                    DmResponse = table.Column<bool>(type: "INTEGER", nullable: false),
                    ContainsAnywhere = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowTarget = table.Column<bool>(type: "INTEGER", nullable: false),
                    Reactions = table.Column<string>(type: "TEXT", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Expressions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GamblingStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Feature = table.Column<string>(type: "TEXT", nullable: true),
                    Bet = table.Column<decimal>(type: "TEXT", nullable: false),
                    PaidOut = table.Column<decimal>(type: "TEXT", nullable: false),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GamblingStats", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GuildConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Prefix = table.Column<string>(type: "TEXT", nullable: true),
                    DeleteMessageOnCommand = table.Column<bool>(type: "INTEGER", nullable: false),
                    AutoAssignRoleIds = table.Column<string>(type: "TEXT", nullable: true),
                    AutoDeleteGreetMessagesTimer = table.Column<int>(type: "INTEGER", nullable: false),
                    AutoDeleteByeMessagesTimer = table.Column<int>(type: "INTEGER", nullable: false),
                    GreetMessageChannelId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    ByeMessageChannelId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    SendDmGreetMessage = table.Column<bool>(type: "INTEGER", nullable: false),
                    DmGreetMessageText = table.Column<string>(type: "TEXT", nullable: true),
                    SendChannelGreetMessage = table.Column<bool>(type: "INTEGER", nullable: false),
                    ChannelGreetMessageText = table.Column<string>(type: "TEXT", nullable: true),
                    SendChannelByeMessage = table.Column<bool>(type: "INTEGER", nullable: false),
                    ChannelByeMessageText = table.Column<string>(type: "TEXT", nullable: true),
                    ExclusiveSelfAssignedRoles = table.Column<bool>(type: "INTEGER", nullable: false),
                    AutoDeleteSelfAssignedRoleMessages = table.Column<bool>(type: "INTEGER", nullable: false),
                    VerbosePermissions = table.Column<bool>(type: "INTEGER", nullable: false),
                    PermissionRole = table.Column<string>(type: "TEXT", nullable: true),
                    FilterInvites = table.Column<bool>(type: "INTEGER", nullable: false),
                    FilterLinks = table.Column<bool>(type: "INTEGER", nullable: false),
                    FilterWords = table.Column<bool>(type: "INTEGER", nullable: false),
                    MuteRoleName = table.Column<string>(type: "TEXT", nullable: true),
                    CleverbotEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    Locale = table.Column<string>(type: "TEXT", nullable: true),
                    TimeZoneId = table.Column<string>(type: "TEXT", nullable: true),
                    WarningsInitialized = table.Column<bool>(type: "INTEGER", nullable: false),
                    GameVoiceChannel = table.Column<ulong>(type: "INTEGER", nullable: true),
                    VerboseErrors = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    NotifyStreamOffline = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeleteStreamOnlineMessage = table.Column<bool>(type: "INTEGER", nullable: false),
                    WarnExpireHours = table.Column<int>(type: "INTEGER", nullable: false),
                    WarnExpireAction = table.Column<int>(type: "INTEGER", nullable: false),
                    DisableGlobalExpressions = table.Column<bool>(type: "INTEGER", nullable: false),
                    SendBoostMessage = table.Column<bool>(type: "INTEGER", nullable: false),
                    BoostMessage = table.Column<string>(type: "TEXT", nullable: true),
                    BoostMessageChannelId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    BoostMessageDeleteAfter = table.Column<int>(type: "INTEGER", nullable: false),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImageOnlyChannels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    ChannelId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImageOnlyChannels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LogSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    LogOtherId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    MessageUpdatedId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    MessageDeletedId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    UserJoinedId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    UserLeftId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    UserBannedId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    UserUnbannedId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    UserUpdatedId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    ChannelCreatedId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    ChannelDestroyedId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    ChannelUpdatedId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    ThreadDeletedId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    ThreadCreatedId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    UserMutedId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    LogUserPresenceId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    LogVoicePresenceId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    LogVoicePresenceTTSId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    LogWarnsId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MusicPlayerSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    PlayerRepeat = table.Column<int>(type: "INTEGER", nullable: false),
                    MusicChannelId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    Volume = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 100),
                    AutoDisconnect = table.Column<bool>(type: "INTEGER", nullable: false),
                    QualityPreset = table.Column<int>(type: "INTEGER", nullable: false),
                    AutoPlay = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MusicPlayerSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MusicPlaylists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Author = table.Column<string>(type: "TEXT", nullable: true),
                    AuthorId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MusicPlaylists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NsfwBlacklistedTags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Tag = table.Column<string>(type: "TEXT", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NsfwBlacklistedTags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PatronQuotas",
                columns: table => new
                {
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    FeatureType = table.Column<int>(type: "INTEGER", nullable: false),
                    Feature = table.Column<string>(type: "TEXT", nullable: false),
                    HourlyCount = table.Column<uint>(type: "INTEGER", nullable: false),
                    DailyCount = table.Column<uint>(type: "INTEGER", nullable: false),
                    MonthlyCount = table.Column<uint>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatronQuotas", x => new { x.UserId, x.FeatureType, x.Feature });
                });

            migrationBuilder.CreateTable(
                name: "Patrons",
                columns: table => new
                {
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UniquePlatformUserId = table.Column<string>(type: "TEXT", nullable: true),
                    AmountCents = table.Column<int>(type: "INTEGER", nullable: false),
                    LastCharge = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ValidThru = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Patrons", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "PlantedCurrency",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Amount = table.Column<long>(type: "INTEGER", nullable: false),
                    Password = table.Column<string>(type: "TEXT", nullable: true),
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    ChannelId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    MessageId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlantedCurrency", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Poll",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    ChannelId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Question = table.Column<string>(type: "TEXT", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Poll", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Quotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Keyword = table.Column<string>(type: "TEXT", nullable: false),
                    AuthorName = table.Column<string>(type: "TEXT", nullable: false),
                    AuthorId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Text = table.Column<string>(type: "TEXT", nullable: false),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quotes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReactionRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    ChannelId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    MessageId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Emote = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    RoleId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Group = table.Column<int>(type: "INTEGER", nullable: false),
                    LevelReq = table.Column<int>(type: "INTEGER", nullable: false),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReactionRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Reminders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    When = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ChannelId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    ServerId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: true),
                    IsPrivate = table.Column<bool>(type: "INTEGER", nullable: false),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reminders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Repeaters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    ChannelId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    LastMessageId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    Message = table.Column<string>(type: "TEXT", nullable: true),
                    Interval = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    StartTimeOfDay = table.Column<TimeSpan>(type: "TEXT", nullable: true),
                    NoRedundant = table.Column<bool>(type: "INTEGER", nullable: false),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Repeaters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RewardedUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    PlatformUserId = table.Column<string>(type: "TEXT", nullable: true),
                    AmountRewardedThisMonth = table.Column<long>(type: "INTEGER", nullable: false),
                    LastReward = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RewardedUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RotatingStatus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Status = table.Column<string>(type: "TEXT", nullable: true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RotatingStatus", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SelfAssignableRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    RoleId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Group = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    LevelRequirement = table.Column<int>(type: "INTEGER", nullable: false),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SelfAssignableRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StreamOnlineMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChannelId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    MessageId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StreamOnlineMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserXpStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Xp = table.Column<long>(type: "INTEGER", nullable: false),
                    AwardedXp = table.Column<long>(type: "INTEGER", nullable: false),
                    NotifyOnLevelUp = table.Column<int>(type: "INTEGER", nullable: false),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserXpStats", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Warnings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", nullable: true),
                    Forgiven = table.Column<bool>(type: "INTEGER", nullable: false),
                    ForgivenBy = table.Column<string>(type: "TEXT", nullable: true),
                    Moderator = table.Column<string>(type: "TEXT", nullable: true),
                    Weight = table.Column<long>(type: "INTEGER", nullable: false, defaultValue: 1L),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Warnings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "XpShopOwnedItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    ItemType = table.Column<int>(type: "INTEGER", nullable: false),
                    IsUsing = table.Column<bool>(type: "INTEGER", nullable: false),
                    ItemKey = table.Column<string>(type: "TEXT", nullable: false),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_XpShopOwnedItem", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AutoTranslateUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChannelId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Source = table.Column<string>(type: "TEXT", nullable: true),
                    Target = table.Column<string>(type: "TEXT", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutoTranslateUsers", x => x.Id);
                    table.UniqueConstraint("AK_AutoTranslateUsers_ChannelId_UserId", x => new { x.ChannelId, x.UserId });
                    table.ForeignKey(
                        name: "FK_AutoTranslateUsers_AutoTranslateChannels_ChannelId",
                        column: x => x.ChannelId,
                        principalTable: "AutoTranslateChannels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AntiAltSetting",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildConfigId = table.Column<int>(type: "INTEGER", nullable: false),
                    MinAge = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    Action = table.Column<int>(type: "INTEGER", nullable: false),
                    ActionDurationMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    RoleId = table.Column<ulong>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AntiAltSetting", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AntiAltSetting_GuildConfigs_GuildConfigId",
                        column: x => x.GuildConfigId,
                        principalTable: "GuildConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AntiRaidSetting",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildConfigId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserThreshold = table.Column<int>(type: "INTEGER", nullable: false),
                    Seconds = table.Column<int>(type: "INTEGER", nullable: false),
                    Action = table.Column<int>(type: "INTEGER", nullable: false),
                    PunishDuration = table.Column<int>(type: "INTEGER", nullable: false),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AntiRaidSetting", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AntiRaidSetting_GuildConfigs_GuildConfigId",
                        column: x => x.GuildConfigId,
                        principalTable: "GuildConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AntiSpamSetting",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildConfigId = table.Column<int>(type: "INTEGER", nullable: false),
                    Action = table.Column<int>(type: "INTEGER", nullable: false),
                    MessageThreshold = table.Column<int>(type: "INTEGER", nullable: false),
                    MuteTime = table.Column<int>(type: "INTEGER", nullable: false),
                    RoleId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AntiSpamSetting", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AntiSpamSetting_GuildConfigs_GuildConfigId",
                        column: x => x.GuildConfigId,
                        principalTable: "GuildConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommandAlias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Trigger = table.Column<string>(type: "TEXT", nullable: true),
                    Mapping = table.Column<string>(type: "TEXT", nullable: true),
                    GuildConfigId = table.Column<int>(type: "INTEGER", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommandAlias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommandAlias_GuildConfigs_GuildConfigId",
                        column: x => x.GuildConfigId,
                        principalTable: "GuildConfigs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CommandCooldown",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Seconds = table.Column<int>(type: "INTEGER", nullable: false),
                    CommandName = table.Column<string>(type: "TEXT", nullable: true),
                    GuildConfigId = table.Column<int>(type: "INTEGER", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommandCooldown", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommandCooldown_GuildConfigs_GuildConfigId",
                        column: x => x.GuildConfigId,
                        principalTable: "GuildConfigs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DelMsgOnCmdChannel",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChannelId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    State = table.Column<bool>(type: "INTEGER", nullable: false),
                    GuildConfigId = table.Column<int>(type: "INTEGER", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DelMsgOnCmdChannel", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DelMsgOnCmdChannel_GuildConfigs_GuildConfigId",
                        column: x => x.GuildConfigId,
                        principalTable: "GuildConfigs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FeedSub",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildConfigId = table.Column<int>(type: "INTEGER", nullable: false),
                    ChannelId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Url = table.Column<string>(type: "TEXT", nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeedSub", x => x.Id);
                    table.UniqueConstraint("AK_FeedSub_GuildConfigId_Url", x => new { x.GuildConfigId, x.Url });
                    table.ForeignKey(
                        name: "FK_FeedSub_GuildConfigs_GuildConfigId",
                        column: x => x.GuildConfigId,
                        principalTable: "GuildConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FilterChannelId",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChannelId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    GuildConfigId = table.Column<int>(type: "INTEGER", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FilterChannelId", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FilterChannelId_GuildConfigs_GuildConfigId",
                        column: x => x.GuildConfigId,
                        principalTable: "GuildConfigs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FilteredWord",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Word = table.Column<string>(type: "TEXT", nullable: true),
                    GuildConfigId = table.Column<int>(type: "INTEGER", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FilteredWord", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FilteredWord_GuildConfigs_GuildConfigId",
                        column: x => x.GuildConfigId,
                        principalTable: "GuildConfigs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FilterLinksChannelId",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChannelId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    GuildConfigId = table.Column<int>(type: "INTEGER", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FilterLinksChannelId", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FilterLinksChannelId_GuildConfigs_GuildConfigId",
                        column: x => x.GuildConfigId,
                        principalTable: "GuildConfigs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FilterWordsChannelId",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChannelId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    GuildConfigId = table.Column<int>(type: "INTEGER", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FilterWordsChannelId", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FilterWordsChannelId_GuildConfigs_GuildConfigId",
                        column: x => x.GuildConfigId,
                        principalTable: "GuildConfigs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FollowedStream",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    ChannelId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Username = table.Column<string>(type: "TEXT", nullable: true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: true),
                    GuildConfigId = table.Column<int>(type: "INTEGER", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FollowedStream", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FollowedStream_GuildConfigs_GuildConfigId",
                        column: x => x.GuildConfigId,
                        principalTable: "GuildConfigs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "GCChannelId",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildConfigId = table.Column<int>(type: "INTEGER", nullable: true),
                    ChannelId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GCChannelId", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GCChannelId_GuildConfigs_GuildConfigId",
                        column: x => x.GuildConfigId,
                        principalTable: "GuildConfigs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "GroupName",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildConfigId = table.Column<int>(type: "INTEGER", nullable: false),
                    Number = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupName", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GroupName_GuildConfigs_GuildConfigId",
                        column: x => x.GuildConfigId,
                        principalTable: "GuildConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MutedUserId",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    GuildConfigId = table.Column<int>(type: "INTEGER", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MutedUserId", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MutedUserId_GuildConfigs_GuildConfigId",
                        column: x => x.GuildConfigId,
                        principalTable: "GuildConfigs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildConfigId = table.Column<int>(type: "INTEGER", nullable: true),
                    Index = table.Column<int>(type: "INTEGER", nullable: false),
                    PrimaryTarget = table.Column<int>(type: "INTEGER", nullable: false),
                    PrimaryTargetId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    SecondaryTarget = table.Column<int>(type: "INTEGER", nullable: false),
                    SecondaryTargetName = table.Column<string>(type: "TEXT", nullable: true),
                    IsCustomCommand = table.Column<bool>(type: "INTEGER", nullable: false),
                    State = table.Column<bool>(type: "INTEGER", nullable: false),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Permissions_GuildConfigs_GuildConfigId",
                        column: x => x.GuildConfigId,
                        principalTable: "GuildConfigs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ShopEntry",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Index = table.Column<int>(type: "INTEGER", nullable: false),
                    Price = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    AuthorId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    RoleName = table.Column<string>(type: "TEXT", nullable: true),
                    RoleId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    RoleRequirement = table.Column<ulong>(type: "INTEGER", nullable: true),
                    GuildConfigId = table.Column<int>(type: "INTEGER", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopEntry", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShopEntry_GuildConfigs_GuildConfigId",
                        column: x => x.GuildConfigId,
                        principalTable: "GuildConfigs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SlowmodeIgnoredRole",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RoleId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    GuildConfigId = table.Column<int>(type: "INTEGER", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlowmodeIgnoredRole", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlowmodeIgnoredRole_GuildConfigs_GuildConfigId",
                        column: x => x.GuildConfigId,
                        principalTable: "GuildConfigs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SlowmodeIgnoredUser",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    GuildConfigId = table.Column<int>(type: "INTEGER", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlowmodeIgnoredUser", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlowmodeIgnoredUser_GuildConfigs_GuildConfigId",
                        column: x => x.GuildConfigId,
                        principalTable: "GuildConfigs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StreamRoleSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildConfigId = table.Column<int>(type: "INTEGER", nullable: false),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    AddRoleId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    FromRoleId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Keyword = table.Column<string>(type: "TEXT", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StreamRoleSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StreamRoleSettings_GuildConfigs_GuildConfigId",
                        column: x => x.GuildConfigId,
                        principalTable: "GuildConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UnbanTimer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    UnbanAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    GuildConfigId = table.Column<int>(type: "INTEGER", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnbanTimer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UnbanTimer_GuildConfigs_GuildConfigId",
                        column: x => x.GuildConfigId,
                        principalTable: "GuildConfigs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UnmuteTimer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    UnmuteAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    GuildConfigId = table.Column<int>(type: "INTEGER", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnmuteTimer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UnmuteTimer_GuildConfigs_GuildConfigId",
                        column: x => x.GuildConfigId,
                        principalTable: "GuildConfigs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UnroleTimer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    RoleId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    UnbanAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    GuildConfigId = table.Column<int>(type: "INTEGER", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnroleTimer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UnroleTimer_GuildConfigs_GuildConfigId",
                        column: x => x.GuildConfigId,
                        principalTable: "GuildConfigs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "VcRoleInfo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    VoiceChannelId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    RoleId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    GuildConfigId = table.Column<int>(type: "INTEGER", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VcRoleInfo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VcRoleInfo_GuildConfigs_GuildConfigId",
                        column: x => x.GuildConfigId,
                        principalTable: "GuildConfigs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WarningPunishment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Count = table.Column<int>(type: "INTEGER", nullable: false),
                    Punishment = table.Column<int>(type: "INTEGER", nullable: false),
                    Time = table.Column<int>(type: "INTEGER", nullable: false),
                    RoleId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    GuildConfigId = table.Column<int>(type: "INTEGER", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarningPunishment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarningPunishment_GuildConfigs_GuildConfigId",
                        column: x => x.GuildConfigId,
                        principalTable: "GuildConfigs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "XpSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildConfigId = table.Column<int>(type: "INTEGER", nullable: false),
                    ServerExcluded = table.Column<bool>(type: "INTEGER", nullable: false),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_XpSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_XpSettings_GuildConfigs_GuildConfigId",
                        column: x => x.GuildConfigId,
                        principalTable: "GuildConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IgnoredLogChannels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LogSettingId = table.Column<int>(type: "INTEGER", nullable: false),
                    LogItemId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    ItemType = table.Column<int>(type: "INTEGER", nullable: false),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IgnoredLogChannels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IgnoredLogChannels_LogSettings_LogSettingId",
                        column: x => x.LogSettingId,
                        principalTable: "LogSettings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IgnoredVoicePresenceCHannels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LogSettingId = table.Column<int>(type: "INTEGER", nullable: true),
                    ChannelId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IgnoredVoicePresenceCHannels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IgnoredVoicePresenceCHannels_LogSettings_LogSettingId",
                        column: x => x.LogSettingId,
                        principalTable: "LogSettings",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PlaylistSong",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Provider = table.Column<string>(type: "TEXT", nullable: true),
                    ProviderType = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: true),
                    Uri = table.Column<string>(type: "TEXT", nullable: true),
                    Query = table.Column<string>(type: "TEXT", nullable: true),
                    MusicPlaylistId = table.Column<int>(type: "INTEGER", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaylistSong", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlaylistSong_MusicPlaylists_MusicPlaylistId",
                        column: x => x.MusicPlaylistId,
                        principalTable: "MusicPlaylists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PollAnswer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Index = table.Column<int>(type: "INTEGER", nullable: false),
                    Text = table.Column<string>(type: "TEXT", nullable: true),
                    PollId = table.Column<int>(type: "INTEGER", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PollAnswer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PollAnswer_Poll_PollId",
                        column: x => x.PollId,
                        principalTable: "Poll",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PollVote",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    VoteIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    PollId = table.Column<int>(type: "INTEGER", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PollVote", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PollVote_Poll_PollId",
                        column: x => x.PollId,
                        principalTable: "Poll",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AntiSpamIgnore",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChannelId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    AntiSpamSettingId = table.Column<int>(type: "INTEGER", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AntiSpamIgnore", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AntiSpamIgnore_AntiSpamSetting_AntiSpamSettingId",
                        column: x => x.AntiSpamSettingId,
                        principalTable: "AntiSpamSetting",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ShopEntryItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Text = table.Column<string>(type: "TEXT", nullable: true),
                    ShopEntryId = table.Column<int>(type: "INTEGER", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopEntryItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShopEntryItem_ShopEntry_ShopEntryId",
                        column: x => x.ShopEntryId,
                        principalTable: "ShopEntry",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StreamRoleBlacklistedUser",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Username = table.Column<string>(type: "TEXT", nullable: true),
                    StreamRoleSettingsId = table.Column<int>(type: "INTEGER", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StreamRoleBlacklistedUser", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StreamRoleBlacklistedUser_StreamRoleSettings_StreamRoleSettingsId",
                        column: x => x.StreamRoleSettingsId,
                        principalTable: "StreamRoleSettings",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StreamRoleWhitelistedUser",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Username = table.Column<string>(type: "TEXT", nullable: true),
                    StreamRoleSettingsId = table.Column<int>(type: "INTEGER", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StreamRoleWhitelistedUser", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StreamRoleWhitelistedUser_StreamRoleSettings_StreamRoleSettingsId",
                        column: x => x.StreamRoleSettingsId,
                        principalTable: "StreamRoleSettings",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ExcludedItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ItemId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    ItemType = table.Column<int>(type: "INTEGER", nullable: false),
                    XpSettingsId = table.Column<int>(type: "INTEGER", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExcludedItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExcludedItem_XpSettings_XpSettingsId",
                        column: x => x.XpSettingsId,
                        principalTable: "XpSettings",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "XpCurrencyReward",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    XpSettingsId = table.Column<int>(type: "INTEGER", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    Amount = table.Column<int>(type: "INTEGER", nullable: false),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_XpCurrencyReward", x => x.Id);
                    table.ForeignKey(
                        name: "FK_XpCurrencyReward_XpSettings_XpSettingsId",
                        column: x => x.XpSettingsId,
                        principalTable: "XpSettings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "XpRoleReward",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    XpSettingsId = table.Column<int>(type: "INTEGER", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    RoleId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Remove = table.Column<bool>(type: "INTEGER", nullable: false),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_XpRoleReward", x => x.Id);
                    table.ForeignKey(
                        name: "FK_XpRoleReward_XpSettings_XpSettingsId",
                        column: x => x.XpSettingsId,
                        principalTable: "XpSettings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClubApplicants",
                columns: table => new
                {
                    ClubId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClubApplicants", x => new { x.ClubId, x.UserId });
                });

            migrationBuilder.CreateTable(
                name: "ClubBans",
                columns: table => new
                {
                    ClubId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClubBans", x => new { x.ClubId, x.UserId });
                });

            migrationBuilder.CreateTable(
                name: "Clubs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    ImageUrl = table.Column<string>(type: "TEXT", nullable: true),
                    Xp = table.Column<int>(type: "INTEGER", nullable: false),
                    OwnerId = table.Column<int>(type: "INTEGER", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clubs", x => x.Id);
                    table.UniqueConstraint("AK_Clubs_Name", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "DiscordUser",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Username = table.Column<string>(type: "TEXT", nullable: true),
                    Discriminator = table.Column<string>(type: "TEXT", nullable: true),
                    AvatarId = table.Column<string>(type: "TEXT", nullable: true),
                    ClubId = table.Column<int>(type: "INTEGER", nullable: true),
                    IsClubAdmin = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    TotalXp = table.Column<long>(type: "INTEGER", nullable: false, defaultValue: 0L),
                    NotifyOnLevelUp = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    CurrencyAmount = table.Column<long>(type: "INTEGER", nullable: false, defaultValue: 0L),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscordUser", x => x.Id);
                    table.UniqueConstraint("AK_DiscordUser_UserId", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_DiscordUser_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WaifuInfo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WaifuId = table.Column<int>(type: "INTEGER", nullable: false),
                    ClaimerId = table.Column<int>(type: "INTEGER", nullable: true),
                    AffinityId = table.Column<int>(type: "INTEGER", nullable: true),
                    Price = table.Column<long>(type: "INTEGER", nullable: false),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WaifuInfo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WaifuInfo_DiscordUser_AffinityId",
                        column: x => x.AffinityId,
                        principalTable: "DiscordUser",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WaifuInfo_DiscordUser_ClaimerId",
                        column: x => x.ClaimerId,
                        principalTable: "DiscordUser",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WaifuInfo_DiscordUser_WaifuId",
                        column: x => x.WaifuId,
                        principalTable: "DiscordUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WaifuUpdates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdateType = table.Column<int>(type: "INTEGER", nullable: false),
                    OldId = table.Column<int>(type: "INTEGER", nullable: true),
                    NewId = table.Column<int>(type: "INTEGER", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WaifuUpdates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WaifuUpdates_DiscordUser_NewId",
                        column: x => x.NewId,
                        principalTable: "DiscordUser",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WaifuUpdates_DiscordUser_OldId",
                        column: x => x.OldId,
                        principalTable: "DiscordUser",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WaifuUpdates_DiscordUser_UserId",
                        column: x => x.UserId,
                        principalTable: "DiscordUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WaifuItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WaifuInfoId = table.Column<int>(type: "INTEGER", nullable: true),
                    ItemEmoji = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WaifuItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WaifuItem_WaifuInfo_WaifuInfoId",
                        column: x => x.WaifuInfoId,
                        principalTable: "WaifuInfo",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AntiAltSetting_GuildConfigId",
                table: "AntiAltSetting",
                column: "GuildConfigId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AntiRaidSetting_GuildConfigId",
                table: "AntiRaidSetting",
                column: "GuildConfigId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AntiSpamIgnore_AntiSpamSettingId",
                table: "AntiSpamIgnore",
                column: "AntiSpamSettingId");

            migrationBuilder.CreateIndex(
                name: "IX_AntiSpamSetting_GuildConfigId",
                table: "AntiSpamSetting",
                column: "GuildConfigId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AutoPublishChannel_GuildId",
                table: "AutoPublishChannel",
                column: "GuildId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AutoTranslateChannels_ChannelId",
                table: "AutoTranslateChannels",
                column: "ChannelId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AutoTranslateChannels_GuildId",
                table: "AutoTranslateChannels",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_BankUsers_UserId",
                table: "BankUsers",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BanTemplates_GuildId",
                table: "BanTemplates",
                column: "GuildId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClubApplicants_UserId",
                table: "ClubApplicants",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ClubBans_UserId",
                table: "ClubBans",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Clubs_OwnerId",
                table: "Clubs",
                column: "OwnerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommandAlias_GuildConfigId",
                table: "CommandAlias",
                column: "GuildConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_CommandCooldown_GuildConfigId",
                table: "CommandCooldown",
                column: "GuildConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyTransactions_UserId",
                table: "CurrencyTransactions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DelMsgOnCmdChannel_GuildConfigId",
                table: "DelMsgOnCmdChannel",
                column: "GuildConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscordPermOverrides_GuildId_Command",
                table: "DiscordPermOverrides",
                columns: new[] { "GuildId", "Command" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DiscordUser_ClubId",
                table: "DiscordUser",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscordUser_CurrencyAmount",
                table: "DiscordUser",
                column: "CurrencyAmount");

            migrationBuilder.CreateIndex(
                name: "IX_DiscordUser_TotalXp",
                table: "DiscordUser",
                column: "TotalXp");

            migrationBuilder.CreateIndex(
                name: "IX_DiscordUser_UserId",
                table: "DiscordUser",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExcludedItem_XpSettingsId",
                table: "ExcludedItem",
                column: "XpSettingsId");

            migrationBuilder.CreateIndex(
                name: "IX_FilterChannelId_GuildConfigId",
                table: "FilterChannelId",
                column: "GuildConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_FilteredWord_GuildConfigId",
                table: "FilteredWord",
                column: "GuildConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_FilterLinksChannelId_GuildConfigId",
                table: "FilterLinksChannelId",
                column: "GuildConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_FilterWordsChannelId_GuildConfigId",
                table: "FilterWordsChannelId",
                column: "GuildConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_FollowedStream_GuildConfigId",
                table: "FollowedStream",
                column: "GuildConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_GamblingStats_Feature",
                table: "GamblingStats",
                column: "Feature",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GCChannelId_GuildConfigId",
                table: "GCChannelId",
                column: "GuildConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupName_GuildConfigId_Number",
                table: "GroupName",
                columns: new[] { "GuildConfigId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GuildConfigs_GuildId",
                table: "GuildConfigs",
                column: "GuildId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GuildConfigs_WarnExpireHours",
                table: "GuildConfigs",
                column: "WarnExpireHours");

            migrationBuilder.CreateIndex(
                name: "IX_IgnoredLogChannels_LogSettingId_LogItemId_ItemType",
                table: "IgnoredLogChannels",
                columns: new[] { "LogSettingId", "LogItemId", "ItemType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IgnoredVoicePresenceCHannels_LogSettingId",
                table: "IgnoredVoicePresenceCHannels",
                column: "LogSettingId");

            migrationBuilder.CreateIndex(
                name: "IX_ImageOnlyChannels_ChannelId",
                table: "ImageOnlyChannels",
                column: "ChannelId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LogSettings_GuildId",
                table: "LogSettings",
                column: "GuildId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MusicPlayerSettings_GuildId",
                table: "MusicPlayerSettings",
                column: "GuildId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MutedUserId_GuildConfigId",
                table: "MutedUserId",
                column: "GuildConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_NsfwBlacklistedTags_GuildId",
                table: "NsfwBlacklistedTags",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_PatronQuotas_UserId",
                table: "PatronQuotas",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Patrons_UniquePlatformUserId",
                table: "Patrons",
                column: "UniquePlatformUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_GuildConfigId",
                table: "Permissions",
                column: "GuildConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_PlantedCurrency_ChannelId",
                table: "PlantedCurrency",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_PlantedCurrency_MessageId",
                table: "PlantedCurrency",
                column: "MessageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlaylistSong_MusicPlaylistId",
                table: "PlaylistSong",
                column: "MusicPlaylistId");

            migrationBuilder.CreateIndex(
                name: "IX_Poll_GuildId",
                table: "Poll",
                column: "GuildId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PollAnswer_PollId",
                table: "PollAnswer",
                column: "PollId");

            migrationBuilder.CreateIndex(
                name: "IX_PollVote_PollId",
                table: "PollVote",
                column: "PollId");

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_GuildId",
                table: "Quotes",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_Keyword",
                table: "Quotes",
                column: "Keyword");

            migrationBuilder.CreateIndex(
                name: "IX_ReactionRoles_GuildId",
                table: "ReactionRoles",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_ReactionRoles_MessageId_Emote",
                table: "ReactionRoles",
                columns: new[] { "MessageId", "Emote" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reminders_When",
                table: "Reminders",
                column: "When");

            migrationBuilder.CreateIndex(
                name: "IX_RewardedUsers_PlatformUserId",
                table: "RewardedUsers",
                column: "PlatformUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SelfAssignableRoles_GuildId_RoleId",
                table: "SelfAssignableRoles",
                columns: new[] { "GuildId", "RoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShopEntry_GuildConfigId",
                table: "ShopEntry",
                column: "GuildConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopEntryItem_ShopEntryId",
                table: "ShopEntryItem",
                column: "ShopEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_SlowmodeIgnoredRole_GuildConfigId",
                table: "SlowmodeIgnoredRole",
                column: "GuildConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_SlowmodeIgnoredUser_GuildConfigId",
                table: "SlowmodeIgnoredUser",
                column: "GuildConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_StreamRoleBlacklistedUser_StreamRoleSettingsId",
                table: "StreamRoleBlacklistedUser",
                column: "StreamRoleSettingsId");

            migrationBuilder.CreateIndex(
                name: "IX_StreamRoleSettings_GuildConfigId",
                table: "StreamRoleSettings",
                column: "GuildConfigId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StreamRoleWhitelistedUser_StreamRoleSettingsId",
                table: "StreamRoleWhitelistedUser",
                column: "StreamRoleSettingsId");

            migrationBuilder.CreateIndex(
                name: "IX_UnbanTimer_GuildConfigId",
                table: "UnbanTimer",
                column: "GuildConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_UnmuteTimer_GuildConfigId",
                table: "UnmuteTimer",
                column: "GuildConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_UnroleTimer_GuildConfigId",
                table: "UnroleTimer",
                column: "GuildConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_UserXpStats_AwardedXp",
                table: "UserXpStats",
                column: "AwardedXp");

            migrationBuilder.CreateIndex(
                name: "IX_UserXpStats_GuildId",
                table: "UserXpStats",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_UserXpStats_UserId",
                table: "UserXpStats",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserXpStats_UserId_GuildId",
                table: "UserXpStats",
                columns: new[] { "UserId", "GuildId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserXpStats_Xp",
                table: "UserXpStats",
                column: "Xp");

            migrationBuilder.CreateIndex(
                name: "IX_VcRoleInfo_GuildConfigId",
                table: "VcRoleInfo",
                column: "GuildConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_WaifuInfo_AffinityId",
                table: "WaifuInfo",
                column: "AffinityId");

            migrationBuilder.CreateIndex(
                name: "IX_WaifuInfo_ClaimerId",
                table: "WaifuInfo",
                column: "ClaimerId");

            migrationBuilder.CreateIndex(
                name: "IX_WaifuInfo_Price",
                table: "WaifuInfo",
                column: "Price");

            migrationBuilder.CreateIndex(
                name: "IX_WaifuInfo_WaifuId",
                table: "WaifuInfo",
                column: "WaifuId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WaifuItem_WaifuInfoId",
                table: "WaifuItem",
                column: "WaifuInfoId");

            migrationBuilder.CreateIndex(
                name: "IX_WaifuUpdates_NewId",
                table: "WaifuUpdates",
                column: "NewId");

            migrationBuilder.CreateIndex(
                name: "IX_WaifuUpdates_OldId",
                table: "WaifuUpdates",
                column: "OldId");

            migrationBuilder.CreateIndex(
                name: "IX_WaifuUpdates_UserId",
                table: "WaifuUpdates",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WarningPunishment_GuildConfigId",
                table: "WarningPunishment",
                column: "GuildConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_Warnings_DateAdded",
                table: "Warnings",
                column: "DateAdded");

            migrationBuilder.CreateIndex(
                name: "IX_Warnings_GuildId",
                table: "Warnings",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_Warnings_UserId",
                table: "Warnings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_XpCurrencyReward_XpSettingsId",
                table: "XpCurrencyReward",
                column: "XpSettingsId");

            migrationBuilder.CreateIndex(
                name: "IX_XpRoleReward_XpSettingsId_Level",
                table: "XpRoleReward",
                columns: new[] { "XpSettingsId", "Level" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_XpSettings_GuildConfigId",
                table: "XpSettings",
                column: "GuildConfigId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_XpShopOwnedItem_UserId_ItemType_ItemKey",
                table: "XpShopOwnedItem",
                columns: new[] { "UserId", "ItemType", "ItemKey" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ClubApplicants_Clubs_ClubId",
                table: "ClubApplicants",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ClubApplicants_DiscordUser_UserId",
                table: "ClubApplicants",
                column: "UserId",
                principalTable: "DiscordUser",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ClubBans_Clubs_ClubId",
                table: "ClubBans",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ClubBans_DiscordUser_UserId",
                table: "ClubBans",
                column: "UserId",
                principalTable: "DiscordUser",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Clubs_DiscordUser_OwnerId",
                table: "Clubs",
                column: "OwnerId",
                principalTable: "DiscordUser",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DiscordUser_Clubs_ClubId",
                table: "DiscordUser");

            migrationBuilder.DropTable(
                name: "AntiAltSetting");

            migrationBuilder.DropTable(
                name: "AntiRaidSetting");

            migrationBuilder.DropTable(
                name: "AntiSpamIgnore");

            migrationBuilder.DropTable(
                name: "AutoCommands");

            migrationBuilder.DropTable(
                name: "AutoPublishChannel");

            migrationBuilder.DropTable(
                name: "AutoTranslateUsers");

            migrationBuilder.DropTable(
                name: "BankUsers");

            migrationBuilder.DropTable(
                name: "BanTemplates");

            migrationBuilder.DropTable(
                name: "Blacklist");

            migrationBuilder.DropTable(
                name: "ClubApplicants");

            migrationBuilder.DropTable(
                name: "ClubBans");

            migrationBuilder.DropTable(
                name: "CommandAlias");

            migrationBuilder.DropTable(
                name: "CommandCooldown");

            migrationBuilder.DropTable(
                name: "CurrencyTransactions");

            migrationBuilder.DropTable(
                name: "DelMsgOnCmdChannel");

            migrationBuilder.DropTable(
                name: "DiscordPermOverrides");

            migrationBuilder.DropTable(
                name: "ExcludedItem");

            migrationBuilder.DropTable(
                name: "Expressions");

            migrationBuilder.DropTable(
                name: "FeedSub");

            migrationBuilder.DropTable(
                name: "FilterChannelId");

            migrationBuilder.DropTable(
                name: "FilteredWord");

            migrationBuilder.DropTable(
                name: "FilterLinksChannelId");

            migrationBuilder.DropTable(
                name: "FilterWordsChannelId");

            migrationBuilder.DropTable(
                name: "FollowedStream");

            migrationBuilder.DropTable(
                name: "GamblingStats");

            migrationBuilder.DropTable(
                name: "GCChannelId");

            migrationBuilder.DropTable(
                name: "GroupName");

            migrationBuilder.DropTable(
                name: "IgnoredLogChannels");

            migrationBuilder.DropTable(
                name: "IgnoredVoicePresenceCHannels");

            migrationBuilder.DropTable(
                name: "ImageOnlyChannels");

            migrationBuilder.DropTable(
                name: "MusicPlayerSettings");

            migrationBuilder.DropTable(
                name: "MutedUserId");

            migrationBuilder.DropTable(
                name: "NsfwBlacklistedTags");

            migrationBuilder.DropTable(
                name: "PatronQuotas");

            migrationBuilder.DropTable(
                name: "Patrons");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "PlantedCurrency");

            migrationBuilder.DropTable(
                name: "PlaylistSong");

            migrationBuilder.DropTable(
                name: "PollAnswer");

            migrationBuilder.DropTable(
                name: "PollVote");

            migrationBuilder.DropTable(
                name: "Quotes");

            migrationBuilder.DropTable(
                name: "ReactionRoles");

            migrationBuilder.DropTable(
                name: "Reminders");

            migrationBuilder.DropTable(
                name: "Repeaters");

            migrationBuilder.DropTable(
                name: "RewardedUsers");

            migrationBuilder.DropTable(
                name: "RotatingStatus");

            migrationBuilder.DropTable(
                name: "SelfAssignableRoles");

            migrationBuilder.DropTable(
                name: "ShopEntryItem");

            migrationBuilder.DropTable(
                name: "SlowmodeIgnoredRole");

            migrationBuilder.DropTable(
                name: "SlowmodeIgnoredUser");

            migrationBuilder.DropTable(
                name: "StreamOnlineMessages");

            migrationBuilder.DropTable(
                name: "StreamRoleBlacklistedUser");

            migrationBuilder.DropTable(
                name: "StreamRoleWhitelistedUser");

            migrationBuilder.DropTable(
                name: "UnbanTimer");

            migrationBuilder.DropTable(
                name: "UnmuteTimer");

            migrationBuilder.DropTable(
                name: "UnroleTimer");

            migrationBuilder.DropTable(
                name: "UserXpStats");

            migrationBuilder.DropTable(
                name: "VcRoleInfo");

            migrationBuilder.DropTable(
                name: "WaifuItem");

            migrationBuilder.DropTable(
                name: "WaifuUpdates");

            migrationBuilder.DropTable(
                name: "WarningPunishment");

            migrationBuilder.DropTable(
                name: "Warnings");

            migrationBuilder.DropTable(
                name: "XpCurrencyReward");

            migrationBuilder.DropTable(
                name: "XpRoleReward");

            migrationBuilder.DropTable(
                name: "XpShopOwnedItem");

            migrationBuilder.DropTable(
                name: "AntiSpamSetting");

            migrationBuilder.DropTable(
                name: "AutoTranslateChannels");

            migrationBuilder.DropTable(
                name: "LogSettings");

            migrationBuilder.DropTable(
                name: "MusicPlaylists");

            migrationBuilder.DropTable(
                name: "Poll");

            migrationBuilder.DropTable(
                name: "ShopEntry");

            migrationBuilder.DropTable(
                name: "StreamRoleSettings");

            migrationBuilder.DropTable(
                name: "WaifuInfo");

            migrationBuilder.DropTable(
                name: "XpSettings");

            migrationBuilder.DropTable(
                name: "GuildConfigs");

            migrationBuilder.DropTable(
                name: "Clubs");

            migrationBuilder.DropTable(
                name: "DiscordUser");
        }
    }
}
