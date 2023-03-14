using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NadekoBot.Migrations
{
    public partial class logthread : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<ulong>(
                name: "ThreadCreatedId",
                table: "LogSettings",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<ulong>(
                name: "ThreadDeletedId",
                table: "LogSettings",
                type: "INTEGER",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ThreadCreatedId",
                table: "LogSettings");

            migrationBuilder.DropColumn(
                name: "ThreadDeletedId",
                table: "LogSettings");
        }
    }
}
