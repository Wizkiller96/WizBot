using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NadekoBot.Migrations.PostgreSql
{
    public partial class gamblingstats : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "gamblingstats",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    feature = table.Column<string>(type: "text", nullable: true),
                    bet = table.Column<decimal>(type: "numeric", nullable: false),
                    paidout = table.Column<decimal>(type: "numeric", nullable: false),
                    dateadded = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gamblingstats", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_gamblingstats_feature",
                table: "gamblingstats",
                column: "feature",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gamblingstats");
        }
    }
}
