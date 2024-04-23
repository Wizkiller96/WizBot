using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NadekoBot.Db.Migrations
{
    /// <inheritdoc />
    public partial class stickyroles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "stickyroles",
                table: "guildconfigs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "stickyroles",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guildid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    roleids = table.Column<string>(type: "text", nullable: false),
                    userid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    dateadded = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stickyroles", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_stickyroles_guildid_userid",
                table: "stickyroles",
                columns: new[] { "guildid", "userid" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stickyroles");

            migrationBuilder.DropColumn(
                name: "stickyroles",
                table: "guildconfigs");
        }
    }
}
