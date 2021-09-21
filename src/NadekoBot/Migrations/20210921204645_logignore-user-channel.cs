using Microsoft.EntityFrameworkCore.Migrations;

namespace NadekoBot.Migrations
{
    public partial class logignoreuserchannel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM IgnoredLogChannels WHERE LogSettingId is NULL");
            
            migrationBuilder.DropForeignKey(
                name: "FK_IgnoredLogChannels_LogSettings_LogSettingId",
                table: "IgnoredLogChannels");

            migrationBuilder.DropIndex(
                name: "IX_IgnoredLogChannels_LogSettingId",
                table: "IgnoredLogChannels");

            migrationBuilder.RenameColumn(
                name: "ChannelId",
                table: "IgnoredLogChannels",
                newName: "LogItemId");

            migrationBuilder.AlterColumn<int>(
                name: "LogSettingId",
                table: "IgnoredLogChannels",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ItemType",
                table: "IgnoredLogChannels",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_IgnoredLogChannels_LogSettingId_LogItemId_ItemType",
                table: "IgnoredLogChannels",
                columns: new[] { "LogSettingId", "LogItemId", "ItemType" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_IgnoredLogChannels_LogSettings_LogSettingId",
                table: "IgnoredLogChannels",
                column: "LogSettingId",
                principalTable: "LogSettings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IgnoredLogChannels_LogSettings_LogSettingId",
                table: "IgnoredLogChannels");

            migrationBuilder.DropIndex(
                name: "IX_IgnoredLogChannels_LogSettingId_LogItemId_ItemType",
                table: "IgnoredLogChannels");

            migrationBuilder.DropColumn(
                name: "ItemType",
                table: "IgnoredLogChannels");

            migrationBuilder.RenameColumn(
                name: "LogItemId",
                table: "IgnoredLogChannels",
                newName: "ChannelId");

            migrationBuilder.AlterColumn<int>(
                name: "LogSettingId",
                table: "IgnoredLogChannels",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.CreateIndex(
                name: "IX_IgnoredLogChannels_LogSettingId",
                table: "IgnoredLogChannels",
                column: "LogSettingId");

            migrationBuilder.AddForeignKey(
                name: "FK_IgnoredLogChannels_LogSettings_LogSettingId",
                table: "IgnoredLogChannels",
                column: "LogSettingId",
                principalTable: "LogSettings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
