using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WizBot.Migrations
{
    /// <inheritdoc />
    public partial class guidlconfigcleanup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            MigrationQueries.GuildConfigCleanup(migrationBuilder);
            
            migrationBuilder.DropForeignKey(
                name: "FK_AntiRaidSetting_GuildConfigs_GuildConfigId",
                table: "AntiRaidSetting");

            migrationBuilder.DropForeignKey(
                name: "FK_AntiSpamIgnore_AntiSpamSetting_AntiSpamSettingId",
                table: "AntiSpamIgnore");

            migrationBuilder.DropForeignKey(
                name: "FK_AntiSpamSetting_GuildConfigs_GuildConfigId",
                table: "AntiSpamSetting");

            migrationBuilder.DropForeignKey(
                name: "FK_CommandAlias_GuildConfigs_GuildConfigId",
                table: "CommandAlias");

            migrationBuilder.DropForeignKey(
                name: "FK_CommandCooldown_GuildConfigs_GuildConfigId",
                table: "CommandCooldown");

            migrationBuilder.DropForeignKey(
                name: "FK_DelMsgOnCmdChannel_GuildConfigs_GuildConfigId",
                table: "DelMsgOnCmdChannel");

            migrationBuilder.DropForeignKey(
                name: "FK_ExcludedItem_XpSettings_XpSettingsId",
                table: "ExcludedItem");

            migrationBuilder.DropForeignKey(
                name: "FK_FilterChannelId_GuildConfigs_GuildConfigId",
                table: "FilterChannelId");

            migrationBuilder.DropForeignKey(
                name: "FK_FilteredWord_GuildConfigs_GuildConfigId",
                table: "FilteredWord");

            migrationBuilder.DropForeignKey(
                name: "FK_FilterLinksChannelId_GuildConfigs_GuildConfigId",
                table: "FilterLinksChannelId");

            migrationBuilder.DropForeignKey(
                name: "FK_FilterWordsChannelId_GuildConfigs_GuildConfigId",
                table: "FilterWordsChannelId");

            migrationBuilder.DropForeignKey(
                name: "FK_FollowedStream_GuildConfigs_GuildConfigId",
                table: "FollowedStream");

            migrationBuilder.DropForeignKey(
                name: "FK_GCChannelId_GuildConfigs_GuildConfigId",
                table: "GCChannelId");

            migrationBuilder.DropForeignKey(
                name: "FK_MutedUserId_GuildConfigs_GuildConfigId",
                table: "MutedUserId");

            migrationBuilder.DropForeignKey(
                name: "FK_Permissions_GuildConfigs_GuildConfigId",
                table: "Permissions");

            migrationBuilder.DropForeignKey(
                name: "FK_ShopEntry_GuildConfigs_GuildConfigId",
                table: "ShopEntry");

            migrationBuilder.DropForeignKey(
                name: "FK_ShopEntryItem_ShopEntry_ShopEntryId",
                table: "ShopEntryItem");

            migrationBuilder.DropForeignKey(
                name: "FK_SlowmodeIgnoredRole_GuildConfigs_GuildConfigId",
                table: "SlowmodeIgnoredRole");

            migrationBuilder.DropForeignKey(
                name: "FK_SlowmodeIgnoredUser_GuildConfigs_GuildConfigId",
                table: "SlowmodeIgnoredUser");

            migrationBuilder.DropForeignKey(
                name: "FK_StreamRoleBlacklistedUser_StreamRoleSettings_StreamRoleSettingsId",
                table: "StreamRoleBlacklistedUser");

            migrationBuilder.DropForeignKey(
                name: "FK_StreamRoleWhitelistedUser_StreamRoleSettings_StreamRoleSettingsId",
                table: "StreamRoleWhitelistedUser");

            migrationBuilder.DropForeignKey(
                name: "FK_UnbanTimer_GuildConfigs_GuildConfigId",
                table: "UnbanTimer");

            migrationBuilder.DropForeignKey(
                name: "FK_UnmuteTimer_GuildConfigs_GuildConfigId",
                table: "UnmuteTimer");

            migrationBuilder.DropForeignKey(
                name: "FK_UnroleTimer_GuildConfigs_GuildConfigId",
                table: "UnroleTimer");

            migrationBuilder.DropForeignKey(
                name: "FK_VcRoleInfo_GuildConfigs_GuildConfigId",
                table: "VcRoleInfo");

            migrationBuilder.DropForeignKey(
                name: "FK_WarningPunishment_GuildConfigs_GuildConfigId",
                table: "WarningPunishment");

            migrationBuilder.DropTable(
                name: "IgnoredVoicePresenceCHannels");

            migrationBuilder.AlterColumn<int>(
                name: "StreamRoleSettingsId",
                table: "StreamRoleWhitelistedUser",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "StreamRoleSettingsId",
                table: "StreamRoleBlacklistedUser",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "GuildConfigId",
                table: "DelMsgOnCmdChannel",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AntiRaidSetting_GuildConfigs_GuildConfigId",
                table: "AntiRaidSetting",
                column: "GuildConfigId",
                principalTable: "GuildConfigs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AntiSpamIgnore_AntiSpamSetting_AntiSpamSettingId",
                table: "AntiSpamIgnore",
                column: "AntiSpamSettingId",
                principalTable: "AntiSpamSetting",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AntiSpamSetting_GuildConfigs_GuildConfigId",
                table: "AntiSpamSetting",
                column: "GuildConfigId",
                principalTable: "GuildConfigs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CommandAlias_GuildConfigs_GuildConfigId",
                table: "CommandAlias",
                column: "GuildConfigId",
                principalTable: "GuildConfigs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CommandCooldown_GuildConfigs_GuildConfigId",
                table: "CommandCooldown",
                column: "GuildConfigId",
                principalTable: "GuildConfigs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DelMsgOnCmdChannel_GuildConfigs_GuildConfigId",
                table: "DelMsgOnCmdChannel",
                column: "GuildConfigId",
                principalTable: "GuildConfigs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ExcludedItem_XpSettings_XpSettingsId",
                table: "ExcludedItem",
                column: "XpSettingsId",
                principalTable: "XpSettings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FilterChannelId_GuildConfigs_GuildConfigId",
                table: "FilterChannelId",
                column: "GuildConfigId",
                principalTable: "GuildConfigs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FilteredWord_GuildConfigs_GuildConfigId",
                table: "FilteredWord",
                column: "GuildConfigId",
                principalTable: "GuildConfigs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FilterLinksChannelId_GuildConfigs_GuildConfigId",
                table: "FilterLinksChannelId",
                column: "GuildConfigId",
                principalTable: "GuildConfigs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FilterWordsChannelId_GuildConfigs_GuildConfigId",
                table: "FilterWordsChannelId",
                column: "GuildConfigId",
                principalTable: "GuildConfigs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FollowedStream_GuildConfigs_GuildConfigId",
                table: "FollowedStream",
                column: "GuildConfigId",
                principalTable: "GuildConfigs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GCChannelId_GuildConfigs_GuildConfigId",
                table: "GCChannelId",
                column: "GuildConfigId",
                principalTable: "GuildConfigs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MutedUserId_GuildConfigs_GuildConfigId",
                table: "MutedUserId",
                column: "GuildConfigId",
                principalTable: "GuildConfigs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Permissions_GuildConfigs_GuildConfigId",
                table: "Permissions",
                column: "GuildConfigId",
                principalTable: "GuildConfigs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ShopEntry_GuildConfigs_GuildConfigId",
                table: "ShopEntry",
                column: "GuildConfigId",
                principalTable: "GuildConfigs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ShopEntryItem_ShopEntry_ShopEntryId",
                table: "ShopEntryItem",
                column: "ShopEntryId",
                principalTable: "ShopEntry",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SlowmodeIgnoredRole_GuildConfigs_GuildConfigId",
                table: "SlowmodeIgnoredRole",
                column: "GuildConfigId",
                principalTable: "GuildConfigs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SlowmodeIgnoredUser_GuildConfigs_GuildConfigId",
                table: "SlowmodeIgnoredUser",
                column: "GuildConfigId",
                principalTable: "GuildConfigs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StreamRoleBlacklistedUser_StreamRoleSettings_StreamRoleSettingsId",
                table: "StreamRoleBlacklistedUser",
                column: "StreamRoleSettingsId",
                principalTable: "StreamRoleSettings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StreamRoleWhitelistedUser_StreamRoleSettings_StreamRoleSettingsId",
                table: "StreamRoleWhitelistedUser",
                column: "StreamRoleSettingsId",
                principalTable: "StreamRoleSettings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UnbanTimer_GuildConfigs_GuildConfigId",
                table: "UnbanTimer",
                column: "GuildConfigId",
                principalTable: "GuildConfigs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UnmuteTimer_GuildConfigs_GuildConfigId",
                table: "UnmuteTimer",
                column: "GuildConfigId",
                principalTable: "GuildConfigs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UnroleTimer_GuildConfigs_GuildConfigId",
                table: "UnroleTimer",
                column: "GuildConfigId",
                principalTable: "GuildConfigs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VcRoleInfo_GuildConfigs_GuildConfigId",
                table: "VcRoleInfo",
                column: "GuildConfigId",
                principalTable: "GuildConfigs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WarningPunishment_GuildConfigs_GuildConfigId",
                table: "WarningPunishment",
                column: "GuildConfigId",
                principalTable: "GuildConfigs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AntiRaidSetting_GuildConfigs_GuildConfigId",
                table: "AntiRaidSetting");

            migrationBuilder.DropForeignKey(
                name: "FK_AntiSpamIgnore_AntiSpamSetting_AntiSpamSettingId",
                table: "AntiSpamIgnore");

            migrationBuilder.DropForeignKey(
                name: "FK_AntiSpamSetting_GuildConfigs_GuildConfigId",
                table: "AntiSpamSetting");

            migrationBuilder.DropForeignKey(
                name: "FK_CommandAlias_GuildConfigs_GuildConfigId",
                table: "CommandAlias");

            migrationBuilder.DropForeignKey(
                name: "FK_CommandCooldown_GuildConfigs_GuildConfigId",
                table: "CommandCooldown");

            migrationBuilder.DropForeignKey(
                name: "FK_DelMsgOnCmdChannel_GuildConfigs_GuildConfigId",
                table: "DelMsgOnCmdChannel");

            migrationBuilder.DropForeignKey(
                name: "FK_ExcludedItem_XpSettings_XpSettingsId",
                table: "ExcludedItem");

            migrationBuilder.DropForeignKey(
                name: "FK_FilterChannelId_GuildConfigs_GuildConfigId",
                table: "FilterChannelId");

            migrationBuilder.DropForeignKey(
                name: "FK_FilteredWord_GuildConfigs_GuildConfigId",
                table: "FilteredWord");

            migrationBuilder.DropForeignKey(
                name: "FK_FilterLinksChannelId_GuildConfigs_GuildConfigId",
                table: "FilterLinksChannelId");

            migrationBuilder.DropForeignKey(
                name: "FK_FilterWordsChannelId_GuildConfigs_GuildConfigId",
                table: "FilterWordsChannelId");

            migrationBuilder.DropForeignKey(
                name: "FK_FollowedStream_GuildConfigs_GuildConfigId",
                table: "FollowedStream");

            migrationBuilder.DropForeignKey(
                name: "FK_GCChannelId_GuildConfigs_GuildConfigId",
                table: "GCChannelId");

            migrationBuilder.DropForeignKey(
                name: "FK_MutedUserId_GuildConfigs_GuildConfigId",
                table: "MutedUserId");

            migrationBuilder.DropForeignKey(
                name: "FK_Permissions_GuildConfigs_GuildConfigId",
                table: "Permissions");

            migrationBuilder.DropForeignKey(
                name: "FK_ShopEntry_GuildConfigs_GuildConfigId",
                table: "ShopEntry");

            migrationBuilder.DropForeignKey(
                name: "FK_ShopEntryItem_ShopEntry_ShopEntryId",
                table: "ShopEntryItem");

            migrationBuilder.DropForeignKey(
                name: "FK_SlowmodeIgnoredRole_GuildConfigs_GuildConfigId",
                table: "SlowmodeIgnoredRole");

            migrationBuilder.DropForeignKey(
                name: "FK_SlowmodeIgnoredUser_GuildConfigs_GuildConfigId",
                table: "SlowmodeIgnoredUser");

            migrationBuilder.DropForeignKey(
                name: "FK_StreamRoleBlacklistedUser_StreamRoleSettings_StreamRoleSettingsId",
                table: "StreamRoleBlacklistedUser");

            migrationBuilder.DropForeignKey(
                name: "FK_StreamRoleWhitelistedUser_StreamRoleSettings_StreamRoleSettingsId",
                table: "StreamRoleWhitelistedUser");

            migrationBuilder.DropForeignKey(
                name: "FK_UnbanTimer_GuildConfigs_GuildConfigId",
                table: "UnbanTimer");

            migrationBuilder.DropForeignKey(
                name: "FK_UnmuteTimer_GuildConfigs_GuildConfigId",
                table: "UnmuteTimer");

            migrationBuilder.DropForeignKey(
                name: "FK_UnroleTimer_GuildConfigs_GuildConfigId",
                table: "UnroleTimer");

            migrationBuilder.DropForeignKey(
                name: "FK_VcRoleInfo_GuildConfigs_GuildConfigId",
                table: "VcRoleInfo");

            migrationBuilder.DropForeignKey(
                name: "FK_WarningPunishment_GuildConfigs_GuildConfigId",
                table: "WarningPunishment");

            migrationBuilder.AlterColumn<int>(
                name: "StreamRoleSettingsId",
                table: "StreamRoleWhitelistedUser",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<int>(
                name: "StreamRoleSettingsId",
                table: "StreamRoleBlacklistedUser",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<int>(
                name: "GuildConfigId",
                table: "DelMsgOnCmdChannel",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

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

            migrationBuilder.CreateIndex(
                name: "IX_IgnoredVoicePresenceCHannels_LogSettingId",
                table: "IgnoredVoicePresenceCHannels",
                column: "LogSettingId");

            migrationBuilder.AddForeignKey(
                name: "FK_AntiRaidSetting_GuildConfigs_GuildConfigId",
                table: "AntiRaidSetting",
                column: "GuildConfigId",
                principalTable: "GuildConfigs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AntiSpamIgnore_AntiSpamSetting_AntiSpamSettingId",
                table: "AntiSpamIgnore",
                column: "AntiSpamSettingId",
                principalTable: "AntiSpamSetting",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AntiSpamSetting_GuildConfigs_GuildConfigId",
                table: "AntiSpamSetting",
                column: "GuildConfigId",
                principalTable: "GuildConfigs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CommandAlias_GuildConfigs_GuildConfigId",
                table: "CommandAlias",
                column: "GuildConfigId",
                principalTable: "GuildConfigs",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CommandCooldown_GuildConfigs_GuildConfigId",
                table: "CommandCooldown",
                column: "GuildConfigId",
                principalTable: "GuildConfigs",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DelMsgOnCmdChannel_GuildConfigs_GuildConfigId",
                table: "DelMsgOnCmdChannel",
                column: "GuildConfigId",
                principalTable: "GuildConfigs",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ExcludedItem_XpSettings_XpSettingsId",
                table: "ExcludedItem",
                column: "XpSettingsId",
                principalTable: "XpSettings",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FilterChannelId_GuildConfigs_GuildConfigId",
                table: "FilterChannelId",
                column: "GuildConfigId",
                principalTable: "GuildConfigs",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FilteredWord_GuildConfigs_GuildConfigId",
                table: "FilteredWord",
                column: "GuildConfigId",
                principalTable: "GuildConfigs",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FilterLinksChannelId_GuildConfigs_GuildConfigId",
                table: "FilterLinksChannelId",
                column: "GuildConfigId",
                principalTable: "GuildConfigs",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FilterWordsChannelId_GuildConfigs_GuildConfigId",
                table: "FilterWordsChannelId",
                column: "GuildConfigId",
                principalTable: "GuildConfigs",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FollowedStream_GuildConfigs_GuildConfigId",
                table: "FollowedStream",
                column: "GuildConfigId",
                principalTable: "GuildConfigs",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_GCChannelId_GuildConfigs_GuildConfigId",
                table: "GCChannelId",
                column: "GuildConfigId",
                principalTable: "GuildConfigs",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MutedUserId_GuildConfigs_GuildConfigId",
                table: "MutedUserId",
                column: "GuildConfigId",
                principalTable: "GuildConfigs",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Permissions_GuildConfigs_GuildConfigId",
                table: "Permissions",
                column: "GuildConfigId",
                principalTable: "GuildConfigs",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ShopEntry_GuildConfigs_GuildConfigId",
                table: "ShopEntry",
                column: "GuildConfigId",
                principalTable: "GuildConfigs",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ShopEntryItem_ShopEntry_ShopEntryId",
                table: "ShopEntryItem",
                column: "ShopEntryId",
                principalTable: "ShopEntry",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SlowmodeIgnoredRole_GuildConfigs_GuildConfigId",
                table: "SlowmodeIgnoredRole",
                column: "GuildConfigId",
                principalTable: "GuildConfigs",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SlowmodeIgnoredUser_GuildConfigs_GuildConfigId",
                table: "SlowmodeIgnoredUser",
                column: "GuildConfigId",
                principalTable: "GuildConfigs",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StreamRoleBlacklistedUser_StreamRoleSettings_StreamRoleSettingsId",
                table: "StreamRoleBlacklistedUser",
                column: "StreamRoleSettingsId",
                principalTable: "StreamRoleSettings",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StreamRoleWhitelistedUser_StreamRoleSettings_StreamRoleSettingsId",
                table: "StreamRoleWhitelistedUser",
                column: "StreamRoleSettingsId",
                principalTable: "StreamRoleSettings",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UnbanTimer_GuildConfigs_GuildConfigId",
                table: "UnbanTimer",
                column: "GuildConfigId",
                principalTable: "GuildConfigs",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UnmuteTimer_GuildConfigs_GuildConfigId",
                table: "UnmuteTimer",
                column: "GuildConfigId",
                principalTable: "GuildConfigs",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UnroleTimer_GuildConfigs_GuildConfigId",
                table: "UnroleTimer",
                column: "GuildConfigId",
                principalTable: "GuildConfigs",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_VcRoleInfo_GuildConfigs_GuildConfigId",
                table: "VcRoleInfo",
                column: "GuildConfigId",
                principalTable: "GuildConfigs",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_WarningPunishment_GuildConfigs_GuildConfigId",
                table: "WarningPunishment",
                column: "GuildConfigId",
                principalTable: "GuildConfigs",
                principalColumn: "Id");
        }
    }
}
