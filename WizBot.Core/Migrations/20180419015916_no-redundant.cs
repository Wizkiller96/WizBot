﻿using Microsoft.EntityFrameworkCore.Migrations;

namespace WizBot.Migrations
{
    public partial class noredundant : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "NoRedundant",
                table: "GuildRepeater",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NoRedundant",
                table: "GuildRepeater");
        }
    }
}
