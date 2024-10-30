using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WizBot.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class ncanvas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ncpixel",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    position = table.Column<int>(type: "integer", nullable: false),
                    price = table.Column<long>(type: "bigint", nullable: false),
                    ownerid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    color = table.Column<long>(type: "bigint", nullable: false),
                    text = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ncpixel", x => x.id);
                    table.UniqueConstraint("ak_ncpixel_position", x => x.position);
                });

            migrationBuilder.CreateIndex(
                name: "ix_discorduser_username",
                table: "discorduser",
                column: "username");

            migrationBuilder.CreateIndex(
                name: "ix_ncpixel_ownerid",
                table: "ncpixel",
                column: "ownerid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ncpixel");

            migrationBuilder.DropIndex(
                name: "ix_discorduser_username",
                table: "discorduser");
        }
    }
}
