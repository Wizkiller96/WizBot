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
