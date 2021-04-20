using Microsoft.EntityFrameworkCore.Migrations;

namespace WizBot.Migrations
{
    public partial class groupgreets : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "GroupGreets",
                table: "BotConfig",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GroupGreets",
                table: "BotConfig");
        }
    }
}
