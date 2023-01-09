using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NadekoBot.Migrations.PostgreSql
{
    public partial class patronfix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "pk_patrons",
                table: "patrons");

            migrationBuilder.AddColumn<int>(
                name: "id",
                table: "patrons",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddPrimaryKey(
                name: "pk_patrons",
                table: "patrons",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "ix_patrons_userid",
                table: "patrons",
                column: "userid");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "pk_patrons",
                table: "patrons");

            migrationBuilder.DropIndex(
                name: "ix_patrons_userid",
                table: "patrons");

            migrationBuilder.DropColumn(
                name: "id",
                table: "patrons");

            migrationBuilder.AddPrimaryKey(
                name: "pk_patrons",
                table: "patrons",
                column: "userid");
        }
    }
}
