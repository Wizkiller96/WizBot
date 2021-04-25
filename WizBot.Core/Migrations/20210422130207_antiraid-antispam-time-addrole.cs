using Microsoft.EntityFrameworkCore.Migrations;

namespace WizBot.Migrations
{
    public partial class antiraidantispamtimeaddrole : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<ulong>(
                name: "RoleId",
                table: "AntiSpamSetting",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PunishDuration",
                table: "AntiRaidSetting",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("DELETE FROM AntiSpamSetting WHERE MuteTime < 60;");
            migrationBuilder.Sql("UPDATE AntiSpamSetting SET MuteTime=MuteTime / 60;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RoleId",
                table: "AntiSpamSetting");

            migrationBuilder.DropColumn(
                name: "PunishDuration",
                table: "AntiRaidSetting");
        }
    }
}
