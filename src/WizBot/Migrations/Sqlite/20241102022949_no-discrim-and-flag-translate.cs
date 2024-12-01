using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WizBot.Migrations
{
    /// <inheritdoc />
    public partial class nodiscrimandflagtranslate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Discriminator",
                table: "DiscordUser");

            migrationBuilder.CreateTable(
                name: "FlagTranslateChannel",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                              .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    ChannelId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlagTranslateChannel", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FlagTranslateChannel_GuildId_ChannelId",
                table: "FlagTranslateChannel",
                columns: new[] { "GuildId", "ChannelId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FlagTranslateChannel");

            migrationBuilder.AddColumn<string>(
                name: "Discriminator",
                table: "DiscordUser",
                type: "TEXT",
                nullable: true);
        }
    }
}