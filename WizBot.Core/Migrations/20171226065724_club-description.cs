using Microsoft.EntityFrameworkCore.Migrations;

namespace WizBot.Migrations
{
    public partial class clubdescription : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Clubs",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "Clubs");
        }
    }
}