﻿using Microsoft.EntityFrameworkCore.Migrations;

namespace WizBot.Migrations
{
    public partial class waifuresetmultiplier : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DivorcePriceMultiplier",
                table: "BotConfig",
                nullable: false,
                defaultValue: 150);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DivorcePriceMultiplier",
                table: "BotConfig");
        }
    }
}
