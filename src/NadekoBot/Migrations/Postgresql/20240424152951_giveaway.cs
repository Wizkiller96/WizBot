using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NadekoBot.Db.Migrations
{
    /// <inheritdoc />
    public partial class giveaway : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "giveawaymodel",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guildid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    messageid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    message = table.Column<string>(type: "text", nullable: false),
                    endsat = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_giveawaymodel", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "giveawayuser",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    giveawayid = table.Column<int>(type: "integer", nullable: false),
                    userid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_giveawayuser", x => x.id);
                    table.ForeignKey(
                        name: "fk_giveawayuser_giveawaymodel_giveawayid",
                        column: x => x.giveawayid,
                        principalTable: "giveawaymodel",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_giveawayuser_giveawayid_userid",
                table: "giveawayuser",
                columns: new[] { "giveawayid", "userid" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "giveawayuser");

            migrationBuilder.DropTable(
                name: "giveawaymodel");
        }
    }
}
