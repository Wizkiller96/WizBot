using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NadekoBot.Migrations
{
    public partial class newrero : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReactionRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                              .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    ChannelId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    MessageId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Emote = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    RoleId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Group = table.Column<int>(type: "INTEGER", nullable: false),
                    LevelReq = table.Column<int>(type: "INTEGER", nullable: false),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReactionRoles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReactionRoles_GuildId",
                table: "ReactionRoles",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_ReactionRoles_MessageId_Emote",
                table: "ReactionRoles",
                columns: new[] { "MessageId", "Emote" },
                unique: true);
            
            MigrationQueries.MigrateRero(migrationBuilder);
            
            migrationBuilder.DropTable(
                name: "ReactionRole");

            migrationBuilder.DropTable(
                name: "ReactionRoleMessage");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReactionRoles");

            migrationBuilder.CreateTable(
                name: "ReactionRoleMessage",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildConfigId = table.Column<int>(type: "INTEGER", nullable: false),
                    ChannelId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Exclusive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Index = table.Column<int>(type: "INTEGER", nullable: false),
                    MessageId = table.Column<ulong>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReactionRoleMessage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReactionRoleMessage_GuildConfigs_GuildConfigId",
                        column: x => x.GuildConfigId,
                        principalTable: "GuildConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReactionRole",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EmoteName = table.Column<string>(type: "TEXT", nullable: true),
                    ReactionRoleMessageId = table.Column<int>(type: "INTEGER", nullable: true),
                    RoleId = table.Column<ulong>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReactionRole", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReactionRole_ReactionRoleMessage_ReactionRoleMessageId",
                        column: x => x.ReactionRoleMessageId,
                        principalTable: "ReactionRoleMessage",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReactionRole_ReactionRoleMessageId",
                table: "ReactionRole",
                column: "ReactionRoleMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_ReactionRoleMessage_GuildConfigId",
                table: "ReactionRoleMessage",
                column: "GuildConfigId");
        }
    }
}
