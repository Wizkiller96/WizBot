using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NadekoBot.Migrations
{
    public partial class musicautoplay : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // these 2 settings weren't being used for a long time
            migrationBuilder.DropColumn(
                name: "AutoDeleteByeMessages",
                table: "GuildConfigs");

            migrationBuilder.DropColumn(
                name: "AutoDeleteGreetMessages",
                table: "GuildConfigs");

            migrationBuilder.AlterColumn<long>(
                name: "Weight",
                table: "Warnings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1L,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldDefaultValue: 1);

            migrationBuilder.AddColumn<bool>(
                name: "AutoPlay",
                table: "MusicPlayerSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoPlay",
                table: "MusicPlayerSettings");

            migrationBuilder.AlterColumn<int>(
                name: "Weight",
                table: "Warnings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(long),
                oldType: "INTEGER",
                oldDefaultValue: 1L);

            migrationBuilder.AddColumn<bool>(
                name: "AutoDeleteByeMessages",
                table: "GuildConfigs",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AutoDeleteGreetMessages",
                table: "GuildConfigs",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }
    }
}
