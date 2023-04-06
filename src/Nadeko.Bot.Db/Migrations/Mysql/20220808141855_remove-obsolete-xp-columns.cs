using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NadekoBot.Migrations.Mysql
{
    public partial class removeobsoletexpcolumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "lastlevelup",
                table: "userxpstats");

            migrationBuilder.DropColumn(
                name: "lastlevelup",
                table: "discorduser");

            migrationBuilder.DropColumn(
                name: "lastxpgain",
                table: "discorduser");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "lastlevelup",
                table: "userxpstats",
                type: "datetime(6)",
                nullable: false,
                defaultValueSql: "(UTC_TIMESTAMP)");

            migrationBuilder.AddColumn<DateTime>(
                name: "lastlevelup",
                table: "discorduser",
                type: "datetime(6)",
                nullable: false,
                defaultValueSql: "(UTC_TIMESTAMP)");

            migrationBuilder.AddColumn<DateTime>(
                name: "lastxpgain",
                table: "discorduser",
                type: "datetime(6)",
                nullable: false,
                defaultValueSql: "(UTC_TIMESTAMP - INTERVAL 1 year)");
        }
    }
}
