using Microsoft.EntityFrameworkCore.Migrations;

namespace WizBot.Migrations
{
    public partial class botsettingsmigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // if this migration is running, it means the user had the database
            // prior to this patch, therefore migraton to .yml is required
            // so the default value is manually changed from true to false
            // but if the user had the database, the snapshot default value
            // (true) will be used
            migrationBuilder.AddColumn<bool>(
                name: "HasMigratedBotSettings",
                table: "BotConfig",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasMigratedBotSettings",
                table: "BotConfig");
        }
    }
}