using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NadekoBot.Db.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class shopcommands : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Command",
                table: "ShopEntry",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Command",
                table: "ShopEntry");
        }
    }
}
