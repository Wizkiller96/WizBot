using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NadekoBot.Db.Migrations.Mysql
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
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    guildid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    messageid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    message = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    endsat = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_giveawaymodel", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "giveawayuser",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    giveawayid = table.Column<int>(type: "int", nullable: false),
                    userid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

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
