using Microsoft.EntityFrameworkCore.Migrations;

namespace NadekoBot.Migrations
{
    public partial class logsettingsindependence : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<ulong>(
                name: "GuildId",
                table: "LogSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0ul);

            migrationBuilder.Sql(
                @"UPDATE LogSettings SET GuildId = (SELECT GuildId FROM GuildConfigs WHERE LogSettingId = LogSettings.Id);
                DELETE FROM LogSettings WHERE GuildId = 0;");
            
            migrationBuilder.DropForeignKey(
                name: "FK_GuildConfigs_LogSettings_LogSettingId",
                table: "GuildConfigs");

            migrationBuilder.DropIndex(
                name: "IX_GuildConfigs_LogSettingId",
                table: "GuildConfigs");

            migrationBuilder.DropColumn(
                name: "LogSettingId",
                table: "GuildConfigs");

            migrationBuilder.CreateIndex(
                name: "IX_LogSettings_GuildId",
                table: "LogSettings",
                column: "GuildId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LogSettings_GuildId",
                table: "LogSettings");

            migrationBuilder.DropColumn(
                name: "GuildId",
                table: "LogSettings");

            migrationBuilder.AddColumn<int>(
                name: "LogSettingId",
                table: "GuildConfigs",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_GuildConfigs_LogSettingId",
                table: "GuildConfigs",
                column: "LogSettingId");

            migrationBuilder.AddForeignKey(
                name: "FK_GuildConfigs_LogSettings_LogSettingId",
                table: "GuildConfigs",
                column: "LogSettingId",
                principalTable: "LogSettings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
