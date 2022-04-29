using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NadekoBot.Migrations
{
    public partial class bank : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BankUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Balance = table.Column<long>(type: "INTEGER", nullable: false),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankUsers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BankUsers_UserId",
                table: "BankUsers",
                column: "UserId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BankUsers");
        }
    }
}
