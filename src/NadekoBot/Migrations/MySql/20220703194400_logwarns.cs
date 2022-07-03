using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NadekoBot.Migrations.Mysql
{
    public partial class logwarns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<ulong>(
                name: "logwarnsid",
                table: "logsettings",
                type: "bigint unsigned",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "logwarnsid",
                table: "logsettings");
        }
    }
}
