using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NadekoBot.Db.Migrations
{
    /// <inheritdoc />
    public partial class shopcommands : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "command",
                table: "shopentry",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "command",
                table: "shopentry");
        }
    }
}
