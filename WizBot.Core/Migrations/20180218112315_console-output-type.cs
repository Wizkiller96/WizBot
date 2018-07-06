using Microsoft.EntityFrameworkCore.Migrations;

namespace WizBot.Migrations
{
    public partial class consoleoutputtype : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ConsoleOutputType",
                table: "BotConfig",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConsoleOutputType",
                table: "BotConfig");
        }
    }
}
