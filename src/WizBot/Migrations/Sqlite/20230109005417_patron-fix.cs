using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WizBot.Migrations
{
    public partial class patronfix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Patrons",
                table: "Patrons");

            migrationBuilder.AlterColumn<ulong>(
                name: "UserId",
                table: "Patrons",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(ulong),
                oldType: "INTEGER")
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "Patrons",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0)
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Patrons",
                table: "Patrons",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Patrons_UserId",
                table: "Patrons",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Patrons",
                table: "Patrons");

            migrationBuilder.DropIndex(
                name: "IX_Patrons_UserId",
                table: "Patrons");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "Patrons");

            migrationBuilder.AlterColumn<ulong>(
                name: "UserId",
                table: "Patrons",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(ulong),
                oldType: "INTEGER")
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Patrons",
                table: "Patrons",
                column: "UserId");
        }
    }
}