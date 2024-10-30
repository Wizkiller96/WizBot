using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WizBot.Migrations
{
    /// <inheritdoc />
    public partial class ncanvas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NCPixel",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                              .Annotation("Sqlite:Autoincrement", true),
                    Position = table.Column<int>(type: "INTEGER", nullable: false),
                    Price = table.Column<long>(type: "INTEGER", nullable: false),
                    OwnerId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Color = table.Column<uint>(type: "INTEGER", nullable: false),
                    Text = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NCPixel", x => x.Id);
                    table.UniqueConstraint("AK_NCPixel_Position", x => x.Position);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiscordUser_Username",
                table: "DiscordUser",
                column: "Username");

            migrationBuilder.CreateIndex(
                name: "IX_NCPixel_OwnerId",
                table: "NCPixel",
                column: "OwnerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NCPixel");

            migrationBuilder.DropIndex(
                name: "IX_DiscordUser_Username",
                table: "DiscordUser");
        }
    }
}