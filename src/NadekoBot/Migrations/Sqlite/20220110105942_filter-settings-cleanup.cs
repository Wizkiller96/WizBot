using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NadekoBot.Migrations
{
    public partial class filtersettingscleanup : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FilterChannelId_GuildConfigs_GuildConfigId1",
                table: "FilterChannelId");

            migrationBuilder.DropIndex(
                name: "IX_FilterChannelId_GuildConfigId1",
                table: "FilterChannelId");

            migrationBuilder.CreateTable(
                name: "FilterWordsChannelId",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChannelId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    GuildConfigId = table.Column<int>(type: "INTEGER", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FilterWordsChannelId", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FilterWordsChannelId_GuildConfigs_GuildConfigId",
                        column: x => x.GuildConfigId,
                        principalTable: "GuildConfigs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_FilterWordsChannelId_GuildConfigId",
                table: "FilterWordsChannelId",
                column: "GuildConfigId");

            migrationBuilder.Sql(@"INSERT INTO FilterWordsChannelId(Id, ChannelId, GuildConfigId, DateAdded)
SELECT Id, ChannelId, GuildConfigId1, DateAdded 
FROM FilterChannelId
WHERE GuildConfigId1 is not null;
-- Remove them after moving them to a different table
DELETE FROM FilterChannelId
WHERE GuildConfigId is null;");

            migrationBuilder.DropColumn(
                name: "GuildConfigId1",
                table: "FilterChannelId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FilterWordsChannelId");

            migrationBuilder.AddColumn<int>(
                name: "GuildConfigId1",
                table: "FilterChannelId",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FilterChannelId_GuildConfigId1",
                table: "FilterChannelId",
                column: "GuildConfigId1");

            migrationBuilder.AddForeignKey(
                name: "FK_FilterChannelId_GuildConfigs_GuildConfigId1",
                table: "FilterChannelId",
                column: "GuildConfigId1",
                principalTable: "GuildConfigs",
                principalColumn: "Id");
        }
    }
}
