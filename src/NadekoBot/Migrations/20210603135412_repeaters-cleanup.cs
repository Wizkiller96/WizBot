using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NadekoBot.Migrations
{
    public partial class repeaterscleanup : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Repeaters",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(nullable: false),
                    ChannelId = table.Column<ulong>(nullable: false),
                    LastMessageId = table.Column<ulong>(nullable: true),
                    Message = table.Column<string>(nullable: true),
                    Interval = table.Column<TimeSpan>(nullable: false),
                    StartTimeOfDay = table.Column<TimeSpan>(nullable: true),
                    NoRedundant = table.Column<bool>(nullable: false),
                    DateAdded = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Repeaters", x => x.Id);
                });
            
            migrationBuilder.Sql(@"INSERT INTO Repeaters(Id, GuildId, ChannelId, LastMessageId, Message, Interval, StartTimeOfDay, NoRedundant, DateAdded)
SELECT Id, GuildId, ChannelId, LastMessageId, Message, Interval, StartTimeOfDay, NoRedundant, DateAdded FROM GuildRepeater
WHERE DateAdded is not null");
            
            migrationBuilder.DropTable(
                name: "GuildRepeater");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Repeaters");

            migrationBuilder.CreateTable(
                name: "GuildRepeater",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChannelId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true),
                    GuildConfigId = table.Column<int>(type: "INTEGER", nullable: true),
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Interval = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    LastMessageId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    Message = table.Column<string>(type: "TEXT", nullable: true),
                    NoRedundant = table.Column<bool>(type: "INTEGER", nullable: false),
                    StartTimeOfDay = table.Column<TimeSpan>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildRepeater", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuildRepeater_GuildConfigs_GuildConfigId",
                        column: x => x.GuildConfigId,
                        principalTable: "GuildConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GuildRepeater_GuildConfigId",
                table: "GuildRepeater",
                column: "GuildConfigId");
        }
    }
}
