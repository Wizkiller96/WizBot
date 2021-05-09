using Microsoft.EntityFrameworkCore.Migrations;

namespace WizBot.Migrations
{
    public partial class gamblingsettingsmigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "WaifuItem",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasMigratedGamblingSettings",
                table: "BotConfig",
                nullable: false,
                defaultValue: true);

            migrationBuilder.Sql("UPDATE BotConfig SET HasMigratedGamblingSettings = 0;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "WaifuItem");

            migrationBuilder.DropColumn(
                name: "HasMigratedGamblingSettings",
                table: "BotConfig");
        }
    }
}