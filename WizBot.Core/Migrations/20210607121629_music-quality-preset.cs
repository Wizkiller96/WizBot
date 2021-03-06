﻿using Microsoft.EntityFrameworkCore.Migrations;

namespace WizBot.Migrations
{
    public partial class musicqualitypreset : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "QualityPreset",
                table: "MusicPlayerSettings",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QualityPreset",
                table: "MusicPlayerSettings");
        }
    }
}
