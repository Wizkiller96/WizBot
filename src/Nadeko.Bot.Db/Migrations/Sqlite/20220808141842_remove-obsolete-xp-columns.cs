using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NadekoBot.Migrations
{
    public partial class removeobsoletexpcolumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastLevelUp",
                table: "UserXpStats");

            migrationBuilder.DropColumn(
                name: "LastLevelUp",
                table: "DiscordUser");

            migrationBuilder.DropColumn(
                name: "LastXpGain",
                table: "DiscordUser");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastLevelUp",
                table: "UserXpStats",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "datetime('now')");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastLevelUp",
                table: "DiscordUser",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "datetime('now')");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastXpGain",
                table: "DiscordUser",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "datetime('now', '-1 years')");
        }
    }
}
