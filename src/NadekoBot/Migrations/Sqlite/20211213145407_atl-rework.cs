using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NadekoBot.Migrations
{
    public partial class atlrework : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AutoTranslateChannels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    ChannelId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    AutoDelete = table.Column<bool>(type: "INTEGER", nullable: false),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutoTranslateChannels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AutoTranslateUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChannelId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Source = table.Column<string>(type: "TEXT", nullable: true),
                    Target = table.Column<string>(type: "TEXT", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutoTranslateUsers", x => x.Id);
                    table.UniqueConstraint("AK_AutoTranslateUsers_ChannelId_UserId", x => new { x.ChannelId, x.UserId });
                    table.ForeignKey(
                        name: "FK_AutoTranslateUsers_AutoTranslateChannels_ChannelId",
                        column: x => x.ChannelId,
                        principalTable: "AutoTranslateChannels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AutoTranslateChannels_ChannelId",
                table: "AutoTranslateChannels",
                column: "ChannelId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AutoTranslateChannels_GuildId",
                table: "AutoTranslateChannels",
                column: "GuildId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AutoTranslateUsers");

            migrationBuilder.DropTable(
                name: "AutoTranslateChannels");
        }
    }
}
