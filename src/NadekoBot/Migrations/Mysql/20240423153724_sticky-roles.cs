using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NadekoBot.Db.Migrations.Mysql
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
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "stickyroles",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    guildid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    roleids = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    userid = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    dateadded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stickyroles", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

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
