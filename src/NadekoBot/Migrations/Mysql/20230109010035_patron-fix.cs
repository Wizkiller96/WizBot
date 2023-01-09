using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NadekoBot.Migrations.Mysql
{
    public partial class patronfix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "pk_patrons",
                table: "patrons");

            migrationBuilder.AlterColumn<ulong>(
                name: "userid",
                table: "patrons",
                type: "bigint unsigned",
                nullable: false,
                oldClrType: typeof(ulong),
                oldType: "bigint unsigned")
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AddColumn<int>(
                name: "id",
                table: "patrons",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

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

            migrationBuilder.AlterColumn<ulong>(
                name: "userid",
                table: "patrons",
                type: "bigint unsigned",
                nullable: false,
                oldClrType: typeof(ulong),
                oldType: "bigint unsigned")
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AddPrimaryKey(
                name: "pk_patrons",
                table: "patrons",
                column: "userid");
        }
    }
}
