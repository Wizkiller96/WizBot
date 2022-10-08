using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NadekoBot.Migrations.PostgreSql
{
    public partial class newrero : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "reactionroles",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guildid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    channelid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    messageid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    emote = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    roleid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    group = table.Column<int>(type: "integer", nullable: false),
                    levelreq = table.Column<int>(type: "integer", nullable: false),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reactionroles", x => x.id);
                });

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
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guildconfigid = table.Column<int>(type: "integer", nullable: false),
                    channelid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    exclusive = table.Column<bool>(type: "boolean", nullable: false),
                    index = table.Column<int>(type: "integer", nullable: false),
                    messageid = table.Column<decimal>(type: "numeric(20,0)", nullable: false)
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
                });

            migrationBuilder.CreateTable(
                name: "reactionrole",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    dateadded = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    emotename = table.Column<string>(type: "text", nullable: true),
                    reactionrolemessageid = table.Column<int>(type: "integer", nullable: true),
                    roleid = table.Column<decimal>(type: "numeric(20,0)", nullable: false)
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
                });

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
