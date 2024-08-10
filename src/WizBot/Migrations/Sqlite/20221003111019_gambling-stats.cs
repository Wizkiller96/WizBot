﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WizBot.Migrations
{
    public partial class gamblingstats : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GamblingStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Feature = table.Column<string>(type: "TEXT", nullable: true),
                    Bet = table.Column<decimal>(type: "TEXT", nullable: false),
                    PaidOut = table.Column<decimal>(type: "TEXT", nullable: false),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GamblingStats", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GamblingStats_Feature",
                table: "GamblingStats",
                column: "Feature",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GamblingStats");
        }
    }
}
