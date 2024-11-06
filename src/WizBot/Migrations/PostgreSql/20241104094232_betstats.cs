using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WizBot.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class betstats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "userbetstats",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                              .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    userid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    game = table.Column<int>(type: "integer", nullable: false),
                    wincount = table.Column<long>(type: "bigint", nullable: false),
                    losecount = table.Column<long>(type: "bigint", nullable: false),
                    totalbet = table.Column<decimal>(type: "numeric", nullable: false),
                    paidout = table.Column<decimal>(type: "numeric", nullable: false),
                    maxwin = table.Column<long>(type: "bigint", nullable: false),
                    maxbet = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_userbetstats", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_userbetstats_userid_game",
                table: "userbetstats",
                columns: new[] { "userid", "game" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "userbetstats");
        }
    }
}