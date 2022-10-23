﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WizBot.Migrations.Mysql
{
    public partial class toggleglobalexpressions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "disableglobalexpressions",
                table: "guildconfigs",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "disableglobalexpressions",
                table: "guildconfigs");
        }
    }
}