using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NadekoBot.Migrations.Mysql
{
    public partial class newrero : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.CreateTable(
                name: "reactionroles",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    guildid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    channelid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    messageid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    emote = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    roleid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    group = table.Column<int>(type: "int", nullable: false),
                    levelreq = table.Column<int>(type: "int", nullable: false),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reactionroles", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ix_reactionroles_guildid",
                table: "reactionroles",
                column: "guildid");

            migrationBuilder.CreateIndex(
                name: "ix_reactionroles_messageid_emote",
                table: "reactionroles",
                columns: new[] { "messageid", "emote" },
                unique: true);
            
            MigrationQueries.MigrateRero(migrationBuilder);
            
            migrationBuilder.DropTable(
                name: "reactionrole");

            migrationBuilder.DropTable(
                name: "reactionrolemessage");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "reactionroles");

            migrationBuilder.CreateTable(
                name: "reactionrolemessage",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    guildconfigid = table.Column<int>(type: "int", nullable: false),
                    channelid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    exclusive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    index = table.Column<int>(type: "int", nullable: false),
                    messageid = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reactionrolemessage", x => x.id);
                    table.ForeignKey(
                        name: "fk_reactionrolemessage_guildconfigs_guildconfigid",
                        column: x => x.guildconfigid,
                        principalTable: "guildconfigs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "reactionrole",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    emotename = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    reactionrolemessageid = table.Column<int>(type: "int", nullable: true),
                    roleid = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reactionrole", x => x.id);
                    table.ForeignKey(
                        name: "fk_reactionrole_reactionrolemessage_reactionrolemessageid",
                        column: x => x.reactionrolemessageid,
                        principalTable: "reactionrolemessage",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ix_reactionrole_reactionrolemessageid",
                table: "reactionrole",
                column: "reactionrolemessageid");

            migrationBuilder.CreateIndex(
                name: "ix_reactionrolemessage_guildconfigid",
                table: "reactionrolemessage",
                column: "guildconfigid");
        }
    }
}
