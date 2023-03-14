using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NadekoBot.Migrations.PostgreSql
{
    public partial class logthread : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "threadcreatedid",
                table: "logsettings",
                type: "numeric(20,0)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "threaddeletedid",
                table: "logsettings",
                type: "numeric(20,0)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "threadcreatedid",
                table: "logsettings");

            migrationBuilder.DropColumn(
                name: "threaddeletedid",
                table: "logsettings");
        }
    }
}
