using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NadekoBot.Migrations
{
    public partial class nsfwblacklisttags : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.CreateTable(
                name: "NsfwBlacklistedTags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Tag = table.Column<string>(type: "TEXT", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NsfwBlacklistedTags", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NsfwBlacklistedTags_GuildId",
                table: "NsfwBlacklistedTags",
                column: "GuildId");

            migrationBuilder.Sql(@"INSERT INTO NsfwBlacklistedTags(Id, GuildId, Tag, DateAdded)
SELECT 
    Id,
    (SELECT GuildId From GuildConfigs WHERE Id=GuildConfigId),
    Tag,
    DateAdded
FROM NsfwBlacklitedTag
WHERE GuildConfigId in (SELECT Id from GuildConfigs);");
            
            migrationBuilder.DropTable(
                name: "NsfwBlacklitedTag");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NsfwBlacklistedTags");

            migrationBuilder.CreateTable(
                name: "NsfwBlacklitedTag",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true),
                    GuildConfigId = table.Column<int>(type: "INTEGER", nullable: true),
                    Tag = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NsfwBlacklitedTag", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NsfwBlacklitedTag_GuildConfigs_GuildConfigId",
                        column: x => x.GuildConfigId,
                        principalTable: "GuildConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NsfwBlacklitedTag_GuildConfigId",
                table: "NsfwBlacklitedTag",
                column: "GuildConfigId");
        }
    }
}
