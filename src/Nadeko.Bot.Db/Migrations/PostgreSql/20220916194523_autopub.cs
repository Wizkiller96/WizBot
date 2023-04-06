using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NadekoBot.Migrations.PostgreSql
{
    public partial class autopub : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "rolerequirement",
                table: "shopentry",
                type: "numeric(20,0)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "autopublishchannel",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guildid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    channelid = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    dateadded = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_autopublishchannel", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_autopublishchannel_guildid",
                table: "autopublishchannel",
                column: "guildid",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "autopublishchannel");

            migrationBuilder.DropColumn(
                name: "rolerequirement",
                table: "shopentry");
        }
    }
}
