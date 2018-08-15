﻿using Microsoft.EntityFrameworkCore.Migrations;

namespace WizBot.Migrations
{
    public partial class vcautodc : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AutoDcFromVc",
                table: "GuildConfigs",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoDcFromVc",
                table: "GuildConfigs");
        }
    }
}
