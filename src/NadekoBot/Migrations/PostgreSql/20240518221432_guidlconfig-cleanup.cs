using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NadekoBot.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class guidlconfigcleanup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_antiraidsetting_guildconfigs_guildconfigid",
                table: "antiraidsetting");

            migrationBuilder.DropForeignKey(
                name: "fk_antispamignore_antispamsetting_antispamsettingid",
                table: "antispamignore");

            migrationBuilder.DropForeignKey(
                name: "fk_antispamsetting_guildconfigs_guildconfigid",
                table: "antispamsetting");

            migrationBuilder.DropForeignKey(
                name: "fk_commandalias_guildconfigs_guildconfigid",
                table: "commandalias");

            migrationBuilder.DropForeignKey(
                name: "fk_commandcooldown_guildconfigs_guildconfigid",
                table: "commandcooldown");

            migrationBuilder.DropForeignKey(
                name: "fk_delmsgoncmdchannel_guildconfigs_guildconfigid",
                table: "delmsgoncmdchannel");

            migrationBuilder.DropForeignKey(
                name: "fk_excludeditem_xpsettings_xpsettingsid",
                table: "excludeditem");

            migrationBuilder.DropForeignKey(
                name: "fk_filterchannelid_guildconfigs_guildconfigid",
                table: "filterchannelid");

            migrationBuilder.DropForeignKey(
                name: "fk_filteredword_guildconfigs_guildconfigid",
                table: "filteredword");

            migrationBuilder.DropForeignKey(
                name: "fk_filterlinkschannelid_guildconfigs_guildconfigid",
                table: "filterlinkschannelid");

            migrationBuilder.DropForeignKey(
                name: "fk_filterwordschannelid_guildconfigs_guildconfigid",
                table: "filterwordschannelid");

            migrationBuilder.DropForeignKey(
                name: "fk_followedstream_guildconfigs_guildconfigid",
                table: "followedstream");

            migrationBuilder.DropForeignKey(
                name: "fk_gcchannelid_guildconfigs_guildconfigid",
                table: "gcchannelid");

            migrationBuilder.DropForeignKey(
                name: "fk_muteduserid_guildconfigs_guildconfigid",
                table: "muteduserid");

            migrationBuilder.DropForeignKey(
                name: "fk_permissions_guildconfigs_guildconfigid",
                table: "permissions");

            migrationBuilder.DropForeignKey(
                name: "fk_shopentry_guildconfigs_guildconfigid",
                table: "shopentry");

            migrationBuilder.DropForeignKey(
                name: "fk_shopentryitem_shopentry_shopentryid",
                table: "shopentryitem");

            migrationBuilder.DropForeignKey(
                name: "fk_slowmodeignoredrole_guildconfigs_guildconfigid",
                table: "slowmodeignoredrole");

            migrationBuilder.DropForeignKey(
                name: "fk_slowmodeignoreduser_guildconfigs_guildconfigid",
                table: "slowmodeignoreduser");

            migrationBuilder.DropForeignKey(
                name: "fk_streamroleblacklisteduser_streamrolesettings_streamrolesett~",
                table: "streamroleblacklisteduser");

            migrationBuilder.DropForeignKey(
                name: "fk_streamrolewhitelisteduser_streamrolesettings_streamrolesett~",
                table: "streamrolewhitelisteduser");

            migrationBuilder.DropForeignKey(
                name: "fk_unbantimer_guildconfigs_guildconfigid",
                table: "unbantimer");

            migrationBuilder.DropForeignKey(
                name: "fk_unmutetimer_guildconfigs_guildconfigid",
                table: "unmutetimer");

            migrationBuilder.DropForeignKey(
                name: "fk_unroletimer_guildconfigs_guildconfigid",
                table: "unroletimer");

            migrationBuilder.DropForeignKey(
                name: "fk_vcroleinfo_guildconfigs_guildconfigid",
                table: "vcroleinfo");

            migrationBuilder.DropForeignKey(
                name: "fk_warningpunishment_guildconfigs_guildconfigid",
                table: "warningpunishment");

            migrationBuilder.DropTable(
                name: "ignoredvoicepresencechannels");

            migrationBuilder.AlterColumn<int>(
                name: "streamrolesettingsid",
                table: "streamrolewhitelisteduser",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "streamrolesettingsid",
                table: "streamroleblacklisteduser",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "guildconfigid",
                table: "delmsgoncmdchannel",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "fk_antiraidsetting_guildconfigs_guildconfigid",
                table: "antiraidsetting",
                column: "guildconfigid",
                principalTable: "guildconfigs",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_antispamignore_antispamsetting_antispamsettingid",
                table: "antispamignore",
                column: "antispamsettingid",
                principalTable: "antispamsetting",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_antispamsetting_guildconfigs_guildconfigid",
                table: "antispamsetting",
                column: "guildconfigid",
                principalTable: "guildconfigs",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_commandalias_guildconfigs_guildconfigid",
                table: "commandalias",
                column: "guildconfigid",
                principalTable: "guildconfigs",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_commandcooldown_guildconfigs_guildconfigid",
                table: "commandcooldown",
                column: "guildconfigid",
                principalTable: "guildconfigs",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_delmsgoncmdchannel_guildconfigs_guildconfigid",
                table: "delmsgoncmdchannel",
                column: "guildconfigid",
                principalTable: "guildconfigs",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_excludeditem_xpsettings_xpsettingsid",
                table: "excludeditem",
                column: "xpsettingsid",
                principalTable: "xpsettings",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_filterchannelid_guildconfigs_guildconfigid",
                table: "filterchannelid",
                column: "guildconfigid",
                principalTable: "guildconfigs",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_filteredword_guildconfigs_guildconfigid",
                table: "filteredword",
                column: "guildconfigid",
                principalTable: "guildconfigs",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_filterlinkschannelid_guildconfigs_guildconfigid",
                table: "filterlinkschannelid",
                column: "guildconfigid",
                principalTable: "guildconfigs",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_filterwordschannelid_guildconfigs_guildconfigid",
                table: "filterwordschannelid",
                column: "guildconfigid",
                principalTable: "guildconfigs",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_followedstream_guildconfigs_guildconfigid",
                table: "followedstream",
                column: "guildconfigid",
                principalTable: "guildconfigs",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_gcchannelid_guildconfigs_guildconfigid",
                table: "gcchannelid",
                column: "guildconfigid",
                principalTable: "guildconfigs",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_muteduserid_guildconfigs_guildconfigid",
                table: "muteduserid",
                column: "guildconfigid",
                principalTable: "guildconfigs",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_permissions_guildconfigs_guildconfigid",
                table: "permissions",
                column: "guildconfigid",
                principalTable: "guildconfigs",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_shopentry_guildconfigs_guildconfigid",
                table: "shopentry",
                column: "guildconfigid",
                principalTable: "guildconfigs",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_shopentryitem_shopentry_shopentryid",
                table: "shopentryitem",
                column: "shopentryid",
                principalTable: "shopentry",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_slowmodeignoredrole_guildconfigs_guildconfigid",
                table: "slowmodeignoredrole",
                column: "guildconfigid",
                principalTable: "guildconfigs",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_slowmodeignoreduser_guildconfigs_guildconfigid",
                table: "slowmodeignoreduser",
                column: "guildconfigid",
                principalTable: "guildconfigs",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_streamroleblacklisteduser_streamrolesettings_streamrolesett~",
                table: "streamroleblacklisteduser",
                column: "streamrolesettingsid",
                principalTable: "streamrolesettings",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_streamrolewhitelisteduser_streamrolesettings_streamrolesett~",
                table: "streamrolewhitelisteduser",
                column: "streamrolesettingsid",
                principalTable: "streamrolesettings",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_unbantimer_guildconfigs_guildconfigid",
                table: "unbantimer",
                column: "guildconfigid",
                principalTable: "guildconfigs",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_unmutetimer_guildconfigs_guildconfigid",
                table: "unmutetimer",
                column: "guildconfigid",
                principalTable: "guildconfigs",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_unroletimer_guildconfigs_guildconfigid",
                table: "unroletimer",
                column: "guildconfigid",
                principalTable: "guildconfigs",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_vcroleinfo_guildconfigs_guildconfigid",
                table: "vcroleinfo",
                column: "guildconfigid",
                principalTable: "guildconfigs",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_warningpunishment_guildconfigs_guildconfigid",
                table: "warningpunishment",
                column: "guildconfigid",
                principalTable: "guildconfigs",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_antiraidsetting_guildconfigs_guildconfigid",
                table: "antiraidsetting");

            migrationBuilder.DropForeignKey(
                name: "fk_antispamignore_antispamsetting_antispamsettingid",
                table: "antispamignore");

            migrationBuilder.DropForeignKey(
                name: "fk_antispamsetting_guildconfigs_guildconfigid",
                table: "antispamsetting");

            migrationBuilder.DropForeignKey(
                name: "fk_commandalias_guildconfigs_guildconfigid",
                table: "commandalias");

            migrationBuilder.DropForeignKey(
                name: "fk_commandcooldown_guildconfigs_guildconfigid",
                table: "commandcooldown");

            migrationBuilder.DropForeignKey(
                name: "fk_delmsgoncmdchannel_guildconfigs_guildconfigid",
                table: "delmsgoncmdchannel");

            migrationBuilder.DropForeignKey(
                name: "fk_excludeditem_xpsettings_xpsettingsid",
                table: "excludeditem");

            migrationBuilder.DropForeignKey(
                name: "fk_filterchannelid_guildconfigs_guildconfigid",
                table: "filterchannelid");

            migrationBuilder.DropForeignKey(
                name: "fk_filteredword_guildconfigs_guildconfigid",
                table: "filteredword");

            migrationBuilder.DropForeignKey(
                name: "fk_filterlinkschannelid_guildconfigs_guildconfigid",
                table: "filterlinkschannelid");

            migrationBuilder.DropForeignKey(
                name: "fk_filterwordschannelid_guildconfigs_guildconfigid",
                table: "filterwordschannelid");

            migrationBuilder.DropForeignKey(
                name: "fk_followedstream_guildconfigs_guildconfigid",
                table: "followedstream");

            migrationBuilder.DropForeignKey(
                name: "fk_gcchannelid_guildconfigs_guildconfigid",
                table: "gcchannelid");

            migrationBuilder.DropForeignKey(
                name: "fk_muteduserid_guildconfigs_guildconfigid",
                table: "muteduserid");

            migrationBuilder.DropForeignKey(
                name: "fk_permissions_guildconfigs_guildconfigid",
                table: "permissions");

            migrationBuilder.DropForeignKey(
                name: "fk_shopentry_guildconfigs_guildconfigid",
                table: "shopentry");

            migrationBuilder.DropForeignKey(
                name: "fk_shopentryitem_shopentry_shopentryid",
                table: "shopentryitem");

            migrationBuilder.DropForeignKey(
                name: "fk_slowmodeignoredrole_guildconfigs_guildconfigid",
                table: "slowmodeignoredrole");

            migrationBuilder.DropForeignKey(
                name: "fk_slowmodeignoreduser_guildconfigs_guildconfigid",
                table: "slowmodeignoreduser");

            migrationBuilder.DropForeignKey(
                name: "fk_streamroleblacklisteduser_streamrolesettings_streamrolesett~",
                table: "streamroleblacklisteduser");

            migrationBuilder.DropForeignKey(
                name: "fk_streamrolewhitelisteduser_streamrolesettings_streamrolesett~",
                table: "streamrolewhitelisteduser");

            migrationBuilder.DropForeignKey(
                name: "fk_unbantimer_guildconfigs_guildconfigid",
                table: "unbantimer");

            migrationBuilder.DropForeignKey(
                name: "fk_unmutetimer_guildconfigs_guildconfigid",
                table: "unmutetimer");

            migrationBuilder.DropForeignKey(
                name: "fk_unroletimer_guildconfigs_guildconfigid",
                table: "unroletimer");

            migrationBuilder.DropForeignKey(
                name: "fk_vcroleinfo_guildconfigs_guildconfigid",
                table: "vcroleinfo");

            migrationBuilder.DropForeignKey(
                name: "fk_warningpunishment_guildconfigs_guildconfigid",
                table: "warningpunishment");

            migrationBuilder.AlterColumn<int>(
                name: "streamrolesettingsid",
                table: "streamrolewhitelisteduser",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "streamrolesettingsid",
                table: "streamroleblacklisteduser",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "guildconfigid",
                table: "delmsgoncmdchannel",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateTable(
                name: "ignoredvoicepresencechannels",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    logsettingid = table.Column<int>(type: "integer", nullable: true),
                    channelid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    dateadded = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ignoredvoicepresencechannels", x => x.id);
                    table.ForeignKey(
                        name: "fk_ignoredvoicepresencechannels_logsettings_logsettingid",
                        column: x => x.logsettingid,
                        principalTable: "logsettings",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_ignoredvoicepresencechannels_logsettingid",
                table: "ignoredvoicepresencechannels",
                column: "logsettingid");

            migrationBuilder.AddForeignKey(
                name: "fk_antiraidsetting_guildconfigs_guildconfigid",
                table: "antiraidsetting",
                column: "guildconfigid",
                principalTable: "guildconfigs",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_antispamignore_antispamsetting_antispamsettingid",
                table: "antispamignore",
                column: "antispamsettingid",
                principalTable: "antispamsetting",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_antispamsetting_guildconfigs_guildconfigid",
                table: "antispamsetting",
                column: "guildconfigid",
                principalTable: "guildconfigs",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_commandalias_guildconfigs_guildconfigid",
                table: "commandalias",
                column: "guildconfigid",
                principalTable: "guildconfigs",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_commandcooldown_guildconfigs_guildconfigid",
                table: "commandcooldown",
                column: "guildconfigid",
                principalTable: "guildconfigs",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_delmsgoncmdchannel_guildconfigs_guildconfigid",
                table: "delmsgoncmdchannel",
                column: "guildconfigid",
                principalTable: "guildconfigs",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_excludeditem_xpsettings_xpsettingsid",
                table: "excludeditem",
                column: "xpsettingsid",
                principalTable: "xpsettings",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_filterchannelid_guildconfigs_guildconfigid",
                table: "filterchannelid",
                column: "guildconfigid",
                principalTable: "guildconfigs",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_filteredword_guildconfigs_guildconfigid",
                table: "filteredword",
                column: "guildconfigid",
                principalTable: "guildconfigs",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_filterlinkschannelid_guildconfigs_guildconfigid",
                table: "filterlinkschannelid",
                column: "guildconfigid",
                principalTable: "guildconfigs",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_filterwordschannelid_guildconfigs_guildconfigid",
                table: "filterwordschannelid",
                column: "guildconfigid",
                principalTable: "guildconfigs",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_followedstream_guildconfigs_guildconfigid",
                table: "followedstream",
                column: "guildconfigid",
                principalTable: "guildconfigs",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_gcchannelid_guildconfigs_guildconfigid",
                table: "gcchannelid",
                column: "guildconfigid",
                principalTable: "guildconfigs",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_muteduserid_guildconfigs_guildconfigid",
                table: "muteduserid",
                column: "guildconfigid",
                principalTable: "guildconfigs",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_permissions_guildconfigs_guildconfigid",
                table: "permissions",
                column: "guildconfigid",
                principalTable: "guildconfigs",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_shopentry_guildconfigs_guildconfigid",
                table: "shopentry",
                column: "guildconfigid",
                principalTable: "guildconfigs",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_shopentryitem_shopentry_shopentryid",
                table: "shopentryitem",
                column: "shopentryid",
                principalTable: "shopentry",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_slowmodeignoredrole_guildconfigs_guildconfigid",
                table: "slowmodeignoredrole",
                column: "guildconfigid",
                principalTable: "guildconfigs",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_slowmodeignoreduser_guildconfigs_guildconfigid",
                table: "slowmodeignoreduser",
                column: "guildconfigid",
                principalTable: "guildconfigs",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_streamroleblacklisteduser_streamrolesettings_streamrolesett~",
                table: "streamroleblacklisteduser",
                column: "streamrolesettingsid",
                principalTable: "streamrolesettings",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_streamrolewhitelisteduser_streamrolesettings_streamrolesett~",
                table: "streamrolewhitelisteduser",
                column: "streamrolesettingsid",
                principalTable: "streamrolesettings",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_unbantimer_guildconfigs_guildconfigid",
                table: "unbantimer",
                column: "guildconfigid",
                principalTable: "guildconfigs",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_unmutetimer_guildconfigs_guildconfigid",
                table: "unmutetimer",
                column: "guildconfigid",
                principalTable: "guildconfigs",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_unroletimer_guildconfigs_guildconfigid",
                table: "unroletimer",
                column: "guildconfigid",
                principalTable: "guildconfigs",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_vcroleinfo_guildconfigs_guildconfigid",
                table: "vcroleinfo",
                column: "guildconfigid",
                principalTable: "guildconfigs",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_warningpunishment_guildconfigs_guildconfigid",
                table: "warningpunishment",
                column: "guildconfigid",
                principalTable: "guildconfigs",
                principalColumn: "id");
        }
    }
}
